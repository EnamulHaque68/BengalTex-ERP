using System.Globalization;
using System.Text;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Renders an HTML email body for a CustomerInvoice / Quotation / PurchaseOrder /
/// ProformaInvoice / Receipt by loading the document with all its lines + party + currency
/// and emitting a self-contained HTML fragment that displays cleanly in any email client.
/// Inline-styled (no &lt;style&gt; tag) for max compatibility. Body includes a header strip,
/// a parties block, a lines table, a totals box, and a footer with company info.
/// </summary>
public class DocumentEmailService : IDocumentEmailService
{
    private readonly IRepository<CustomerInvoice, long> _ciRepo;
    private readonly IRepository<Quotation, long> _quoteRepo;
    private readonly IRepository<PurchaseOrder, long> _poRepo;
    private readonly IRepository<ProformaInvoice, long> _pfmRepo;
    private readonly IRepository<Receipt, long> _receiptRepo;
    private readonly IRepository<Company> _companyRepo;

    public DocumentEmailService(
        IRepository<CustomerInvoice, long> ciRepo,
        IRepository<Quotation, long> quoteRepo,
        IRepository<PurchaseOrder, long> poRepo,
        IRepository<ProformaInvoice, long> pfmRepo,
        IRepository<Receipt, long> receiptRepo,
        IRepository<Company> companyRepo)
    {
        _ciRepo = ciRepo; _quoteRepo = quoteRepo; _poRepo = poRepo;
        _pfmRepo = pfmRepo; _receiptRepo = receiptRepo; _companyRepo = companyRepo;
    }

    public async Task<DocumentEmailRenderResult?> RenderAsync(string sourceType, long sourceId, CancellationToken ct = default)
    {
        var company = await _companyRepo.Query().AsNoTracking().FirstOrDefaultAsync(ct);
        var companyName = company?.Name ?? "Bengal TEX";

        return sourceType switch
        {
            "CustomerInvoice"  => await RenderCustomerInvoiceAsync(sourceId, companyName, company, ct),
            "Quotation"        => await RenderQuotationAsync(sourceId, companyName, company, ct),
            "PurchaseOrder"    => await RenderPurchaseOrderAsync(sourceId, companyName, company, ct),
            "ProformaInvoice"  => await RenderProformaAsync(sourceId, companyName, company, ct),
            "Receipt"          => await RenderReceiptAsync(sourceId, companyName, company, ct),
            _ => null
        };
    }

    private async Task<DocumentEmailRenderResult?> RenderCustomerInvoiceAsync(long id, string companyName, Company? company, CancellationToken ct)
    {
        var ci = await _ciRepo.Query().AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Currency)
            .Include(x => x.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (ci is null) return null;

        var sb = OpenBody($"Invoice {ci.Code}", companyName);
        AppendMeta(sb,
            ("Invoice #", ci.Code),
            ("Invoice Date", ci.InvoiceDate.ToString("yyyy-MM-dd")),
            ("Due Date", ci.DueDate.ToString("yyyy-MM-dd")),
            ("Status", ci.Status.ToString()));
        AppendParty(sb, "Billed To", ci.Customer.Name,
            FormatAddress(ci.Customer.AddressLine1, ci.Customer.AddressLine2, ci.Customer.City, ci.Customer.District, ci.Customer.PostalCode));
        AppendProductLines(sb, ci.Currency.Code,
            ci.Lines.OrderBy(l => l.SortOrder).Select(l => (
                Item: $"{l.Product.Name} ({l.Product.Code})",
                Unit: l.Product.UnitOfMeasure?.Code ?? "",
                Qty: l.Quantity, Price: l.UnitPrice)).ToList());
        AppendTotals(sb, ci.Currency.Code,
            ("Subtotal", ci.SubtotalAmount),
            ("VAT", ci.VatAmount),
            ("Total", ci.TotalAmount),
            ("Amount Paid", ci.AmountPaid),
            ("Outstanding", ci.TotalAmount - ci.AmountPaid));
        if (!string.IsNullOrWhiteSpace(ci.Notes)) AppendNotes(sb, ci.Notes);
        CloseBody(sb, companyName, company);

        return new DocumentEmailRenderResult(
            ci.Code,
            $"Invoice {ci.Code} from {companyName}",
            sb.ToString(),
            ci.Customer.Email);
    }

