namespace BengalTex.ERP.Application.Reports.Dtos;

/// <summary>
/// One movement line on the supplier statement — an Invoice (credit: our payable goes up)
/// or a Payment (debit: our payable goes down). Classic AP convention.
/// </summary>
public record SupplierStatementLineDto(
    DateOnly Date,
    string Type,                  // "Invoice" | "Payment"
    string Reference,             // our SupplierInvoice / Payment code
    string? DocumentRef,          // PO code (+ supplier's own invoice #) for invoices, payment method for payments
    decimal Debit,                // payment amount in base BDT (reduces what we owe)
    decimal Credit,               // invoice amount in base BDT (increases what we owe)
    decimal RunningBalance);      // running BDT payable after this line

/// <summary>
/// Supplier statement = supplier info + opening payable + chronological transactions in
/// window + closing payable. All amounts in base BDT. RunningBalance &gt; 0 = we owe them.
/// Mirror of <see cref="CustomerStatementReportDto"/> on the payables side.
/// </summary>
public record SupplierStatementReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    string? SupplierEmail,
    decimal OpeningBalance,           // BDT payable brought forward (invoices − payments dated < FromDate)
    decimal TotalCredits,             // BDT — invoice credits IN window (payable up)
    decimal TotalDebits,              // BDT — payment debits IN window (payable down)
    decimal ClosingBalance,           // Opening + TotalCredits − TotalDebits
    int LineCount,
    IReadOnlyList<SupplierStatementLineDto> Lines);
