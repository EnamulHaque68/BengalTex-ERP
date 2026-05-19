using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Reports.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stock-summary")]
    [HasPermission(Permissions.Reports.ViewInventory)]
    public async Task<IActionResult> GetStockSummary(
        [FromQuery] string? itemType = null,
        [FromQuery] int? warehouseId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStockSummaryReportQuery(itemType, warehouseId), ct);
        return Ok(result);
    }

    [HttpGet("ar-ageing")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> GetArAgeing(
        [FromQuery] DateOnly? asOfDate = null,
        [FromQuery] int? customerId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetArAgeingReportQuery(asOfDate, customerId), ct);
        return Ok(result);
    }

    [HttpGet("ap-ageing")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> GetApAgeing(
        [FromQuery] DateOnly? asOfDate = null,
        [FromQuery] int? supplierId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetApAgeingReportQuery(asOfDate, supplierId), ct);
        return Ok(result);
    }

    [HttpGet("sales-summary")]
    [HasPermission(Permissions.Reports.ViewSales)]
    public async Task<IActionResult> GetSalesSummary(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] int? customerId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSalesSummaryReportQuery(fromDate, toDate, customerId), ct);
        return Ok(result);
    }

    [HttpGet("dashboard-kpis")]
    [HasPermission(Permissions.Dashboard.ViewOwner)]
    public async Task<IActionResult> GetDashboardKpis(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardKpisQuery(), ct);
        return Ok(result);
    }

    [HttpGet("vat-summary")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> GetVatSummary(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetVatSummaryReportQuery(fromDate, toDate), ct);
        return Ok(result);
    }
}
