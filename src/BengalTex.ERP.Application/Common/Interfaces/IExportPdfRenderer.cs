using BengalTex.ERP.Application.CustomerInvoice.Dtos;

namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Renders the export-document PDFs (Commercial Invoice + Packing List) from a
/// <see cref="CustomerInvoiceDto"/>. Implemented in Infrastructure via QuestPDF.
/// </summary>
public interface IExportPdfRenderer
{
    byte[] RenderCommercialInvoice(CustomerInvoiceDto invoice, string companyName, string? companyAddress);

    byte[] RenderPackingList(CustomerInvoiceDto invoice, string companyName, string? companyAddress);
}
