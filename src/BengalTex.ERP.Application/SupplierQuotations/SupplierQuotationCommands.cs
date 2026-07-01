using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.PurchaseOrder.Commands;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierQuotations;

internal static class SupplierQuotationRules
{
    public static void ApplyLines<T>(AbstractValidator<T> v, Func<T, IReadOnlyList<SupplierQuotationLineInput>> lines)
    {
        v.RuleFor(x => lines(x)).NotEmpty().WithMessage("Add at least one line.");
        v.RuleForEach(x => lines(x)).ChildRules(l =>
        {
            l.RuleFor(i => i.RawMaterialId).GreaterThan(0);
            l.RuleFor(i => i.Quantity).GreaterThan(0);
            l.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
            l.RuleFor(i => i.LeadTimeDays).GreaterThanOrEqualTo(0).When(i => i.LeadTimeDays.HasValue);
            l.RuleFor(i => i.LineNotes).MaximumLength(1000);
        });
        v.RuleFor(x => lines(x))
            .Must(ls => ls.Select(l => l.RawMaterialId).Distinct().Count() == ls.Count)
            .WithMessage("The same raw material appears more than once.")
            .When(x => lines(x) is { Count: > 0 });
    }

    public static List<SupplierQuotationLine> Build(IReadOnlyList<SupplierQuotationLineInput> lines) =>
        lines.Select((l, i) => new SupplierQuotationLine
        {
            RawMaterialId = l.RawMaterialId,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LeadTimeDays = l.LeadTimeDays,
            SortOrder = i,
            LineNotes = string.IsNullOrWhiteSpace(l.LineNotes) ? null : l.LineNotes.Trim()
        }).ToList();

    public static async Task<string?> ValidateRefs(
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IRepository<Domain.Entities.Currency> currencyRepo,
        IRepository<Domain.Entities.RawMaterial> rmRepo,
        int supplierId, int currencyId, IReadOnlyList<SupplierQuotationLineInput> lines, CancellationToken ct)
    {
        if (await supplierRepo.GetByIdAsync(supplierId, ct) is null) return "Supplier not found.";
        if (await currencyRepo.GetByIdAsync(currencyId, ct) is null) return "Currency not found.";
        var rmIds = lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var count = await rmRepo.Query().CountAsync(r => rmIds.Contains(r.Id), ct);
        return count != rmIds.Count ? "One or more raw materials not found." : null;
    }
}

// ── Create ──
public sealed record CreateSupplierQuotationCommand(
    DateOnly QuotationDate, int SupplierId, long? PurchaseRequisitionId, int CurrencyId, decimal ExchangeRate,
    DateOnly? ValidUntil, string? Notes, IReadOnlyList<SupplierQuotationLineInput> Lines) : IRequest<ApiResponse<long>>;

public sealed class CreateSupplierQuotationCommandValidator : AbstractValidator<CreateSupplierQuotationCommand>
{
    public CreateSupplierQuotationCommandValidator()
    {
        RuleFor(x => x.QuotationDate).NotEmpty();
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
        SupplierQuotationRules.ApplyLines(this, x => x.Lines);
    }
}

