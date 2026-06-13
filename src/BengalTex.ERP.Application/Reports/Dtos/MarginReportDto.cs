namespace BengalTex.ERP.Application.Reports.Dtos;

public record MarginReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int? CustomerId,
    string? CustomerName,
    int ProductCount,
    decimal TotalRevenue,              // net of VAT (invoice line qty × unitPrice)
    decimal TotalCogs,                 // qty × current Product weighted-average cost
    decimal TotalMargin,               // TotalRevenue − TotalCogs
    decimal OverallMarginPercent,      // TotalMargin / TotalRevenue × 100 (0 if revenue 0)
    IReadOnlyList<MarginReportRowDto> Rows);

public record MarginReportRowDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasureCode,
    decimal QuantitySold,
    decimal Revenue,
    decimal Cogs,
    decimal UnitCost,                  // weighted-avg cost-at-sale across the window's lines
    decimal Margin,                    // Revenue − Cogs
    decimal MarginPercent);            // Margin / Revenue × 100 (0 if revenue 0)
