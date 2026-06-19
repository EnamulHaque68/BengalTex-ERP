using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierQuotations;

// ── List ──
public sealed record GetSupplierQuotationsQuery(
    PagedQueryParameters Parameters, string? Status = null, int? SupplierId = null, long? PurchaseRequisitionId = null)
    : IRequest<ApiResponse<PagedResult<SupplierQuotationListItemDto>>>;

internal sealed class GetSupplierQuotationsQueryHandler
    : IRequestHandler<GetSupplierQuotationsQuery, ApiResponse<PagedResult<SupplierQuotationListItemDto>>>
{
    private readonly IRepository<SupplierQuotation, long> _repo;
    public GetSupplierQuotationsQueryHandler(IRepository<SupplierQuotation, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<SupplierQuotationListItemDto>>> Handle(
        GetSupplierQuotationsQuery req, CancellationToken ct)
    {
        var q = _repo.Query()
            .Include(x => x.Supplier).Include(x => x.Currency).Include(x => x.PurchaseRequisition)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Status) && Enum.TryParse<SupplierQuotationStatus>(req.Status, true, out var s))
            q = q.Where(x => x.Status == s);
        if (req.SupplierId is int sid) q = q.Where(x => x.SupplierId == sid);
        if (req.PurchaseRequisitionId is long prid) q = q.Where(x => x.PurchaseRequisitionId == prid);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search) || x.Supplier.Name.Contains(search));

        q = q.OrderByDescending(x => x.Id);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((req.Parameters.Page - 1) * req.Parameters.PageSize).Take(req.Parameters.PageSize)
            .Select(x => new SupplierQuotationListItemDto(
                x.Id, x.Code, x.QuotationDate, x.Supplier.Name,
                x.PurchaseRequisition != null ? x.PurchaseRequisition.Code : null,
                x.Currency.Code, x.Status.ToString(),
                x.Lines.Count,
                x.Lines.Sum(l => l.Quantity * l.UnitPrice),
                x.Lines.Sum(l => l.Quantity * l.UnitPrice) * x.ExchangeRate))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<SupplierQuotationListItemDto>>.Ok(
            PagedResult<SupplierQuotationListItemDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ── By id ──
public sealed record GetSupplierQuotationByIdQuery(long Id) : IRequest<ApiResponse<SupplierQuotationDto>>;

internal sealed class GetSupplierQuotationByIdQueryHandler
    : IRequestHandler<GetSupplierQuotationByIdQuery, ApiResponse<SupplierQuotationDto>>
{
    private readonly IRepository<SupplierQuotation, long> _repo;
    public GetSupplierQuotationByIdQueryHandler(IRepository<SupplierQuotation, long> repo) => _repo = repo;

    public async Task<ApiResponse<SupplierQuotationDto>> Handle(GetSupplierQuotationByIdQuery req, CancellationToken ct)
    {
        var x = await _repo.Query().AsNoTracking()
            .Include(q => q.Supplier).Include(q => q.Currency).Include(q => q.PurchaseRequisition)
            .Include(q => q.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm.UnitOfMeasure)
            .FirstOrDefaultAsync(q => q.Id == req.Id, ct);
        if (x is null) return ApiResponse<SupplierQuotationDto>.Fail("Supplier quotation not found.");

        var lines = x.Lines.OrderBy(l => l.SortOrder).Select(l => new SupplierQuotationLineDto(
            l.Id, l.RawMaterialId, l.RawMaterial.Code, l.RawMaterial.Name, l.RawMaterial.UnitOfMeasure.Code,
            l.Quantity, l.UnitPrice, l.Quantity * l.UnitPrice, l.LeadTimeDays, l.SortOrder, l.LineNotes)).ToList();
        var totalAmount = lines.Sum(l => l.LineTotal);

        return ApiResponse<SupplierQuotationDto>.Ok(new SupplierQuotationDto(
            x.Id, x.Code, x.QuotationDate, x.SupplierId, x.Supplier.Name,
            x.PurchaseRequisitionId, x.PurchaseRequisition != null ? x.PurchaseRequisition.Code : null,
            x.CurrencyId, x.Currency.Code, x.ExchangeRate, x.ValidUntil, x.Status.ToString(),
            x.DecidedAt, x.DecidedBy, x.ConvertedPurchaseOrderId, x.ConvertedAt,
            x.Notes, totalAmount, totalAmount * x.ExchangeRate, lines));
    }
}

// ── Comparison matrix (competing quotes for one requisition) ──
public sealed record GetQuotationComparisonQuery(long PurchaseRequisitionId)
    : IRequest<ApiResponse<QuotationComparisonDto>>;

internal sealed class GetQuotationComparisonQueryHandler
    : IRequestHandler<GetQuotationComparisonQuery, ApiResponse<QuotationComparisonDto>>
{
    private readonly IRepository<SupplierQuotation, long> _repo;
    private readonly IRepository<PurchaseRequisition, long> _prRepo;
    public GetQuotationComparisonQueryHandler(
        IRepository<SupplierQuotation, long> repo, IRepository<PurchaseRequisition, long> prRepo)
    { _repo = repo; _prRepo = prRepo; }

    public async Task<ApiResponse<QuotationComparisonDto>> Handle(GetQuotationComparisonQuery req, CancellationToken ct)
    {
        var pr = await _prRepo.Query().AsNoTracking()
            .Include(p => p.Lines).ThenInclude(l => l.RawMaterial)
            .FirstOrDefaultAsync(p => p.Id == req.PurchaseRequisitionId, ct);
        if (pr is null) return ApiResponse<QuotationComparisonDto>.Fail("Purchase requisition not found.");

        // Only Submitted/Selected quotes are comparable (drafts/rejected excluded)
        var quotes = await _repo.Query().AsNoTracking()
            .Where(q => q.PurchaseRequisitionId == req.PurchaseRequisitionId
                && (q.Status == SupplierQuotationStatus.Submitted || q.Status == SupplierQuotationStatus.Selected))
            .Include(q => q.Supplier).Include(q => q.Currency).Include(q => q.Lines)
            .ToListAsync(ct);

        // Rows are the requisition's lines (the requirement); quantity from the PR.
        var rows = new List<QuotationComparisonRowDto>();
        var supplierTotals = quotes.ToDictionary(q => q.Id, _ => 0m);

        foreach (var prLine in pr.Lines.OrderBy(l => l.SortOrder))
        {
            var cells = new List<(long qId, bool has, decimal up, decimal upBase, int? lead, decimal lineBase)>();
            foreach (var q in quotes)
            {
                var ql = q.Lines.FirstOrDefault(l => l.RawMaterialId == prLine.RawMaterialId);
                if (ql is null) { cells.Add((q.Id, false, 0m, 0m, null, 0m)); continue; }
                var upBase = ql.UnitPrice * q.ExchangeRate;
                var lineBase = prLine.Quantity * upBase;
                supplierTotals[q.Id] += lineBase;
                cells.Add((q.Id, true, ql.UnitPrice, upBase, ql.LeadTimeDays, lineBase));
            }

            var quoted = cells.Where(c => c.has).ToList();
            var lowestBase = quoted.Count > 0 ? quoted.Min(c => c.upBase) : 0m;

            rows.Add(new QuotationComparisonRowDto(
                prLine.RawMaterialId, prLine.RawMaterial.Code, prLine.RawMaterial.Name, prLine.Quantity,
                cells.Select(c => new QuotationComparisonCellDto(
                    c.qId, c.has, c.up, c.upBase, c.lead, c.lineBase,
                    c.has && c.upBase == lowestBase)).ToList()));
        }

        var lowestTotal = supplierTotals.Count > 0 ? supplierTotals.Values.Min() : 0m;
        var suppliers = quotes.Select(q => new QuotationComparisonSupplierDto(
            q.Id, q.Code, q.Supplier.Name, q.Currency.Code, q.ExchangeRate, q.Status.ToString(), q.ValidUntil,
            supplierTotals[q.Id], supplierTotals[q.Id] == lowestTotal)).ToList();

        return ApiResponse<QuotationComparisonDto>.Ok(new QuotationComparisonDto(
            pr.Id, pr.Code, suppliers, rows));
    }
}
