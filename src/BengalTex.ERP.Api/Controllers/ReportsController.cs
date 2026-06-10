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

    [HttpGet("margin")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> GetMargin(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] int? customerId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMarginReportQuery(fromDate, toDate, customerId), ct);
        return Ok(result);
    }

    /// <summary>WIP — every Production Order currently in progress, with stage progress + overdue flag.</summary>
    [HttpGet("wip")]
    [HasPermission(Permissions.Reports.ViewProduction)]
    public async Task<IActionResult> GetWip(CancellationToken ct)
        => Ok(await _mediator.Send(new GetWipReportQuery(), ct));

    /// <summary>Production summary — per-product output rollup for completed orders in the date window.</summary>
    [HttpGet("production-summary")]
    [HasPermission(Permissions.Reports.ViewProduction)]
    public async Task<IActionResult> GetProductionSummary(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] int? productId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetProductionSummaryReportQuery(fromDate, toDate, productId), ct));

    /// <summary>Operator productivity — per-employee rollup of completed Job Card output, reject rate, units-per-hour.</summary>
    [HttpGet("operator-productivity")]
    [HasPermission(Permissions.Reports.ViewProduction)]
    public async Task<IActionResult> GetOperatorProductivity(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetOperatorProductivityReportQuery(fromDate, toDate), ct));

    /// <summary>Machine productivity — per-machine rollup of throughput, reject rate, units-per-hour.</summary>
    [HttpGet("machine-productivity")]
    [HasPermission(Permissions.Reports.ViewProduction)]
    public async Task<IActionResult> GetMachineProductivity(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetMachineProductivityReportQuery(fromDate, toDate), ct));

    /// <summary>Buyer order book — per-customer rollup of active sales orders + outstanding invoices.</summary>
    [HttpGet("buyer-order-book")]
    [HasPermission(Permissions.Reports.ViewSales)]
    public async Task<IActionResult> GetBuyerOrderBook(
        [FromQuery] int? customerId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBuyerOrderBookReportQuery(customerId), ct));

    /// <summary>EPB Export Register — every foreign-currency invoice in the date range, in Form-N shape.</summary>
    [HttpGet("epb-export-register")]
    [HasPermission(Permissions.Reports.ViewSales)]
    public async Task<IActionResult> GetEpbExportRegister(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] bool pendingFormExpOnly = false,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetEpbExportRegisterQuery(fromDate, toDate, pendingFormExpOnly), ct));
}
