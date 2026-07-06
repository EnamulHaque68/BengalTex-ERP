using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.LandedCost;

internal static class LandedCostRules
{
    public static void ApplyCharges<T>(AbstractValidator<T> v, Func<T, IReadOnlyList<LandedCostChargeInput>> charges)
    {
        v.RuleFor(x => charges(x)).NotEmpty().WithMessage("Add at least one charge.");
        v.RuleForEach(x => charges(x)).ChildRules(c =>
        {
            c.RuleFor(i => i.ChargeType).Must(t => Enum.TryParse<LandedCostChargeType>(t, out _))
                .WithMessage("Invalid charge type.");
            c.RuleFor(i => i.Amount).GreaterThan(0);
            c.RuleFor(i => i.Notes).MaximumLength(500);
        });
    }

    public static List<LandedCostCharge> Build(IReadOnlyList<LandedCostChargeInput> charges) =>
        charges.Select((c, i) => new LandedCostCharge
        {
            ChargeType = Enum.Parse<LandedCostChargeType>(c.ChargeType),
            Amount = c.Amount,
            Notes = string.IsNullOrWhiteSpace(c.Notes) ? null : c.Notes.Trim(),
            SortOrder = i
        }).ToList();
}

// ── Create ──
public sealed record CreateLandedCostVoucherCommand(
    DateOnly VoucherDate, long GoodsReceiptNoteId, string AllocationBasis, string PaymentMethod,
    string? Notes, IReadOnlyList<LandedCostChargeInput> Charges,
    bool IsOnCredit = false, int? SupplierId = null) : IRequest<ApiResponse<long>>;

