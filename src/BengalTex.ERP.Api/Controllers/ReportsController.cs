using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Reports.Commands;
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

    /// <summary>Per-item stock ledger — running balance of all movements for one item.</summary>
    [HttpGet("stock-ledger")]
    [HasPermission(Permissions.Reports.ViewInventory)]
    public async Task<IActionResult> GetStockLedger(
        [FromQuery] string itemType,
        [FromQuery] int itemId,
        [FromQuery] int? warehouseId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetStockLedgerReportQuery(itemType, itemId, warehouseId, fromDate, toDate), ct);
        return Ok(result);
    }

    /// <summary>Dead / slow-moving stock — on-hand items with no movement for N+ days.</summary>
    [HttpGet("dead-stock")]
    [HasPermission(Permissions.Reports.ViewInventory)]
    public async Task<IActionResult> GetDeadStock(
        [FromQuery] int daysThreshold = 90,
        [FromQuery] string? itemType = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] DateOnly? asOfDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetDeadStockReportQuery(daysThreshold, itemType, warehouseId, asOfDate), ct);
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

    /// <summary>Production cost — actual cost sheet (material+labour+machine+overhead+…) per completed order, with margin when sales-linked.</summary>
    [HttpGet("production-cost")]
    [HasPermission(Permissions.Reports.ViewProduction)]
    public async Task<IActionResult> GetProductionCost(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] int? productId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetProductionCostReportQuery(fromDate, toDate, productId), ct));

    /// <summary>Wastage variance — BOM standard wastage allowance vs actual wastage per completed production order.</summary>
    [HttpGet("wastage-variance")]
    [HasPermission(Permissions.Reports.ViewProduction)]
    public async Task<IActionResult> GetWastageVariance(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] int? productId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetWastageVarianceReportQuery(fromDate, toDate, productId), ct));

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

    /// <summary>Customer Statement of Account — opening balance + chronological invoices/receipts + running balance + closing.</summary>
    [HttpGet("customer-statement")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> GetCustomerStatement(
        [FromQuery] int customerId,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCustomerStatementQuery(customerId, fromDate, toDate), ct));

    /// <summary>Supplier Statement of Account — opening payable + chronological invoices/payments + running balance + closing (AP mirror).</summary>
    [HttpGet("supplier-statement")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> GetSupplierStatement(
        [FromQuery] int supplierId,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSupplierStatementQuery(supplierId, fromDate, toDate), ct));

    /// <summary>Email the customer their statement for the window as an attached PDF.</summary>
    [HttpPost("customer-statement/email")]
    [HasPermission(Permissions.Emails.Send)]
    public async Task<IActionResult> EmailCustomerStatement(
        [FromBody] EmailStatementRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SendCustomerStatementEmailCommand(
            request.PartyId, request.FromDate, request.ToDate,
            request.ToAddresses, request.CcAddresses, request.Subject, request.HtmlBody), ct));

    /// <summary>Email the supplier their payable statement for the window as an attached PDF.</summary>
    [HttpPost("supplier-statement/email")]
    [HasPermission(Permissions.Emails.Send)]
    public async Task<IActionResult> EmailSupplierStatement(
        [FromBody] EmailStatementRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SendSupplierStatementEmailCommand(
            request.PartyId, request.FromDate, request.ToDate,
            request.ToAddresses, request.CcAddresses, request.Subject, request.HtmlBody), ct));
}

public record EmailStatementRequest(
    int PartyId,                       // CustomerId or SupplierId depending on endpoint
    DateOnly? FromDate,
    DateOnly? ToDate,
    string ToAddresses,
    string? CcAddresses,
    string Subject,
    string HtmlBody);
