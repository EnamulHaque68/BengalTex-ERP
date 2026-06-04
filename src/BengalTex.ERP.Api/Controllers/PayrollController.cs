using System.Globalization;
using System.Text;
using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payroll.Commands;
using BengalTex.ERP.Application.Payroll.Queries;
using BengalTex.ERP.Shared.Common;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayrollController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Payroll.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPayslipsQuery(parameters, year, month, employeeId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Payroll.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPayslipByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Enriched payslip for the printable salary-slip page (employee + bank + company).</summary>
    [HttpGet("{id:long}/print-data")]
    [HasPermission(Permissions.Payroll.View)]
    public async Task<IActionResult> GetForPrint(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetPayslipForPrintQuery(id), ct));

    /// <summary>Generate draft payslips for all active employees for a month.</summary>
    [HttpPost("generate")]
    [HasPermission(Permissions.Payroll.Process)]
    public async Task<IActionResult> Generate([FromBody] GeneratePayrollRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new GeneratePayrollCommand(request.Year, request.Month), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Payroll.Process)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePayslipRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdatePayslipCommand(
            id, request.OvertimeAmount, request.Allowances, request.Deductions,
            request.HouseRent, request.Medical, request.Transport, request.FoodAllowance, request.FestivalBonus,
            request.PfEmployee, request.PfEmployer, request.IncomeTax, request.LoanDeduction,
            request.Notes), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/mark-paid")]
    [HasPermission(Permissions.Payroll.Process)]
    public async Task<IActionResult> MarkPaid(long id, [FromBody] MarkPayslipPaidRequest? request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new MarkPayslipPaidCommand(id, request?.PaymentMethod), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Payroll.Process)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePayslipCommand(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Download bank-disbursement CSV for a payroll month. Format suits Bangladesh
    /// EFT batch upload (BEFTN-friendly columns: bank, branch, account#, routing#, amount).
    /// </summary>
    [HttpGet("bank-advice")]
    [HasPermission(Permissions.Payroll.ExportBankAdvice)]
    public async Task<IActionResult> BankAdvice(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBankAdviceQuery(year, month), ct);
        if (!result.Success || result.Data is null) return Ok(result);

        var sb = new StringBuilder();
        sb.AppendLine("EmployeeCode,EmployeeName,BankName,BranchName,AccountNumber,RoutingNumber,NetPay");
        foreach (var r in result.Data)
        {
            sb.Append(Csv(r.EmployeeCode)).Append(',')
              .Append(Csv(r.EmployeeName)).Append(',')
              .Append(Csv(r.BankName)).Append(',')
              .Append(Csv(r.BranchName)).Append(',')
              .Append(Csv(r.AccountNumber)).Append(',')
              .Append(Csv(r.RoutingNumber)).Append(',')
              .Append(r.NetPay.ToString("0.00", CultureInfo.InvariantCulture))
              .AppendLine();
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BankAdvice-{year:0000}-{month:00}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var needsQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuote ? $"\"{escaped}\"" : escaped;
    }
}

public record GeneratePayrollRequest(int Year, int Month);

public record MarkPayslipPaidRequest(string? PaymentMethod);

public record UpdatePayslipRequest(
    decimal OvertimeAmount,
    decimal Allowances,
    decimal Deductions,
    decimal HouseRent,
    decimal Medical,
    decimal Transport,
    decimal FoodAllowance,
    decimal FestivalBonus,
    decimal PfEmployee,
    decimal PfEmployer,
    decimal IncomeTax,
    decimal LoanDeduction,
    string? Notes);