    private async Task<DocumentEmailRenderResult?> RenderQuotationAsync(long id, string companyName, Company? company, CancellationToken ct)
    {
        var q = await _quoteRepo.Query().AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Currency)
            .Include(x => x.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return null;

        var sb = OpenBody($"Quotation {q.Code}", companyName);
        AppendMeta(sb,
            ("Quotation #", q.Code),
            ("Quotation Date", q.QuotationDate.ToString("yyyy-MM-dd")),
            ("Valid Until", q.ValidUntil?.ToString("yyyy-MM-dd") ?? "—"),
            ("Status", q.Status.ToString()));
        AppendParty(sb, "Prepared For", q.Customer.Name,
            FormatAddress(q.Customer.AddressLine1, q.Customer.AddressLine2, q.Customer.City, q.Customer.District, q.Customer.PostalCode));
        AppendProductLines(sb, q.Currency.Code,
            q.Lines.OrderBy(l => l.SortOrder).Select(l => (
                Item: $"{l.Product.Name} ({l.Product.Code})",
                Unit: l.Product.UnitOfMeasure?.Code ?? "",
                Qty: l.Quantity, Price: l.UnitPrice)).ToList());
        AppendTotals(sb, q.Currency.Code,
            ("Total", q.TotalAmount));
        if (!string.IsNullOrWhiteSpace(q.Notes)) AppendNotes(sb, q.Notes);
        CloseBody(sb, companyName, company);

        return new DocumentEmailRenderResult(
            q.Code,
            $"Quotation {q.Code} from {companyName}",
            sb.ToString(),
            q.Customer.Email);
    }

    private async Task<DocumentEmailRenderResult?> RenderPurchaseOrderAsync(long id, string companyName, Company? company, CancellationToken ct)
    {
        var po = await _poRepo.Query().AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Currency)
            .Include(x => x.DeliveryWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (po is null) return null;

        var sb = OpenBody($"Purchase Order {po.Code}", companyName);
        AppendMeta(sb,
            ("PO #", po.Code),
            ("Order Date", po.OrderDate.ToString("yyyy-MM-dd")),
            ("Expected Delivery", po.ExpectedDeliveryDate?.ToString("yyyy-MM-dd") ?? "—"),
            ("Deliver To", po.DeliveryWarehouse?.Name ?? "—"),
            ("Status", po.Status.ToString()));
        AppendParty(sb, "Supplier", po.Supplier.Name,
            FormatAddress(po.Supplier.AddressLine1, po.Supplier.AddressLine2, po.Supplier.City, po.Supplier.District, po.Supplier.PostalCode));
        var subtotal = po.Lines.Sum(l => l.Quantity * l.UnitPrice);
        AppendProductLines(sb, po.Currency.Code,
            po.Lines.OrderBy(l => l.SortOrder).Select(l => (
                Item: $"{l.RawMaterial.Name} ({l.RawMaterial.Code})",
                Unit: l.RawMaterial.UnitOfMeasure?.Code ?? "",
                Qty: l.Quantity, Price: l.UnitPrice)).ToList());
        AppendTotals(sb, po.Currency.Code, ("Total", subtotal));
        if (!string.IsNullOrWhiteSpace(po.Notes)) AppendNotes(sb, po.Notes);
        CloseBody(sb, companyName, company);

        return new DocumentEmailRenderResult(
            po.Code,
            $"Purchase Order {po.Code} from {companyName}",
            sb.ToString(),
            po.Supplier.Email);
    }

    private async Task<DocumentEmailRenderResult?> RenderProformaAsync(long id, string companyName, Company? company, CancellationToken ct)
    {
        var pfm = await _pfmRepo.Query().AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Currency)
            .Include(x => x.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (pfm is null) return null;

        var sb = OpenBody($"Proforma Invoice {pfm.Code}", companyName);
        AppendMeta(sb,
            ("Proforma #", pfm.Code),
            ("Issue Date", pfm.IssueDate.ToString("yyyy-MM-dd")),
            ("Valid Until", pfm.ValidUntil.ToString("yyyy-MM-dd")),
            ("Status", pfm.Status.ToString()));
        AppendParty(sb, "Billed To", pfm.Customer.Name,
            FormatAddress(pfm.Customer.AddressLine1, pfm.Customer.AddressLine2, pfm.Customer.City, pfm.Customer.District, pfm.Customer.PostalCode));
        AppendProductLines(sb, pfm.Currency.Code,
            pfm.Lines.OrderBy(l => l.SortOrder).Select(l => (
                Item: $"{l.Product.Name} ({l.Product.Code})",
                Unit: l.Product.UnitOfMeasure?.Code ?? "",
                Qty: l.Quantity, Price: l.UnitPrice)).ToList());
        AppendTotals(sb, pfm.Currency.Code,
            ("Subtotal", pfm.SubtotalAmount),
            ("VAT", pfm.VatAmount),
            ("Total", pfm.TotalAmount));
        AppendNotes(sb, "<em>This is a Proforma Invoice and is non-binding. Final invoice will be issued on delivery.</em>");
        if (!string.IsNullOrWhiteSpace(pfm.Notes)) AppendNotes(sb, pfm.Notes);
        CloseBody(sb, companyName, company);

        return new DocumentEmailRenderResult(
            pfm.Code,
            $"Proforma Invoice {pfm.Code} from {companyName}",
            sb.ToString(),
            pfm.Customer.Email);
    }