internal sealed class CreateSupplierQuotationCommandHandler : IRequestHandler<CreateSupplierQuotationCommand, ApiResponse<long>>
{
    private readonly IRepository<SupplierQuotation, long> _repo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IRepository<PurchaseRequisition, long> _prRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateSupplierQuotationCommandHandler(
        IRepository<SupplierQuotation, long> repo, IRepository<Domain.Entities.Supplier> supplierRepo,
        IRepository<Domain.Entities.Currency> currencyRepo, IRepository<Domain.Entities.RawMaterial> rmRepo,
        IRepository<PurchaseRequisition, long> prRepo,
        IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _supplierRepo = supplierRepo; _currencyRepo = currencyRepo; _rmRepo = rmRepo; _prRepo = prRepo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<long>> Handle(CreateSupplierQuotationCommand cmd, CancellationToken ct)
    {
        var err = await SupplierQuotationRules.ValidateRefs(_supplierRepo, _currencyRepo, _rmRepo, cmd.SupplierId, cmd.CurrencyId, cmd.Lines, ct);
        if (err is not null) return ApiResponse<long>.Fail(err);

        // Workflow lock — can't start an RFQ on a requisition that was already converted directly to a PO.
        if (cmd.PurchaseRequisitionId.HasValue)
        {
            var pr = await _prRepo.GetByIdAsync(cmd.PurchaseRequisitionId.Value, ct);
            if (pr is null) return ApiResponse<long>.Fail("Purchase requisition not found.");
            if (pr.Status == PurchaseRequisitionStatus.Converted)
                return ApiResponse<long>.Fail(
                    "This requisition was already converted directly to a PO — the RFQ workflow is not available for it.");
            if (pr.Status is PurchaseRequisitionStatus.Cancelled or PurchaseRequisitionStatus.Rejected)
                return ApiResponse<long>.Fail($"Cannot quote against a {pr.Status} requisition.");
        }

        var e = new SupplierQuotation
        {
            Code = await _numbering.NextAsync("SQ", null, ct),
            QuotationDate = cmd.QuotationDate,
            SupplierId = cmd.SupplierId,
            PurchaseRequisitionId = cmd.PurchaseRequisitionId,
            CurrencyId = cmd.CurrencyId,
            ExchangeRate = cmd.ExchangeRate,
            ValidUntil = cmd.ValidUntil,
            Status = SupplierQuotationStatus.Draft,
            Notes = cmd.Notes,
            Lines = SupplierQuotationRules.Build(cmd.Lines)
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(e.Id, "Supplier quotation created.");
    }
}

// ── Update (draft only) ──
public sealed record UpdateSupplierQuotationCommand(
    long Id, DateOnly QuotationDate, int SupplierId, long? PurchaseRequisitionId, int CurrencyId, decimal ExchangeRate,
    DateOnly? ValidUntil, string? Notes, IReadOnlyList<SupplierQuotationLineInput> Lines) : IRequest<ApiResponse<long>>;

public sealed class UpdateSupplierQuotationCommandValidator : AbstractValidator<UpdateSupplierQuotationCommand>
{
    public UpdateSupplierQuotationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.QuotationDate).NotEmpty();
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
        SupplierQuotationRules.ApplyLines(this, x => x.Lines);
    }
}

internal sealed class UpdateSupplierQuotationCommandHandler : IRequestHandler<UpdateSupplierQuotationCommand, ApiResponse<long>>
{
    private readonly IRepository<SupplierQuotation, long> _repo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IRepository<Domain.Entities.Currency> _currencyRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IUnitOfWork _uow;

    public UpdateSupplierQuotationCommandHandler(
        IRepository<SupplierQuotation, long> repo, IRepository<Domain.Entities.Supplier> supplierRepo,
        IRepository<Domain.Entities.Currency> currencyRepo, IRepository<Domain.Entities.RawMaterial> rmRepo, IUnitOfWork uow)
    { _repo = repo; _supplierRepo = supplierRepo; _currencyRepo = currencyRepo; _rmRepo = rmRepo; _uow = uow; }

    public async Task<ApiResponse<long>> Handle(UpdateSupplierQuotationCommand cmd, CancellationToken ct)
    {
        var e = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (e is null) return ApiResponse<long>.Fail("Supplier quotation not found.");
        if (e.Status != SupplierQuotationStatus.Draft) return ApiResponse<long>.Fail("Only draft quotations can be edited.");

        var err = await SupplierQuotationRules.ValidateRefs(_supplierRepo, _currencyRepo, _rmRepo, cmd.SupplierId, cmd.CurrencyId, cmd.Lines, ct);
        if (err is not null) return ApiResponse<long>.Fail(err);

        e.QuotationDate = cmd.QuotationDate;
        e.SupplierId = cmd.SupplierId;
        e.PurchaseRequisitionId = cmd.PurchaseRequisitionId;
        e.CurrencyId = cmd.CurrencyId;
        e.ExchangeRate = cmd.ExchangeRate;
        e.ValidUntil = cmd.ValidUntil;
        e.Notes = cmd.Notes;
        e.Lines.Clear();
        foreach (var l in SupplierQuotationRules.Build(cmd.Lines)) e.Lines.Add(l);

        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(e.Id, "Supplier quotation updated.");
    }
}

// ── Delete (draft only) ──
public sealed record DeleteSupplierQuotationCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteSupplierQuotationCommandHandler : IRequestHandler<DeleteSupplierQuotationCommand, ApiResponse>
{
    private readonly IRepository<SupplierQuotation, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteSupplierQuotationCommandHandler(IRepository<SupplierQuotation, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteSupplierQuotationCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Supplier quotation not found.");
        if (e.Status != SupplierQuotationStatus.Draft) return ApiResponse.Fail("Only draft quotations can be deleted.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Supplier quotation deleted.");
    }
}

// ── Submit (Draft → Submitted) ──
public sealed record SubmitSupplierQuotationCommand(long Id) : IRequest<ApiResponse<long>>;

