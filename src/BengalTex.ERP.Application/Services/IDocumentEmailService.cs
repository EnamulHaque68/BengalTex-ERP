namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Renders an HTML email body for a document so the email-send pipeline can hand it to
/// <see cref="BengalTex.ERP.Application.Common.Interfaces.IEmailSender"/>. Supports the
/// document types listed in <see cref="SupportedSourceTypes"/>. Returns null when the
/// requested doc is not found or the type isn't supported.
/// </summary>
public interface IDocumentEmailService
{
    Task<DocumentEmailRenderResult?> RenderAsync(string sourceType, long sourceId, CancellationToken ct = default);

    /// <summary>Source-type strings the renderer recognises (also used to gate the UI button list).</summary>
    public static readonly IReadOnlyList<string> SupportedSourceTypes = new[]
    {
        "CustomerInvoice", "Quotation", "PurchaseOrder", "ProformaInvoice", "Receipt"
    };
}

public sealed record DocumentEmailRenderResult(
    string SourceCode,
    string DefaultSubject,
    string HtmlBody,
    string? DefaultToAddress);   // best-guess recipient (e.g. customer email / supplier email)
