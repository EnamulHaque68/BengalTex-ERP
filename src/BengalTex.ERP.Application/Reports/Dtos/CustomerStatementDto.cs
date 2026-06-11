namespace BengalTex.ERP.Application.Reports.Dtos;

/// <summary>One movement line on the customer statement — an Invoice (debit) or a Receipt (credit).</summary>
public record CustomerStatementLineDto(
    DateOnly Date,
    string Type,                  // "Invoice" | "Receipt"
    string Reference,             // invoice or receipt code
    string? DocumentRef,          // SO code for invoices, payment method for receipts
    decimal Debit,                // amount in base BDT
    decimal Credit,
    decimal RunningBalance);      // running BDT balance after this line

/// <summary>
/// Customer statement = customer info + opening balance + chronological transactions
/// in window + closing balance. All amounts converted to base BDT so multi-currency
/// invoices sum correctly. Statement window inclusive at both ends.
/// </summary>
public record CustomerStatementReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    string? CustomerEmail,
    decimal OpeningBalance,           // BDT — sum of (invoice debits − receipt credits) dated < FromDate
    decimal TotalDebits,              // BDT — sum of invoice debits IN window
    decimal TotalCredits,             // BDT — sum of receipt credits IN window
    decimal ClosingBalance,           // BDT — Opening + TotalDebits − TotalCredits
    int LineCount,
    IReadOnlyList<CustomerStatementLineDto> Lines);