    private async Task<DocumentEmailRenderResult?> RenderReceiptAsync(long id, string companyName, Company? company, CancellationToken ct)
    {
        var r = await _receiptRepo.Query().AsNoTracking()
            .Include(x => x.CustomerInvoice).ThenInclude(ci => ci.Customer)
            .Include(x => x.CustomerInvoice).ThenInclude(ci => ci.Currency)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return null;

        var sb = OpenBody($"Receipt {r.Code}", companyName);
        AppendMeta(sb,
            ("Receipt #", r.Code),
            ("Date", r.ReceiptDate.ToString("yyyy-MM-dd")),
            ("Against Invoice", r.CustomerInvoice.Code),
            ("Payment Method", r.PaymentMethod.ToString()),
            ("Reference", r.ReferenceNumber ?? "—"));
        AppendParty(sb, "Received From", r.CustomerInvoice.Customer.Name,
            FormatAddress(r.CustomerInvoice.Customer.AddressLine1, r.CustomerInvoice.Customer.AddressLine2, r.CustomerInvoice.Customer.City, r.CustomerInvoice.Customer.District, r.CustomerInvoice.Customer.PostalCode));
        AppendTotals(sb, r.CustomerInvoice.Currency.Code,
            ("Amount Received", r.Amount));
        if (!string.IsNullOrWhiteSpace(r.Notes)) AppendNotes(sb, r.Notes);
        CloseBody(sb, companyName, company);

        return new DocumentEmailRenderResult(
            r.Code,
            $"Receipt {r.Code} from {companyName}",
            sb.ToString(),
            r.CustomerInvoice.Customer.Email);
    }

    // ─── HTML helpers ──────────────────────────────────────────────────────

    private static StringBuilder OpenBody(string docTitle, string companyName)
    {
        var sb = new StringBuilder(8 * 1024);
        sb.Append("<div style=\"font-family:Arial,Helvetica,sans-serif;color:#1f2937;max-width:680px;margin:0 auto;\">");
        sb.Append("<div style=\"background:#1e3a8a;color:#fff;padding:18px 22px;border-radius:8px 8px 0 0;\">");
        sb.Append($"<div style=\"font-size:18px;font-weight:700;\">{System.Net.WebUtility.HtmlEncode(companyName)}</div>");
        sb.Append($"<div style=\"font-size:14px;opacity:0.85;margin-top:4px;\">{System.Net.WebUtility.HtmlEncode(docTitle)}</div>");
        sb.Append("</div>");
        sb.Append("<div style=\"background:#fff;padding:22px;border:1px solid #e2e8f0;border-top:none;\">");
        return sb;
    }

    private static void AppendMeta(StringBuilder sb, params (string Label, string Value)[] pairs)
    {
        sb.Append("<table style=\"width:100%;border-collapse:collapse;margin-bottom:14px;font-size:13px;\"><tr>");
        foreach (var (label, value) in pairs)
        {
            sb.Append("<td style=\"vertical-align:top;padding:6px 12px 6px 0;\">");
            sb.Append($"<div style=\"color:#94a3b8;font-size:11px;text-transform:uppercase;letter-spacing:0.03em;\">{System.Net.WebUtility.HtmlEncode(label)}</div>");
            sb.Append($"<div style=\"color:#0f172a;font-weight:600;margin-top:2px;\">{System.Net.WebUtility.HtmlEncode(value)}</div>");
            sb.Append("</td>");
        }
        sb.Append("</tr></table>");
    }

    private static void AppendParty(StringBuilder sb, string label, string name, string? address)
    {
        sb.Append("<div style=\"padding:10px 14px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:6px;margin-bottom:14px;font-size:13px;\">");
        sb.Append($"<div style=\"color:#94a3b8;font-size:11px;text-transform:uppercase;\">{System.Net.WebUtility.HtmlEncode(label)}</div>");
        sb.Append($"<div style=\"color:#0f172a;font-weight:600;margin-top:2px;\">{System.Net.WebUtility.HtmlEncode(name)}</div>");
        if (!string.IsNullOrWhiteSpace(address))
            sb.Append($"<div style=\"color:#64748b;margin-top:2px;\">{System.Net.WebUtility.HtmlEncode(address)}</div>");
        sb.Append("</div>");
    }

