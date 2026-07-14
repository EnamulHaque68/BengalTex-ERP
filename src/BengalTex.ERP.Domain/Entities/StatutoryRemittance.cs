using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Phase A5b — a remittance of a withheld statutory liability to the government / fund on a treasury
/// challan. Clears the accumulated payable raised by payroll (AIT/PF) and supplier-payment withholding
/// (AIT/VDS): posts <c>Dr 2160|2170|2135 / Cr Cash|Bank</c>. One row per challan.
/// </summary>
public class StatutoryRemittance : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;     // "SR-####"

    public StatutoryTaxType TaxType { get; set; }

    /// <summary>The period the remittance covers (informational — YYYY + 1-12).</summary>
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }

    /// <summary>Amount remitted, in BDT.</summary>
    public decimal Amount { get; set; }

    public DateOnly RemittanceDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>Treasury challan / a-challan number.</summary>
    public string? ChallanNo { get; set; }

    public string? Notes { get; set; }
}

public enum StatutoryTaxType
{
    Ait = 1,            // income tax withheld at source → 2160 AIT Payable
    Vds = 2,            // VAT deducted at source → 2170 VDS Payable
    ProvidentFund = 3   // employee + employer PF → 2135 PF Payable
}
