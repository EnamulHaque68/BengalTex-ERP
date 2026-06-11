using BengalTex.ERP.Application.Reports.Dtos;

namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Renders Statement-of-Account PDFs (customer / AR and supplier / AP) from the
/// statement report DTOs. Implemented in Infrastructure via QuestPDF — both sides
/// share one layout, only labels and column semantics differ.
/// </summary>
public interface IStatementPdfRenderer
{
    byte[] RenderCustomerStatement(CustomerStatementReportDto report, string companyName, string? companyAddress);

    byte[] RenderSupplierStatement(SupplierStatementReportDto report, string companyName, string? companyAddress);
}