    private static void AppendProductLines(StringBuilder sb, string currencyCode, IReadOnlyList<(string Item, string Unit, decimal Qty, decimal Price)> lines)
    {
        if (lines.Count == 0) return;
        sb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:13px;margin-bottom:14px;\">");
        sb.Append("<thead><tr style=\"background:#f1f5f9;color:#475569;text-align:left;\">");
        sb.Append("<th style=\"padding:8px 10px;\">Item</th>");
        sb.Append("<th style=\"padding:8px 10px;\">Unit</th>");
        sb.Append("<th style=\"padding:8px 10px;text-align:right;\">Qty</th>");
        sb.Append("<th style=\"padding:8px 10px;text-align:right;\">Unit Price</th>");
        sb.Append("<th style=\"padding:8px 10px;text-align:right;\">Total</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var (item, unit, qty, price) in lines)
        {
            sb.Append("<tr style=\"border-bottom:1px solid #f1f5f9;\">");
            sb.Append($"<td style=\"padding:8px 10px;\">{System.Net.WebUtility.HtmlEncode(item)}</td>");
            sb.Append($"<td style=\"padding:8px 10px;color:#64748b;\">{System.Net.WebUtility.HtmlEncode(unit)}</td>");
            sb.Append($"<td style=\"padding:8px 10px;text-align:right;font-variant-numeric:tabular-nums;\">{Fmt(qty)}</td>");
            sb.Append($"<td style=\"padding:8px 10px;text-align:right;font-variant-numeric:tabular-nums;\">{Fmt(price)}</td>");
            sb.Append($"<td style=\"padding:8px 10px;text-align:right;font-weight:600;font-variant-numeric:tabular-nums;\">{currencyCode} {Fmt(qty * price)}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
    }

    private static void AppendTotals(StringBuilder sb, string currencyCode, params (string Label, decimal Amount)[] rows)
    {
        sb.Append("<table style=\"width:340px;margin-left:auto;border-collapse:collapse;font-size:13px;margin-bottom:14px;\">");
        for (int i = 0; i < rows.Length; i++)
        {
            var (label, amount) = rows[i];
            var isLast = i == rows.Length - 1;
            var style = isLast
                ? "padding:10px 12px;border-top:2px solid #1e3a8a;font-weight:700;font-size:14px;color:#1e3a8a;"
                : "padding:6px 12px;color:#475569;";
            sb.Append("<tr>");
            sb.Append($"<td style=\"{style}\">{System.Net.WebUtility.HtmlEncode(label)}</td>");
            sb.Append($"<td style=\"{style}text-align:right;font-variant-numeric:tabular-nums;\">{currencyCode} {Fmt(amount)}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</table>");
    }

    private static void AppendNotes(StringBuilder sb, string notesHtmlOrText)
    {
        sb.Append("<div style=\"padding:12px 14px;background:#fef3c7;border-left:3px solid #f59e0b;border-radius:4px;font-size:13px;color:#78350f;margin-bottom:14px;line-height:1.5;\">");
        sb.Append(notesHtmlOrText);
        sb.Append("</div>");
    }

    private static void CloseBody(StringBuilder sb, string companyName, Company? company)
    {
        sb.Append("</div>");   // inner padding div
        sb.Append("<div style=\"padding:12px 22px;background:#f8fafc;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 8px 8px;color:#64748b;font-size:12px;text-align:center;\">");
        sb.Append($"<div style=\"font-weight:600;color:#475569;\">{System.Net.WebUtility.HtmlEncode(companyName)}</div>");
        if (company is not null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(company.AddressLine1)) parts.Add(company.AddressLine1);
            if (!string.IsNullOrWhiteSpace(company.City)) parts.Add(company.City);
            if (!string.IsNullOrWhiteSpace(company.Phone)) parts.Add("Tel: " + company.Phone);
            if (!string.IsNullOrWhiteSpace(company.Email)) parts.Add(company.Email);
            if (parts.Count > 0)
                sb.Append($"<div style=\"margin-top:3px;\">{System.Net.WebUtility.HtmlEncode(string.Join(" · ", parts))}</div>");
        }
        sb.Append("</div>");
        sb.Append("</div>");   // outer wrapper
    }

    private static string Fmt(decimal v) => v.ToString("N2", CultureInfo.InvariantCulture);

    private static string FormatAddress(string? line1, string? line2, string? city, string? district, string? postal)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(line1)) parts.Add(line1);
        if (!string.IsNullOrWhiteSpace(line2)) parts.Add(line2);
        var cityBits = new List<string>();
        if (!string.IsNullOrWhiteSpace(city)) cityBits.Add(city);
        if (!string.IsNullOrWhiteSpace(district)) cityBits.Add(district);
        if (!string.IsNullOrWhiteSpace(postal)) cityBits.Add(postal);
        if (cityBits.Count > 0) parts.Add(string.Join(" ", cityBits));
        return string.Join(", ", parts);
    }
}
