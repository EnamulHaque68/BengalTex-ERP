using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.PurchaseOrder.Commands;
using BengalTex.ERP.Application.PurchaseRequisitions.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.PurchaseRequisitions.Commands;

public sealed record PurchaseRequisitionLineInput(
    int RawMaterialId, decimal Quantity, decimal EstimatedUnitPrice, string? LineNotes);

// ───────────────────────────────────────────────────────────────────────────
//   List
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetPurchaseRequisitionsQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    int? DepartmentId = null
) : IRequest<ApiResponse<PagedResult<PurchaseRequisitionDto>>>;

internal sealed class GetPurchaseRequisitionsQueryHandler
    : IRequestHandler<GetPurchaseRequisitionsQuery, ApiResponse<PagedResult<PurchaseRequisitionDto>>>
{
    private readonly IRepository<PurchaseRequisition, long> _repo;
    public GetPurchaseRequisitionsQueryHandler(IRepository<PurchaseRequisition, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<PurchaseRequisitionDto>>> Handle(
        GetPurchaseRequisitionsQuery req, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!string.IsNullOrEmpty(req.Status)
            && Enum.TryParse<PurchaseRequisitionStatus>(req.Status, out var s))
            q = q.Where(x => x.Status == s);
        if (req.DepartmentId.HasValue) q = q.Where(x => x.DepartmentId == req.DepartmentId.Value);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search)
                          || (x.RequestedBy != null && x.RequestedBy.Contains(search))
                          || (x.DepartmentText != null && x.DepartmentText.Contains(search)));

        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .Select(x => new PurchaseRequisitionDto(
                x.Id, x.Code, x.RequisitionDate, x.NeededByDate,
                x.DepartmentId, x.Department != null ? x.Department.Name : null,
                x.DepartmentText, x.RequestedBy, x.Purpose,
                x.Status.ToString(),
                x.Lines.Sum(l => l.Quantity * l.EstimatedUnitPrice),
                x.SubmittedAt, x.SubmittedByUser,
                x.DecidedAt, x.DecidedByUser, x.DecisionNotes,
                x.ConvertedAt, x.ConvertedPurchaseOrderId,
                x.ConvertedPurchaseOrder != null ? x.ConvertedPurchaseOrder.Code : null,
                x.Notes,
                new List<PurchaseRequisitionLineDto>()))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<PurchaseRequisitionDto>>.Ok(
            PagedResult<PurchaseRequisitionDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Get By Id (with lines)
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetPurchaseRequisitionByIdQuery(long Id) : IRequest<ApiResponse<PurchaseRequisitionDto>>;

internal sealed class GetPurchaseRequisitionByIdQueryHandler
    : IRequestHandler<GetPurchaseRequisitionByIdQuery, ApiResponse<PurchaseRequisitionDto>>
{
    private readonly IRepository<PurchaseRequisition, long> _repo;
    public GetPurchaseRequisitionByIdQueryHandler(IRepository<PurchaseRequisition, long> repo) => _repo = repo;

    public async Task<ApiResponse<PurchaseRequisitionDto>> Handle(GetPurchaseRequisitionByIdQuery q, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .Where(x => x.Id == q.Id)
            .Select(x => new PurchaseRequisitionDto(
                x.Id, x.Code, x.RequisitionDate, x.NeededByDate,
                x.DepartmentId, x.Department != null ? x.Department.Name : null,
                x.DepartmentText, x.RequestedBy, x.Purpose,
                x.Status.ToString(),
                x.Lines.Sum(l => l.Quantity * l.EstimatedUnitPrice),
                x.SubmittedAt, x.SubmittedByUser,
                x.DecidedAt, x.DecidedByUser, x.DecisionNotes,
                x.ConvertedAt, x.ConvertedPurchaseOrderId,
                x.ConvertedPurchaseOrder != null ? x.ConvertedPurchaseOrder.Code : null,
                x.Notes,
                x.Lines.OrderBy(l => l.SortOrder).Select(l => new PurchaseRequisitionLineDto(
                    l.Id, l.RawMaterialId, l.RawMaterial.Code, l.RawMaterial.Name,
                    l.RawMaterial.UnitOfMeasure != null ? l.RawMaterial.UnitOfMeasure.Code : null,
                    l.Quantity, l.EstimatedUnitPrice, l.Quantity * l.EstimatedUnitPrice,
                    l.SortOrder, l.LineNotes)).ToList()))
            .FirstOrDefaultAsync(ct);
        return dto is null
            ? ApiResponse<PurchaseRequisitionDto>.Fail("Purchase requisition not found.")
            : ApiResponse<PurchaseRequisitionDto>.Ok(dto);
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Create Draft
// ───────────────────────────────────────────────────────────────────────────
public sealed record CreatePurchaseRequisitionCommand(
    DateOnly RequisitionDate,
    DateOnly? NeededByDate,
    int? DepartmentId,
    string? DepartmentText,
    string? RequestedBy,
    string? Purpose,
    string? Notes,
    IReadOnlyList<PurchaseRequisitionLineInput> Lines
) : IRequest<ApiResponse<long>>;

public sealed class CreatePurchaseRequisitionCommandValidator : AbstractValidator<CreatePurchaseRequisitionCommand>
{
    public CreatePurchaseRequisitionCommandValidator()
    {
        RuleFor(x => x.RequisitionDate).NotEmpty();
        RuleFor(x => x.DepartmentText).MaximumLength(100);
        RuleFor(x => x.RequestedBy).MaximumLength(100);
        RuleFor(x => x.Purpose).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A requisition must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.RawMaterialId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.EstimatedUnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.RawMaterialId).Distinct().Count() == lines.Count)
            .WithMessage("The same raw material appears more than once.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreatePurchaseRequisitionCommandHandler
    : IRequestHandler<CreatePurchaseRequisitionCommand, ApiResponse<long>>
{
    private readonly IRepository<PurchaseRequisition, long> _repo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreatePurchaseRequisitionCommandHandler(
        IRepository<PurchaseRequisition, long> repo,
        IRepository<Domain.Entities.RawMaterial> rmRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo; _rmRepo = rmRepo; _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long>> Handle(CreatePurchaseRequisitionCommand cmd, CancellationToken ct)
    {
        var rmIds = cmd.Lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var existing = await _rmRepo.Query().CountAsync(r => rmIds.Contains(r.Id), ct);
        if (existing != rmIds.Count) return ApiResponse<long>.Fail("One or more raw materials not found.");

        var code = await _numbering.NextAsync("PR", null, ct);
        var entity = new PurchaseRequisition
        {
            Code = code,
            RequisitionDate = cmd.RequisitionDate,
            NeededByDate = cmd.NeededByDate,
            DepartmentId = cmd.DepartmentId,
            DepartmentText = string.IsNullOrWhiteSpace(cmd.DepartmentText) ? null : cmd.DepartmentText.Trim(),
            RequestedBy = string.IsNullOrWhiteSpace(cmd.RequestedBy) ? null : cmd.RequestedBy.Trim(),
            Purpose = string.IsNullOrWhiteSpace(cmd.Purpose) ? null : cmd.Purpose.Trim(),
            Status = PurchaseRequisitionStatus.Draft,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            Lines = cmd.Lines.Select((l, i) => new PurchaseRequisitionLine
            {
                RawMaterialId = l.RawMaterialId,
                Quantity = l.Quantity,
                EstimatedUnitPrice = l.EstimatedUnitPrice,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, "Purchase requisition draft created.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Update (Draft only)
// ───────────────────────────────────────────────────────────────────────────
public sealed record UpdatePurchaseRequisitionCommand(
    long Id,
    DateOnly RequisitionDate,
    DateOnly? NeededByDate,
    int? DepartmentId,
    string? DepartmentText,
    string? RequestedBy,
    string? Purpose,
    string? Notes,
    IReadOnlyList<PurchaseRequisitionLineInput> Lines
) : IRequest<ApiResponse>;

public sealed class UpdatePurchaseRequisitionCommandValidator : AbstractValidator<UpdatePurchaseRequisitionCommand>
{
    public UpdatePurchaseRequisitionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.DepartmentText).MaximumLength(100);
        RuleFor(x => x.RequestedBy).MaximumLength(100);
        RuleFor(x => x.Purpose).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.RawMaterialId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.EstimatedUnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
    }
}

internal sealed class UpdatePurchaseRequisitionCommandHandler
    : IRequestHandler<UpdatePurchaseRequisitionCommand, ApiResponse>
{
    private readonly IRepository<PurchaseRequisition, long> _repo;
    private readonly IRepository<PurchaseRequisitionLine, long> _lineRepo;
    private readonly IUnitOfWork _uow;

    public UpdatePurchaseRequisitionCommandHandler(
        IRepository<PurchaseRequisition, long> repo,
        IRepository<PurchaseRequisitionLine, long> lineRepo,
        IUnitOfWork uow)
    {
        _repo = repo; _lineRepo = lineRepo; _uow = uow;
    }

    public async Task<ApiResponse> Handle(UpdatePurchaseRequisitionCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.Query()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id, ct);
        if (entity is null) return ApiResponse.Fail("Purchase requisition not found.");
        if (entity.Status != PurchaseRequisitionStatus.Draft)
            return ApiResponse.Fail($"Cannot edit a {entity.Status} requisition.");

        entity.RequisitionDate = cmd.RequisitionDate;
        entity.NeededByDate = cmd.NeededByDate;
        entity.DepartmentId = cmd.DepartmentId;
        entity.DepartmentText = string.IsNullOrWhiteSpace(cmd.DepartmentText) ? null : cmd.DepartmentText.Trim();
        entity.RequestedBy = string.IsNullOrWhiteSpace(cmd.RequestedBy) ? null : cmd.RequestedBy.Trim();
        entity.Purpose = string.IsNullOrWhiteSpace(cmd.Purpose) ? null : cmd.Purpose.Trim();
        entity.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();

        foreach (var oldLine in entity.Lines.ToList()) _lineRepo.Remove(oldLine);
        entity.Lines.Clear();
        foreach (var (l, i) in cmd.Lines.Select((l, i) => (l, i)))
        {
            entity.Lines.Add(new PurchaseRequisitionLine
            {
                PurchaseRequisitionId = entity.Id,
                RawMaterialId = l.RawMaterialId,
                Quantity = l.Quantity,
                EstimatedUnitPrice = l.EstimatedUnitPrice,
                SortOrder = i,
                LineNotes = l.LineNotes
            });
        }

        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Purchase requisition updated.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Delete (Draft only)
// ───────────────────────────────────────────────────────────────────────────
public sealed record DeletePurchaseRequisitionCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeletePurchaseRequisitionCommandHandler
    : IRequestHandler<DeletePurchaseRequisitionCommand, ApiResponse>
{
    private readonly IRepository<PurchaseRequisition, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeletePurchaseRequisitionCommandHandler(IRepository<PurchaseRequisition, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeletePurchaseRequisitionCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Purchase requisition not found.");
        if (e.Status != PurchaseRequisitionStatus.Draft)
            return ApiResponse.Fail($"Cannot delete a {e.Status} requisition. Cancel it first.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Purchase requisition deleted.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   State transitions
// ───────────────────────────────────────────────────────────────────────────
public sealed record SubmitPurchaseRequisitionCommand(long Id) : IRequest<ApiResponse>;
public sealed record ApprovePurchaseRequisitionCommand(long Id, string? Notes = null) : IRequest<ApiResponse>;
public sealed record RejectPurchaseRequisitionCommand(long Id, string? Notes = null) : IRequest<ApiResponse>;
public sealed record CancelPurchaseRequisitionCommand(long Id) : IRequest<ApiResponse>;

internal sealed class PurchaseRequisitionStateHandlers :
    IRequestHandler<SubmitPurchaseRequisitionCommand, ApiResponse>,
    IRequestHandler<ApprovePurchaseRequisitionCommand, ApiResponse>,
    IRequestHandler<RejectPurchaseRequisitionCommand, ApiResponse>,
    IRequestHandler<CancelPurchaseRequisitionCommand, ApiResponse>
{
    private readonly IRepository<PurchaseRequisition, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public PurchaseRequisitionStateHandlers(
        IRepository<PurchaseRequisition, long> repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _repo = repo; _uow = uow; _currentUser = currentUser;
    }

    public async Task<ApiResponse> Handle(SubmitPurchaseRequisitionCommand req, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(req.Id, ct);
        if (e is null) return ApiResponse.Fail("Purchase requisition not found.");
        if (e.Status != PurchaseRequisitionStatus.Draft)
            return ApiResponse.Fail($"Cannot submit a {e.Status} requisition.");
        e.Status = PurchaseRequisitionStatus.Submitted;
        e.SubmittedAt = DateTimeOffset.UtcNow;
        e.SubmittedByUser = _currentUser.UserName ?? "system";
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"{e.Code} submitted for approval.");
    }

    public async Task<ApiResponse> Handle(ApprovePurchaseRequisitionCommand req, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(req.Id, ct);
        if (e is null) return ApiResponse.Fail("Purchase requisition not found.");
        if (e.Status != PurchaseRequisitionStatus.Submitted)
            return ApiResponse.Fail($"Only Submitted requisitions can be approved (current: {e.Status}).");
        e.Status = PurchaseRequisitionStatus.Approved;
        e.DecidedAt = DateTimeOffset.UtcNow;
        e.DecidedByUser = _currentUser.UserName ?? "system";
        e.DecisionNotes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"{e.Code} approved.");
    }

    public async Task<ApiResponse> Handle(RejectPurchaseRequisitionCommand req, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(req.Id, ct);
        if (e is null) return ApiResponse.Fail("Purchase requisition not found.");
        if (e.Status != PurchaseRequisitionStatus.Submitted)
            return ApiResponse.Fail($"Only Submitted requisitions can be rejected (current: {e.Status}).");
        e.Status = PurchaseRequisitionStatus.Rejected;
        e.DecidedAt = DateTimeOffset.UtcNow;
        e.DecidedByUser = _currentUser.UserName ?? "system";
        e.DecisionNotes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"{e.Code} rejected.");
    }

    public async Task<ApiResponse> Handle(CancelPurchaseRequisitionCommand req, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(req.Id, ct);
        if (e is null) return ApiResponse.Fail("Purchase requisition not found.");
        if (e.Status == PurchaseRequisitionStatus.Converted)
            return ApiResponse.Fail("Cannot cancel a converted requisition (already became a PO).");
        if (e.Status == PurchaseRequisitionStatus.Cancelled)
            return ApiResponse.Fail("Requisition already cancelled.");
        e.Status = PurchaseRequisitionStatus.Cancelled;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"{e.Code} cancelled.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Convert Approved PR → real PurchaseOrder
// ───────────────────────────────────────────────────────────────────────────
public sealed record ConvertPurchaseRequisitionToPoCommand(
    long PurchaseRequisitionId,
    int SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    int? DeliveryWarehouseId,
    int CurrencyId,
    decimal ExchangeRate,
    string? Notes,
    IReadOnlyList<ConvertPrLinePriceInput> LinePrices    // user-supplied final unit prices (default = estimated)
) : IRequest<ApiResponse<long>>;

public sealed record ConvertPrLinePriceInput(long PurchaseRequisitionLineId, decimal UnitPrice);

internal sealed class ConvertPurchaseRequisitionToPoCommandHandler
    : IRequestHandler<ConvertPurchaseRequisitionToPoCommand, ApiResponse<long>>
{
    private readonly IRepository<PurchaseRequisition, long> _repo;
    private readonly IRepository<SupplierQuotation, long> _sqRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ConvertPurchaseRequisitionToPoCommandHandler(
        IRepository<PurchaseRequisition, long> repo,
        IRepository<SupplierQuotation, long> sqRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo; _sqRepo = sqRepo; _uow = uow; _mediator = mediator;
    }

    public async Task<ApiResponse<long>> Handle(ConvertPurchaseRequisitionToPoCommand cmd, CancellationToken ct)
    {
        var pr = await _repo.Query()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == cmd.PurchaseRequisitionId, ct);
        if (pr is null) return ApiResponse<long>.Fail("Purchase requisition not found.");
        if (pr.Status != PurchaseRequisitionStatus.Approved)
            return ApiResponse<long>.Fail($"Only Approved requisitions can be converted (current: {pr.Status}).");
        if (pr.ConvertedPurchaseOrderId.HasValue)
            return ApiResponse<long>.Fail("This requisition has already been converted.");

        // Workflow lock — once an RFQ is started (a non-rejected supplier quotation exists for this
        // requisition), the direct-conversion path is closed; the PO must come from the winning quote.
        var rfqStarted = await _sqRepo.Query()
            .AnyAsync(s => s.PurchaseRequisitionId == cmd.PurchaseRequisitionId
                        && s.Status != SupplierQuotationStatus.Rejected, ct);
        if (rfqStarted)
            return ApiResponse<long>.Fail(
                "An RFQ has been started for this requisition — convert it by selecting the winning " +
                "supplier quotation, not a direct conversion.");

        var priceByLineId = cmd.LinePrices.ToDictionary(x => x.PurchaseRequisitionLineId, x => x.UnitPrice);
        var poLines = pr.Lines.OrderBy(l => l.SortOrder)
            .Select(l => new PurchaseOrderLineInput(
                l.RawMaterialId,
                l.Quantity,
                priceByLineId.TryGetValue(l.Id, out var p) ? p : l.EstimatedUnitPrice,
                l.LineNotes))
            .ToList();

        var poResult = await _mediator.Send(new CreatePurchaseOrderCommand(
            cmd.SupplierId, cmd.OrderDate, cmd.ExpectedDeliveryDate, cmd.DeliveryWarehouseId,
            string.IsNullOrWhiteSpace(cmd.Notes) ? $"Converted from PR {pr.Code}" : cmd.Notes,
            cmd.CurrencyId, cmd.ExchangeRate,
            poLines, PurchaseRequisitionId: pr.Id), ct);

        if (!poResult.Success || poResult.Data is null)
            return ApiResponse<long>.Fail(poResult.Message ?? "Failed to create purchase order from requisition.");

        pr.Status = PurchaseRequisitionStatus.Converted;
        pr.ConvertedAt = DateTimeOffset.UtcNow;
        pr.ConvertedPurchaseOrderId = poResult.Data.Id;
        _repo.Update(pr);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(poResult.Data.Id, $"PR {pr.Code} converted to PO {poResult.Data.Code}.");
    }
}