internal sealed class SubmitSupplierQuotationCommandHandler : IRequestHandler<SubmitSupplierQuotationCommand, ApiResponse<long>>
{
    private readonly IRepository<SupplierQuotation, long> _repo;
    private readonly IUnitOfWork _uow;
    public SubmitSupplierQuotationCommandHandler(IRepository<SupplierQuotation, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<long>> Handle(SubmitSupplierQuotationCommand cmd, CancellationToken ct)
    {
        var e = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (e is null) return ApiResponse<long>.Fail("Supplier quotation not found.");
        if (e.Status != SupplierQuotationStatus.Draft) return ApiResponse<long>.Fail("Only draft quotations can be submitted.");
        if (e.Lines.Count == 0) return ApiResponse<long>.Fail("Cannot submit a quotation with no lines.");
        e.Status = SupplierQuotationStatus.Submitted;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(e.Id, "Supplier quotation submitted.");
    }
}

// ── Reject (Submitted → Rejected) ──
public sealed record RejectSupplierQuotationCommand(long Id) : IRequest<ApiResponse<long>>;

internal sealed class RejectSupplierQuotationCommandHandler : IRequestHandler<RejectSupplierQuotationCommand, ApiResponse<long>>
{
    private readonly IRepository<SupplierQuotation, long> _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    public RejectSupplierQuotationCommandHandler(IRepository<SupplierQuotation, long> repo, ICurrentUserService currentUser, IUnitOfWork uow)
    { _repo = repo; _currentUser = currentUser; _uow = uow; }

    public async Task<ApiResponse<long>> Handle(RejectSupplierQuotationCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<long>.Fail("Supplier quotation not found.");
        if (e.Status != SupplierQuotationStatus.Submitted) return ApiResponse<long>.Fail("Only submitted quotations can be rejected.");
        e.Status = SupplierQuotationStatus.Rejected;
        e.DecidedAt = DateTimeOffset.UtcNow;
        e.DecidedBy = _currentUser.UserName;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(e.Id, "Supplier quotation rejected.");
    }
}

// ── Select + convert to PO (Submitted → Selected) ──
public sealed record SelectSupplierQuotationCommand(long Id) : IRequest<ApiResponse<long>>;

internal sealed class SelectSupplierQuotationCommandHandler : IRequestHandler<SelectSupplierQuotationCommand, ApiResponse<long>>
{
    private readonly IRepository<SupplierQuotation, long> _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public SelectSupplierQuotationCommandHandler(
        IRepository<SupplierQuotation, long> repo, ICurrentUserService currentUser, IUnitOfWork uow, IMediator mediator)
    { _repo = repo; _currentUser = currentUser; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<long>> Handle(SelectSupplierQuotationCommand cmd, CancellationToken ct)
    {
        var e = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (e is null) return ApiResponse<long>.Fail("Supplier quotation not found.");
        if (e.Status != SupplierQuotationStatus.Submitted) return ApiResponse<long>.Fail("Only submitted quotations can be selected.");
        if (e.Lines.Count == 0) return ApiResponse<long>.Fail("Cannot select a quotation with no lines.");

        // Duplicate-PO guard — only one winning quotation per requisition (one RFQ → one PO).
        if (e.PurchaseRequisitionId.HasValue)
        {
            var alreadySelected = await _repo.Query().AnyAsync(
                s => s.Id != e.Id && s.PurchaseRequisitionId == e.PurchaseRequisitionId
                  && s.Status == SupplierQuotationStatus.Selected, ct);
            if (alreadySelected)
                return ApiResponse<long>.Fail(
                    "A supplier quotation for this requisition has already been selected — a purchase order already exists.");
        }

        // Create the PO from the winning quote (delegates to the PO create handler, which commits it).
        var poResult = await _mediator.Send(new CreatePurchaseOrderCommand(
            e.SupplierId, DateOnly.FromDateTime(DateTime.UtcNow), null, null,
            $"From supplier quotation {e.Code}", e.CurrencyId, e.ExchangeRate,
            e.Lines.OrderBy(l => l.SortOrder)
                .Select(l => new PurchaseOrderLineInput(l.RawMaterialId, l.Quantity, l.UnitPrice, l.LineNotes))
                .ToList(),
            PurchaseRequisitionId: e.PurchaseRequisitionId, SupplierQuotationId: e.Id), ct);
        if (!poResult.Success || poResult.Data is null)
            return ApiResponse<long>.Fail(poResult.Message ?? "Failed to create the purchase order.");

        e.Status = SupplierQuotationStatus.Selected;
        e.DecidedAt = DateTimeOffset.UtcNow;
        e.DecidedBy = _currentUser.UserName;
        e.ConvertedPurchaseOrderId = poResult.Data.Id;
        e.ConvertedAt = DateTimeOffset.UtcNow;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(poResult.Data.Id, $"Quotation selected — purchase order {poResult.Data.Code} created.");
    }
}
