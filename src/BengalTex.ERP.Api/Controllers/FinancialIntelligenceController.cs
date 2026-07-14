using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.Intelligence;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Phase A8 — Financial Intelligence: liquidity/profitability/efficiency ratios, AR/AP aging, and
/// the P&amp;L trend, all computed read-only over the posted GL and open invoices.
/// </summary>
[ApiController]
[Route("api/financial-intelligence")]
[Authorize]
public class FinancialIntelligenceController : ControllerBase
{
    private readonly IMediator _mediator;
    public FinancialIntelligenceController(IMediator mediator) => _mediator = mediator;

    /// <summary>Liquidity / profitability / efficiency / leverage ratios (BS as-of + P&L period).</summary>
    [HttpGet("kpis")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> Kpis(
        [FromQuery] DateOnly? asOfDate = null, [FromQuery] DateOnly? fromDate = null, [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetFinancialKpisQuery(asOfDate, fromDate, toDate), ct));

    /// <summary>AR / AP aging buckets (0-30 / 31-60 / 61-90 / 90+) per customer / supplier.</summary>
    [HttpGet("ar-ap-aging")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> ArApAging([FromQuery] DateOnly? asOfDate = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetArApAgingQuery(asOfDate), ct));

    /// <summary>Monthly revenue / expense / net profit trend (last N months).</summary>
    [HttpGet("profit-trend")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> ProfitTrend([FromQuery] int months = 12, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetProfitTrendQuery(months), ct));
}
