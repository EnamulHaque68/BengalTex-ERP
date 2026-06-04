using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Audit log of every outbound email sent via the email gateway. Records who/what/when +
/// outcome (delivery is fire-and-forget once SMTP accepts; <see cref="Status"/> tracks
/// whether the send call itself threw — actual delivery status comes from the receiving
/// mail server which we don't track here). Polymorphic `SourceType`/`SourceId` link the
/// email back to the document it was sent for (CustomerInvoice / Quotation / PurchaseOrder
/// / ProformaInvoice / etc.) — same pattern as <see cref="JournalEntry"/>.
/// </summary>
public class SentEmail : BaseTransactionalEntity
{
    public DateTimeOffset SentAt { get; set; }
    public string SentByUser { get; set; } = string.Empty;

    /// <summary>Polymorphic source ref — what document this email was about.</summary>
    public string? SourceType { get; set; }
    public long? SourceId { get; set; }
    public string? SourceCode { get; set; }

    /// <summary>Comma-separated recipient list (for audit display).</summary>
    public string ToAddresses { get; set; } = string.Empty;
    public string? CcAddresses { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;            // rendered HTML, snapshotted

    public SentEmailStatus Status { get; set; } = SentEmailStatus.Sent;
    public string? ErrorMessage { get; set; }                    // populated when Status = Failed
}

public enum SentEmailStatus
{
    Sent = 1,       // SMTP accepted the message
    Failed = 2      // SMTP threw / send command errored
}