public sealed class CreateLandedCostVoucherCommandValidator : AbstractValidator<CreateLandedCostVoucherCommand>
{
    public CreateLandedCostVoucherCommandValidator()
    {
        RuleFor(x => x.VoucherDate).NotEmpty();
        RuleFor(x => x.GoodsReceiptNoteId).GreaterThan(0);
        RuleFor(x => x.AllocationBasis).Must(b => Enum.TryParse<LandedCostAllocationBasis>(b, out _)).WithMessage("Invalid allocation basis.");
        RuleFor(x => x.PaymentMethod).Must(p => Enum.TryParse<PaymentMethod>(p, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.Notes).MaximumLength(2000);
        // Phase A2 — on credit requires the agent/supplier owed.
        RuleFor(x => x.SupplierId).NotNull().GreaterThan(0)
            .When(x => x.IsOnCredit)
            .WithMessage("Select the agent/supplier the charges are owed to when booking on credit.");
        LandedCostRules.ApplyCharges(this, x => x.Charges);
    }
}

internal sealed class CreateLandedCostVoucherCommandHandler : IRequestHandler<CreateLandedCostVoucherCommand, ApiResponse<long>>
{
    private readonly IRepository<LandedCostVoucher, long> _repo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateLandedCostVoucherCommandHandler(
        IRepository<LandedCostVoucher, long> repo, IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _grnRepo = grnRepo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<long>> Handle(CreateLandedCostVoucherCommand cmd, CancellationToken ct)
    {
        var grn = await _grnRepo.GetByIdAsync(cmd.GoodsReceiptNoteId, ct);
        if (grn is null) return ApiResponse<long>.Fail("Goods receipt not found.");
        if (grn.Status != GoodsReceiptStatus.Posted)
            return ApiResponse<long>.Fail("Landed cost can only be applied to a posted goods receipt.");

        var e = new LandedCostVoucher
        {
            Code = await _numbering.NextAsync("LCV", null, ct),
            VoucherDate = cmd.VoucherDate,
            GoodsReceiptNoteId = cmd.GoodsReceiptNoteId,
            AllocationBasis = Enum.Parse<LandedCostAllocationBasis>(cmd.AllocationBasis),
            PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod),
            IsOnCredit = cmd.IsOnCredit,
            SupplierId = cmd.IsOnCredit ? cmd.SupplierId : null,
            Status = LandedCostVoucherStatus.Draft,
            Notes = cmd.Notes,
            Charges = LandedCostRules.Build(cmd.Charges)
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(e.Id, "Landed-cost voucher created.");
    }
}

// ── Update (draft only) ──
public sealed record UpdateLandedCostVoucherCommand(
    long Id, DateOnly VoucherDate, string AllocationBasis, string PaymentMethod,
    string? Notes, IReadOnlyList<LandedCostChargeInput> Charges,
    bool IsOnCredit = false, int? SupplierId = null) : IRequest<ApiResponse<long>>;

public sealed class UpdateLandedCostVoucherCommandValidator : AbstractValidator<UpdateLandedCostVoucherCommand>
{
    public UpdateLandedCostVoucherCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.VoucherDate).NotEmpty();
        RuleFor(x => x.AllocationBasis).Must(b => Enum.TryParse<LandedCostAllocationBasis>(b, out _)).WithMessage("Invalid allocation basis.");
        RuleFor(x => x.PaymentMethod).Must(p => Enum.TryParse<PaymentMethod>(p, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.SupplierId).NotNull().GreaterThan(0)
            .When(x => x.IsOnCredit)
            .WithMessage("Select the agent/supplier the charges are owed to when booking on credit.");
        LandedCostRules.ApplyCharges(this, x => x.Charges);
    }
}

internal sealed class UpdateLandedCostVoucherCommandHandler : IRequestHandler<UpdateLandedCostVoucherCommand, ApiResponse<long>>
{
    private readonly IRepository<LandedCostVoucher, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateLandedCostVoucherCommandHandler(IRepository<LandedCostVoucher, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<long>> Handle(UpdateLandedCostVoucherCommand cmd, CancellationToken ct)
    {
        var e = await _repo.Query().Include(x => x.Charges).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (e is null) return ApiResponse<long>.Fail("Landed-cost voucher not found.");
        if (e.Status != LandedCostVoucherStatus.Draft) return ApiResponse<long>.Fail("Only draft vouchers can be edited.");

        e.VoucherDate = cmd.VoucherDate;
        e.AllocationBasis = Enum.Parse<LandedCostAllocationBasis>(cmd.AllocationBasis);
        e.PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
        e.IsOnCredit = cmd.IsOnCredit;
        e.SupplierId = cmd.IsOnCredit ? cmd.SupplierId : null;
        e.Notes = cmd.Notes;
        e.Charges.Clear();
        foreach (var c in LandedCostRules.Build(cmd.Charges)) e.Charges.Add(c);

        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(e.Id, "Landed-cost voucher updated.");
    }
}

// ── Delete (draft only) ──
public sealed record DeleteLandedCostVoucherCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteLandedCostVoucherCommandHandler : IRequestHandler<DeleteLandedCostVoucherCommand, ApiResponse>
{
    private readonly IRepository<LandedCostVoucher, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteLandedCostVoucherCommandHandler(IRepository<LandedCostVoucher, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteLandedCostVoucherCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Landed-cost voucher not found.");
        if (e.Status != LandedCostVoucherStatus.Draft) return ApiResponse.Fail("Only draft vouchers can be deleted.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Landed-cost voucher deleted.");
    }
}

// ── Post (capitalise charges into RM WAC + journal) ──
public sealed record PostLandedCostVoucherCommand(long Id) : IRequest<ApiResponse<long>>;

internal sealed class PostLandedCostVoucherCommandHandler : IRequestHandler<PostLandedCostVoucherCommand, ApiResponse<long>>
{
    private readonly IRepository<LandedCostVoucher, long> _repo;
    private readonly IStockService _stock;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public PostLandedCostVoucherCommandHandler(
        IRepository<LandedCostVoucher, long> repo, IStockService stock, IJournalPostingService journal,
        ICurrentUserService currentUser, IUnitOfWork uow)
    { _repo = repo; _stock = stock; _journal = journal; _currentUser = currentUser; _uow = uow; }

