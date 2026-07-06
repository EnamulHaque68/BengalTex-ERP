using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting.InventoryGL;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.LandedCost;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.SupplierInvoice.Commands;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A2 — Inventory Truth (GR/IR). Covers the supplier-bill posting legs (new GR/IR clearing
/// vs legacy inventory-debit, purchase price variance, service lines, cancel mirror), the
/// over-billing guard, GR/IR initialization, landed-cost on-credit settlement and the tie-out.
/// </summary>
public class InventoryTruthTests
{
    // ── account seeding shared across the journal-posting tests ──
    private static void SeedCoa(ApplicationDbContext ctx)
    {
        var defs = new (int Id, string Code, string Name, AccountType Type)[]
        {
            (1, "1110", "Cash", AccountType.Asset),
            (2, "1120", "Bank", AccountType.Asset),
            (3, "1140", "RM Inventory", AccountType.Asset),
            (4, "1150", "FG Inventory", AccountType.Asset),
            (5, "1160", "WIP", AccountType.Asset),
            (6, "1170", "VAT Receivable", AccountType.Asset),
            (7, "2110", "Accounts Payable", AccountType.Liability),
            (8, "2115", "Accrued Charges Payable", AccountType.Liability),
            (9, "2150", "GR/IR Clearing", AccountType.Liability),
            (10, "5100", "COGS", AccountType.Expense),
            (11, "5155", "Purchase Price Variance", AccountType.Expense),
            (12, "5400", "Admin Expense", AccountType.Expense),
        };
        foreach (var d in defs)
            ctx.Accounts.Add(new Account { Id = d.Id, Code = d.Code, Name = d.Name, AccountType = d.Type });
    }

    private static JournalPostingService PostingService(ApplicationDbContext ctx) =>
        new(new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            TestHarness.Numbering().Object, new StubCurrentUser(), new StubClock(),
            new PeriodGuard(ctx, new StubCurrentUser()));

    private static readonly Mock<IMediator> MediatorStub = new();

    /// <summary>Net GL balance (Dr − Cr) for an account code from posted journal lines.</summary>
    private static decimal GlBalance(ApplicationDbContext ctx, string code)
    {
        var acc = ctx.Accounts.Single(a => a.Code == code);
        return ctx.JournalEntryLines.Where(l => l.AccountId == acc.Id).Sum(l => l.Debit)
             - ctx.JournalEntryLines.Where(l => l.AccountId == acc.Id).Sum(l => l.Credit);
    }

    // ═══════════ 1. SupplierBillPosting pure logic ═══════════

    private static Domain.Entities.SupplierInvoice Bill(decimal vat, params SupplierInvoiceLine[] lines)
    {
        var inv = new Domain.Entities.SupplierInvoice { ExchangeRate = 1m, VatRate = vat };
        foreach (var l in lines) inv.Lines.Add(l);
        inv.SubtotalAmount = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);
        inv.VatAmount = Math.Round(inv.SubtotalAmount * vat, 4);
        inv.TotalAmount = inv.SubtotalAmount + inv.VatAmount;
        return inv;
    }

    private static Domain.Entities.PurchaseOrder Po(params (int rm, decimal price, decimal received)[] lines)
    {
        var po = new Domain.Entities.PurchaseOrder { Id = 1, ExchangeRate = 1m };
        foreach (var (rm, price, received) in lines)
            po.Lines.Add(new PurchaseOrderLine { RawMaterialId = rm, UnitPrice = price, Quantity = received, ReceivedQuantity = received });
        return po;
    }

    [Fact]
    public void New_path_clears_grir_and_isolates_price_variance()
    {
        // PO 100 @ 50; bill 100 @ 52 → GR/IR 5000, PPV +200, AP 5980 (incl 15% VAT on 5200).
        var inv = Bill(0.15m, new SupplierInvoiceLine { RawMaterialId = 7, Quantity = 100m, UnitPrice = 52m });
        var po = Po((7, 50m, 100m));

        var legs = SupplierBillPosting.BuildApprovalLegs(inv, po, useGrIrPath: true);

        legs.Single(l => l.AccountCode == "2150").Debit.Should().Be(5000m);
        legs.Single(l => l.AccountCode == "5155").Debit.Should().Be(200m);
        legs.Single(l => l.AccountCode == "1170").Debit.Should().Be(780m);
        legs.Single(l => l.AccountCode == "2110").Credit.Should().Be(5980m);
        legs.Sum(l => l.Debit).Should().Be(legs.Sum(l => l.Credit));   // balanced
        legs.Should().NotContain(l => l.AccountCode == "1140");        // inventory untouched on the bill
    }

    [Fact]
    public void New_path_credits_ppv_when_bill_is_cheaper()
    {
        var inv = Bill(0m, new SupplierInvoiceLine { RawMaterialId = 7, Quantity = 100m, UnitPrice = 48m });
        var po = Po((7, 50m, 100m));

        var legs = SupplierBillPosting.BuildApprovalLegs(inv, po, useGrIrPath: true);

        legs.Single(l => l.AccountCode == "2150").Debit.Should().Be(5000m);
        legs.Single(l => l.AccountCode == "5155").Credit.Should().Be(200m);   // cheaper → credit variance
        legs.Single(l => l.AccountCode == "2110").Credit.Should().Be(4800m);
    }

    [Fact]
    public void Legacy_path_debits_inventory_directly()
    {
        var inv = Bill(0m, new SupplierInvoiceLine { RawMaterialId = 7, Quantity = 100m, UnitPrice = 52m });
        var po = Po((7, 50m, 100m));

        var legs = SupplierBillPosting.BuildApprovalLegs(inv, po, useGrIrPath: false);

        legs.Single(l => l.AccountCode == "1140").Debit.Should().Be(5200m);   // bill value, today's behaviour
        legs.Should().NotContain(l => l.AccountCode == "2150");
        legs.Should().NotContain(l => l.AccountCode == "5155");
    }

    [Fact]
    public void Service_line_debits_its_expense_account_not_inventory()
    {
        var inv = Bill(0m,
            new SupplierInvoiceLine { RawMaterialId = 7, Quantity = 10m, UnitPrice = 50m },
            new SupplierInvoiceLine { AccountId = 12, Account = new Account { Id = 12, Code = "5400" }, Quantity = 1m, UnitPrice = 300m });
        var po = Po((7, 50m, 10m));

        var legs = SupplierBillPosting.BuildApprovalLegs(inv, po, useGrIrPath: true);

        legs.Single(l => l.AccountCode == "5400").Debit.Should().Be(300m);    // C&F etc. → expense
        legs.Single(l => l.AccountCode == "2150").Debit.Should().Be(500m);    // only the material clears GR/IR
        legs.Single(l => l.AccountCode == "2110").Credit.Should().Be(800m);   // gross = 500 + 300
    }

    [Fact]
    public void Cancel_mirror_is_the_exact_reverse()
    {
        var inv = Bill(0m, new SupplierInvoiceLine { RawMaterialId = 7, Quantity = 100m, UnitPrice = 52m });
        var legs = SupplierBillPosting.BuildApprovalLegs(inv, Po((7, 50m, 100m)), useGrIrPath: true);
        var mirror = SupplierBillPosting.Mirror(legs);

        foreach (var l in legs)
            mirror.Should().ContainSingle(m => m.AccountCode == l.AccountCode && m.Debit == l.Credit && m.Credit == l.Debit);
    }

    // ═══════════ 2. Bill approve end-to-end ═══════════

    private static (Domain.Entities.PurchaseOrder po, Domain.Entities.SupplierInvoice inv) SeedPoAndBill(
        ApplicationDbContext ctx, bool grnGlPosted, decimal billPrice)
    {
        var po = new Domain.Entities.PurchaseOrder
        {
            Code = "PO-1", SupplierId = 1, ExchangeRate = 1m,
            Status = PurchaseOrderStatus.PartiallyReceived,
            Lines = { new PurchaseOrderLine { RawMaterialId = 7, UnitPrice = 50m, Quantity = 100m, ReceivedQuantity = 100m } }
        };
        ctx.PurchaseOrders.Add(po);
        ctx.GoodsReceiptNotes.Add(new GoodsReceiptNote
        {
            Code = "GRN-1", PurchaseOrderId = po.Id, ReceiveDate = new DateOnly(2026, 3, 1),
            Status = GoodsReceiptStatus.Posted, IsGlPosted = grnGlPosted, ReceivingWarehouseId = 1
        });
        var inv = new Domain.Entities.SupplierInvoice
        {
            Code = "SI-1", SupplierId = 1, PurchaseOrderId = po.Id, CurrencyId = 1, ExchangeRate = 1m,
            InvoiceDate = new DateOnly(2026, 3, 10), Status = SupplierInvoiceStatus.Draft, VatRate = 0m,
            Lines = { new SupplierInvoiceLine { RawMaterialId = 7, Quantity = 100m, UnitPrice = billPrice } }
        };
        ctx.SupplierInvoices.Add(inv);
        ctx.SaveChanges();
        return (po, inv);
    }

    private ApproveSupplierInvoiceCommandHandler ApproveHandler(ApplicationDbContext ctx)
    {
        MediatorStub.Setup(m => m.Send(It.IsAny<Application.SupplierInvoice.Queries.GetSupplierInvoiceByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BengalTex.ERP.Shared.Common.ApiResponse<Application.SupplierInvoice.Dtos.SupplierInvoiceDto>.Ok(null!));
        return new ApproveSupplierInvoiceCommandHandler(
            new Repository<Domain.Entities.SupplierInvoice, long>(ctx),
            new Repository<Domain.Entities.PurchaseOrder, long>(ctx),
            new Repository<Domain.Entities.GoodsReceiptNote, long>(ctx),
            new UnitOfWork(ctx), new StubCurrentUser(), PostingService(ctx), MediatorStub.Object);
    }

    [Fact]
    public async Task Bill_approve_new_path_nets_grir_to_zero_after_matching_receipt()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        // Simulate the GRN having posted Dr 1140 / Cr 2150 5000 already.
        ctx.SaveChanges();
        await PostingService(ctx).PostAsync(new DateOnly(2026, 3, 1), "grn", "GoodsReceiptNote", 1, "GRN-1",
            new[] { new JournalPostingLine("1140", 5000m, 0m), new JournalPostingLine("2150", 0m, 5000m) });
        var (_, inv) = SeedPoAndBill(ctx, grnGlPosted: true, billPrice: 52m);

        var res = await ApproveHandler(ctx).Handle(new ApproveSupplierInvoiceCommand(inv.Id), default);

        res.Success.Should().BeTrue();
        GlBalance(ctx, "2150").Should().Be(0m);        // receipt liability fully cleared
        GlBalance(ctx, "5155").Should().Be(200m);      // the price rise is isolated
        GlBalance(ctx, "1140").Should().Be(5000m);     // inventory stayed at receipt value
        GlBalance(ctx, "2110").Should().Be(-5200m);    // AP credited gross
    }

    [Fact]
    public async Task Bill_approve_legacy_path_debits_inventory()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var (_, inv) = SeedPoAndBill(ctx, grnGlPosted: false, billPrice: 52m);

        var res = await ApproveHandler(ctx).Handle(new ApproveSupplierInvoiceCommand(inv.Id), default);

        res.Success.Should().BeTrue();
        GlBalance(ctx, "1140").Should().Be(5200m);     // legacy: bill debits inventory
        GlBalance(ctx, "2150").Should().Be(0m);        // GR/IR untouched
    }

    [Fact]
    public async Task Over_billing_beyond_received_is_blocked()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.RawMaterials.Add(new RawMaterial { Id = 7, Code = "RM7", Name = "Yarn" });
        var po = new Domain.Entities.PurchaseOrder
        {
            Code = "PO-2", SupplierId = 1, ExchangeRate = 1m, Status = PurchaseOrderStatus.PartiallyReceived,
            Lines = { new PurchaseOrderLine { RawMaterialId = 7, UnitPrice = 50m, Quantity = 100m, ReceivedQuantity = 40m } }
        };
        ctx.PurchaseOrders.Add(po);
        ctx.SaveChanges();   // materialize po.Id before referencing it
        ctx.GoodsReceiptNotes.Add(new GoodsReceiptNote { Code = "GRN-2", PurchaseOrderId = po.Id, Status = GoodsReceiptStatus.Posted, IsGlPosted = true, ReceivingWarehouseId = 1, ReceiveDate = new DateOnly(2026, 3, 1) });
        var inv = new Domain.Entities.SupplierInvoice
        {
            Code = "SI-2", SupplierId = 1, PurchaseOrderId = po.Id, CurrencyId = 1, ExchangeRate = 1m,
            InvoiceDate = new DateOnly(2026, 3, 10), Status = SupplierInvoiceStatus.Draft, VatRate = 0m,
            Lines = { new SupplierInvoiceLine { RawMaterialId = 7, Quantity = 100m, UnitPrice = 50m } }   // billing 100, only 40 received
        };
        ctx.SupplierInvoices.Add(inv);
        ctx.SaveChanges();

        var res = await ApproveHandler(ctx).Handle(new ApproveSupplierInvoiceCommand(inv.Id), default);

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("exceed received");
        ctx.SupplierInvoices.Single().Status.Should().Be(SupplierInvoiceStatus.Draft);   // not approved
    }

    // ═══════════ 3. GR/IR initialization ═══════════

    [Fact]
    public async Task GrIr_init_catches_up_unbilled_received_value_and_marks_grns()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var po = new Domain.Entities.PurchaseOrder
        {
            Code = "PO-9", SupplierId = 1, ExchangeRate = 1m, Status = PurchaseOrderStatus.Received,
            Supplier = new Supplier { Id = 1, Code = "S1", Name = "Acme" },
            Lines = { new PurchaseOrderLine { RawMaterialId = 7, UnitPrice = 50m, Quantity = 100m, ReceivedQuantity = 100m } }
        };
        ctx.PurchaseOrders.Add(po);
        ctx.GoodsReceiptNotes.Add(new GoodsReceiptNote { Code = "GRN-9", PurchaseOrderId = po.Id, Status = GoodsReceiptStatus.Posted, IsGlPosted = false, ReceivingWarehouseId = 1, ReceiveDate = new DateOnly(2026, 2, 1) });
        // 60 of the 100 already billed under legacy → 40 unbilled.
        ctx.SupplierInvoices.Add(new Domain.Entities.SupplierInvoice
        {
            Code = "SI-9", SupplierId = 1, PurchaseOrderId = po.Id, CurrencyId = 1, ExchangeRate = 1m,
            InvoiceDate = new DateOnly(2026, 2, 5), Status = SupplierInvoiceStatus.Approved, VatRate = 0m,
            Lines = { new SupplierInvoiceLine { RawMaterialId = 7, Quantity = 60m, UnitPrice = 50m } }
        });
        ctx.SaveChanges();

        var handler = new InitializeGrIrCommandHandler(
            new Repository<Domain.Entities.PurchaseOrder, long>(ctx),
            new Repository<Domain.Entities.GoodsReceiptNote, long>(ctx),
            new Repository<Domain.Entities.SupplierInvoice, long>(ctx),
            new Repository<JournalEntry, long>(ctx),
            new Repository<Account>(ctx),
            new PeriodGuard(ctx, new StubCurrentUser()), TestHarness.Numbering().Object,
            new StubCurrentUser(), new UnitOfWork(ctx));

        var res = await handler.Handle(new InitializeGrIrCommand(new DateOnly(2026, 3, 31)), default);

        res.Success.Should().BeTrue();
        GlBalance(ctx, "1140").Should().Be(2000m);    // 40 × 50 caught up
        GlBalance(ctx, "2150").Should().Be(-2000m);   // GR/IR credit established
        ctx.GoodsReceiptNotes.Single().IsGlPosted.Should().BeTrue();

        // Second run is blocked.
        var again = await handler.Handle(new InitializeGrIrCommand(new DateOnly(2026, 3, 31)), default);
        again.Success.Should().BeFalse();
        again.Message.Should().Contain("already been initialized");
    }

    // ═══════════ 4. Landed-cost on-credit settle ═══════════

    [Fact]
    public async Task Landed_cost_settle_clears_accrued_charges()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var v = new LandedCostVoucher
        {
            Code = "LCV-1", VoucherDate = new DateOnly(2026, 3, 1), GoodsReceiptNoteId = 1,
            IsOnCredit = true, SupplierId = 1, Status = LandedCostVoucherStatus.Posted, PostedAt = DateTimeOffset.UtcNow,
            Charges = { new LandedCostCharge { ChargeType = LandedCostChargeType.Freight, Amount = 3000m } }
        };
        ctx.LandedCostVouchers.Add(v);
        ctx.SaveChanges();
        // Simulate the original on-credit posting (Dr inventory / Cr 2115) so 2115 carries the liability.
        await PostingService(ctx).PostAsync(new DateOnly(2026, 3, 1), "lc", "LandedCostVoucher", v.Id, "LCV-1",
            new[] { new JournalPostingLine("1140", 3000m, 0m), new JournalPostingLine("2115", 0m, 3000m) });
        ctx.SaveChanges();

        var handler = new SettleLandedCostVoucherCommandHandler(
            new Repository<LandedCostVoucher, long>(ctx), PostingService(ctx), new StubCurrentUser(), new UnitOfWork(ctx));

        var res = await handler.Handle(new SettleLandedCostVoucherCommand(v.Id, new DateOnly(2026, 3, 20), "BankTransfer"), default);

        res.Success.Should().BeTrue();
        GlBalance(ctx, "2115").Should().Be(0m);      // liability fully cleared (Cr 3000 then Dr 3000)
        GlBalance(ctx, "1120").Should().Be(-3000m);  // paid from bank
        ctx.LandedCostVouchers.Single().SettledAt.Should().NotBeNull();

        var again = await handler.Handle(new SettleLandedCostVoucherCommand(v.Id, new DateOnly(2026, 3, 21), "Cash"), default);
        again.Success.Should().BeFalse();   // already settled
    }

    // ═══════════ 5. Tie-out ═══════════

    [Fact]
    public async Task Tie_out_reports_stock_vs_gl_variance()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.RawMaterials.Add(new RawMaterial { Id = 7, Code = "RM7", Name = "Yarn", WeightedAverageCost = 50m });
        ctx.StockOnHand.Add(new StockOnHand { RawMaterialId = 7, WarehouseId = 1, Quantity = 100m });   // stock value 5000
        ctx.SaveChanges();
        // GL 1140 only shows 4000 → variance 1000.
        await PostingService(ctx).PostAsync(new DateOnly(2026, 3, 1), "x", "GoodsReceiptNote", 1, "GRN",
            new[] { new JournalPostingLine("1140", 4000m, 0m), new JournalPostingLine("2150", 0m, 4000m) });
        ctx.SaveChanges();   // commit the journal so the tie-out query sees it

        var handler = new GetInventoryGlTieOutQueryHandler(
            new Repository<StockOnHand>(ctx), new Repository<RawMaterial>(ctx), new Repository<Product>(ctx),
            new Repository<JournalEntryLine, long>(ctx), new Repository<Domain.Entities.PurchaseOrder, long>(ctx),
            new Repository<Domain.Entities.GoodsReceiptNote, long>(ctx), new Repository<Domain.Entities.SupplierInvoice, long>(ctx));

        var res = await handler.Handle(new GetInventoryGlTieOutQuery(new DateOnly(2026, 3, 31)), default);

        res.Success.Should().BeTrue();
        var rm = res.Data!.Rows.Single(r => r.AccountCode == "1140");
        rm.StockValue.Should().Be(5000m);
        rm.GlBalance.Should().Be(4000m);
        rm.Variance.Should().Be(1000m);
        rm.Matches.Should().BeFalse();
        res.Data.GrIrBalance.Should().Be(4000m);   // net credit shown positive
    }
}