    public async Task<ApiResponse<long>> Handle(PostLandedCostVoucherCommand cmd, CancellationToken ct)
    {
        var v = await _repo.Query()
            .Include(x => x.Charges)
            .Include(x => x.GoodsReceiptNote).ThenInclude(g => g.PurchaseOrder)
            .Include(x => x.GoodsReceiptNote).ThenInclude(g => g.Lines).ThenInclude(l => l.PurchaseOrderLine).ThenInclude(pl => pl.RawMaterial)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (v is null) return ApiResponse<long>.Fail("Landed-cost voucher not found.");
        if (v.Status != LandedCostVoucherStatus.Draft) return ApiResponse<long>.Fail("Only draft vouchers can be posted.");
        if (v.Charges.Count == 0) return ApiResponse<long>.Fail("Cannot post a voucher with no charges.");

        var grn = v.GoodsReceiptNote;
        if (grn.Lines.Count == 0) return ApiResponse<long>.Fail("The goods receipt has no lines to absorb the cost.");

        var total = v.Charges.Sum(c => c.Amount);
        var rate = grn.PurchaseOrder.ExchangeRate;
        var lineWeights = grn.Lines.OrderBy(l => l.SortOrder).Select(l => new LandedCostAllocator.LineWeight(
            l.PurchaseOrderLine.RawMaterialId, l.PurchaseOrderLine.RawMaterial.Code, l.PurchaseOrderLine.RawMaterial.Name,
            l.ReceivedQuantity, l.ReceivedQuantity * l.PurchaseOrderLine.UnitPrice * rate)).ToList();

        var allocation = LandedCostAllocator.Allocate(lineWeights, total, v.AllocationBasis);

        // Capitalise each allocated share into its raw material's WAC, where stock is still on hand.
        // The portion that can't be absorbed (stock already consumed) goes to COGS.
        var onHandCache = new Dictionary<int, decimal>();
        var rmByLine = grn.Lines.OrderBy(l => l.SortOrder).Select(l => l.PurchaseOrderLine.RawMaterial).ToList();
        var absorbed = 0m;
        for (var i = 0; i < allocation.Count; i++)
        {
            var (line, alloc) = allocation[i];
            if (alloc <= 0m) continue;
            if (!onHandCache.TryGetValue(line.RawMaterialId, out var qty))
            {
                qty = await _stock.GetRawMaterialTotalOnHandAsync(line.RawMaterialId, ct);
                onHandCache[line.RawMaterialId] = qty;
            }
            if (qty > 0m)
            {
                rmByLine[i].WeightedAverageCost += alloc / qty;
                absorbed += alloc;
            }
        }

        var toCogs = total - absorbed;
        if (total > 0m)
        {
            // Phase A2 — on credit → the C&F agent is owed (2115 Accrued Charges), settled later;
            // otherwise settled immediately from cash/bank (existing behaviour).
            var creditAccount = v.IsOnCredit
                ? LedgerAccounts.AccruedChargesPayable
                : (v.PaymentMethod == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank);
            await _journal.PostAsync(
                v.VoucherDate, $"Landed cost {v.Code} on GRN {grn.Code}",
                "LandedCostVoucher", v.Id, v.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.RawMaterialInventory, absorbed, 0m),
                    new JournalPostingLine(LedgerAccounts.CostOfGoodsSold, toCogs, 0m),
                    new JournalPostingLine(creditAccount, 0m, total),
                }, ct);
        }

        v.Status = LandedCostVoucherStatus.Posted;
        v.PostedAt = DateTimeOffset.UtcNow;
        v.PostedBy = _currentUser.UserName;
        _repo.Update(v);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(v.Id, "Landed-cost voucher posted.");
    }
}

// ── Settle (on-credit voucher: pay off Accrued Charges Payable) — Phase A2 ──
public sealed record SettleLandedCostVoucherCommand(long Id, DateOnly SettleDate, string PaymentMethod)
    : IRequest<ApiResponse<long>>;

public sealed class SettleLandedCostVoucherCommandValidator : AbstractValidator<SettleLandedCostVoucherCommand>
{
    public SettleLandedCostVoucherCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.SettleDate).NotEmpty();
        RuleFor(x => x.PaymentMethod).Must(p => Enum.TryParse<PaymentMethod>(p, out _)).WithMessage("Invalid payment method.");
    }
}

internal sealed class SettleLandedCostVoucherCommandHandler : IRequestHandler<SettleLandedCostVoucherCommand, ApiResponse<long>>
{
    private readonly IRepository<LandedCostVoucher, long> _repo;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public SettleLandedCostVoucherCommandHandler(
        IRepository<LandedCostVoucher, long> repo, IJournalPostingService journal,
        ICurrentUserService currentUser, IUnitOfWork uow)
    { _repo = repo; _journal = journal; _currentUser = currentUser; _uow = uow; }

    public async Task<ApiResponse<long>> Handle(SettleLandedCostVoucherCommand cmd, CancellationToken ct)
    {
        var v = await _repo.Query().Include(x => x.Charges)
            .FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (v is null) return ApiResponse<long>.Fail("Landed-cost voucher not found.");
        if (v.Status != LandedCostVoucherStatus.Posted) return ApiResponse<long>.Fail("Only a posted voucher can be settled.");
        if (!v.IsOnCredit) return ApiResponse<long>.Fail("This voucher was paid on posting — nothing to settle.");
        if (v.SettledAt is not null) return ApiResponse<long>.Fail("This voucher is already settled.");

        var method = Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
        var total = v.Charges.Sum(c => c.Amount);
        var cashAccount = method == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;

        // Dr Accrued Charges Payable 2115 / Cr Cash|Bank — clears the agent liability.
        await _journal.PostAsync(
            cmd.SettleDate, $"Settle landed cost {v.Code}", "LandedCostSettlement", v.Id, v.Code,
            new[]
            {
                new JournalPostingLine(LedgerAccounts.AccruedChargesPayable, total, 0m),
                new JournalPostingLine(cashAccount, 0m, total),
            }, ct);

        v.SettledDate = cmd.SettleDate;
        v.SettledAt = DateTimeOffset.UtcNow;
        v.SettledBy = _currentUser.UserName;
        v.SettlementMethod = method;
        _repo.Update(v);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(v.Id, "Landed-cost charges settled.");
    }
}
