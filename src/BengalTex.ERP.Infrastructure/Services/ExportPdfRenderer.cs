using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// QuestPDF-backed renderer for export Commercial Invoice + Packing List PDFs.
/// Minimal, data-dense A4 layouts (not pixel-perfect mirrors of the HTML prints —
/// buyers + customs care about the data, not the styling).
/// QuestPDF.Settings.License is initialised once at app startup in Program.cs.
/// </summary>
public sealed class ExportPdfRenderer : IExportPdfRenderer
{
    public byte[] RenderCommercialInvoice(CustomerInvoiceDto inv, string companyName, string? companyAddress)
    {
        var doc = Document.Create(c =>
        {
            c.Page(p =>
            {
                Common(p);
                p.Header().Element(h => RenderHeader(h, "COMMERCIAL INVOICE", companyName, companyAddress));
                p.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Element(e => RenderMetaStrip(e, inv, includeShipmentDate: true));
                        col.Item().Element(e => RenderParties(e, inv));
                        col.Item().Element(e => RenderShippingStrip(e, inv));
                        col.Item().Element(e => RenderLines(e, inv, includeAmounts: true));
                        col.Item().Element(e => RenderTotals(e, inv));
                        if (inv.BeneficiaryBank is not null)
                            col.Item().Element(e => RenderBeneficiaryBank(e, inv));
                        col.Item().PaddingTop(6).Text(
                            "We declare that the information given above is true and correct, and that the goods described herein are of Bangladesh origin.")
                            .Italic().FontSize(8).FontColor(Colors.Grey.Darken1);
                        col.Item().Element(e => RenderSignatures(e, companyName, "Buyer Acknowledgement"));
                    });
                });
                p.Footer().Element(f => RenderFooter(f, inv));
            });
        });
        return doc.GeneratePdf();
    }

    public byte[] RenderPackingList(CustomerInvoiceDto inv, string companyName, string? companyAddress)
    {
        var doc = Document.Create(c =>
        {
            c.Page(p =>
            {
                Common(p);
                p.Header().Element(h => RenderHeader(h, "PACKING LIST", companyName, companyAddress));
                p.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Element(e => RenderMetaStrip(e, inv, includeShipmentDate: true));
                        col.Item().Element(e => RenderParties(e, inv));
                        col.Item().Element(e => RenderShippingStrip(e, inv));
                        if (!string.IsNullOrWhiteSpace(inv.ShippingMarks))
                            col.Item().Element(e => RenderShippingMarks(e, inv.ShippingMarks!));
                        col.Item().Element(e => RenderLines(e, inv, includeAmounts: false));
                        col.Item().Element(e => RenderPackingSummary(e, inv));
                        col.Item().Element(e => RenderSignatures(e, "Packed By for " + companyName, "Checked By for " + companyName));
                    });
                });
                p.Footer().Element(f => RenderFooter(f, inv));
            });
        });
        return doc.GeneratePdf();
    }

    // ──────────────────────────── Shared building blocks ─────────────────────────

    private static void Common(PageDescriptor p)
    {
        p.Size(PageSizes.A4);
        p.Margin(28);
        p.PageColor(Colors.White);
        p.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial").FontColor(Colors.Grey.Darken4));
    }

    private static void RenderHeader(IContainer container, string title, string companyName, string? companyAddress)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(companyName).Bold().FontSize(14).FontColor(Colors.Grey.Darken4);
                    if (!string.IsNullOrWhiteSpace(companyAddress))
                        c.Item().Text(companyAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(180).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(title).Bold().FontSize(16).FontColor(Colors.Grey.Darken4);
                    c.Item().AlignRight().Text("For export shipment").FontSize(7).FontColor(Colors.Grey.Darken1);
                });
            });
            col.Item().PaddingTop(4).LineHorizontal(1.2f).LineColor(Colors.Black);
        });
    }

    private static void RenderMetaStrip(IContainer container, CustomerInvoiceDto inv, bool includeShipmentDate)
    {
        container.Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
        {
            void Cell(string label, string value)
                => row.RelativeItem().Column(c =>
                {
                    c.Item().Text(label).FontSize(7).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().Text(value ?? "—").FontSize(9).Bold();
                });

            Cell("INVOICE #", inv.Code);
            Cell("INVOICE DATE", inv.InvoiceDate.ToString("yyyy-MM-dd"));
            if (includeShipmentDate)
                Cell("SHIPMENT DATE", inv.ShipmentDate?.ToString("yyyy-MM-dd") ?? "—");
            if (!string.IsNullOrWhiteSpace(inv.EpbFormNumber))
                Cell("FORM-EXP #", inv.EpbFormNumber!);
            if (!string.IsNullOrWhiteSpace(inv.LcNumber))
                Cell("LC #", inv.LcNumber!);
            Cell("SO REF", inv.SalesOrderCode);
        });
    }

    private static void RenderParties(IContainer container, CustomerInvoiceDto inv)
    {
        container.Row(row =>
        {
            row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(6).Column(c =>
            {
                c.Item().Text("EXPORTER").Bold().FontSize(7).FontColor(Colors.Grey.Darken1);
                c.Item().Text(inv.CustomerName).FontSize(10).Bold();
            });
            row.ConstantItem(8);
            row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(6).Column(c =>
            {
                c.Item().Text("CONSIGNEE / BUYER").Bold().FontSize(7).FontColor(Colors.Grey.Darken1);
                c.Item().Text(inv.CustomerName).FontSize(10).Bold();
                c.Item().Text($"Country: {inv.CountryOfDestination ?? "—"}").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void RenderShippingStrip(IContainer container, CustomerInvoiceDto inv)
    {
        var cells = new List<(string l, string v)>
        {
            ("Incoterm", inv.IncoTerm ?? "—"),
            ("Port of Loading", inv.PortOfLoading ?? "—"),
            ("Port of Discharge", inv.PortOfDischarge ?? "—"),
            ("Vessel / Flight", inv.VesselName ?? "—"),
            ("Currency", inv.CurrencyCode),
            ("Country of Origin", "Bangladesh"),
            ("Country of Destination", inv.CountryOfDestination ?? "—"),
        };
        if (!string.IsNullOrWhiteSpace(inv.ContainerNumber)) cells.Add(("Container #", inv.ContainerNumber!));
        if (!string.IsNullOrWhiteSpace(inv.SealNumber)) cells.Add(("Seal #", inv.SealNumber!));
        if (!string.IsNullOrWhiteSpace(inv.TruckNumber)) cells.Add(("Truck #", inv.TruckNumber!));

        container.Background(Colors.Indigo.Lighten5).Padding(6).Column(col =>
        {
            // 4 cells per row
            for (var i = 0; i < cells.Count; i += 4)
            {
                col.Item().Row(row =>
                {
                    for (var j = i; j < Math.Min(i + 4, cells.Count); j++)
                    {
                        var (l, v) = cells[j];
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(l).FontSize(7).Bold().FontColor(Colors.Indigo.Darken2);
                            c.Item().Text(v).FontSize(9).SemiBold();
                        });
                    }
                });
                if (i + 4 < cells.Count) col.Item().Height(4);
            }
        });
    }

    private static void RenderLines(IContainer container, CustomerInvoiceDto inv, bool includeAmounts)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.ConstantColumn(20);                     // #
                cd.RelativeColumn(3);                      // Description
                cd.RelativeColumn(1.2f);                   // HS Code
                cd.RelativeColumn(0.8f);                   // Qty
                cd.RelativeColumn(0.6f);                   // UoM
                if (includeAmounts)
                {
                    cd.RelativeColumn(1.2f);               // Unit Price
                    cd.RelativeColumn(1.4f);               // Amount
                }
                else
                {
                    cd.RelativeColumn(1f);                 // Cartons
                    cd.RelativeColumn(0.7f);               // Units/Ctn
                    cd.RelativeColumn(0.9f);               // Net Wt
                    cd.RelativeColumn(0.9f);               // Gross Wt
                }
            });

            t.Header(h =>
            {
                IContainer Th(string text) => h.Cell().Background(Colors.Grey.Darken4)
                    .Padding(4).DefaultTextStyle(s => s.FontColor(Colors.White).FontSize(8).Bold());
                Th("#").AlignRight().Text("#");
                Th("Item").Text("Description of Goods");
                Th("HS").Text("HS Code");
                Th("Qty").AlignRight().Text("Qty");
                Th("UoM").Text("UoM");
                if (includeAmounts)
                {
                    Th("Price").AlignRight().Text("Unit Price");
                    Th("Amt").AlignRight().Text("Amount");
                }
                else
                {
                    Th("Ctn").AlignRight().Text("Cartons");
                    Th("UPC").AlignRight().Text("Units/Ctn");
                    Th("Net").AlignRight().Text("Net Wt (kg)");
                    Th("Gross").AlignRight().Text("Gross Wt (kg)");
                }
            });

            var i = 1;
            foreach (var l in inv.Lines)
            {
                IContainer Td() => t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3);

                Td().AlignRight().Text(i.ToString());
                Td().Column(c =>
                {
                    c.Item().Text(l.ProductName).SemiBold();
                    c.Item().Text(l.ProductCode).FontSize(7).FontColor(Colors.Grey.Darken1);
                });
                Td().Text(l.HsCode ?? "—");
                Td().AlignRight().Text(l.Quantity.ToString("0.####"));
                Td().Text(l.UnitOfMeasureCode);
                if (includeAmounts)
                {
                    Td().AlignRight().Text(Money(l.UnitPrice, inv.CurrencyCode));
                    Td().AlignRight().Text(Money(l.LineTotal, inv.CurrencyCode));
                }
                else
                {
                    Td().AlignRight().Text(CartonRange(l));
                    Td().AlignRight().Text(l.UnitsPerCarton?.ToString() ?? "—");
                    Td().AlignRight().Text(l.NetWeightKgPerLine?.ToString("0.###") ?? "—");
                    Td().AlignRight().Text(l.GrossWeightKgPerLine?.ToString("0.###") ?? "—");
                }
                i++;
            }
        });
    }

    private static void RenderTotals(IContainer container, CustomerInvoiceDto inv)
    {
        container.AlignRight().Width(220).Background(Colors.Grey.Lighten4).Padding(6).Column(col =>
        {
            void Line(string l, string v, bool emph = false)
            {
                col.Item().Row(r =>
                {
                    var ll = r.RelativeItem().Text(l).FontSize(emph ? 10 : 9);
                    var vv = r.ConstantItem(110).AlignRight().Text(v).FontSize(emph ? 10 : 9);
                    if (emph) { ll.Bold(); vv.Bold(); }
                });
            }
            Line("Subtotal", Money(inv.SubtotalAmount, inv.CurrencyCode));
            if (inv.VatAmount > 0) Line("VAT", Money(inv.VatAmount, inv.CurrencyCode));
            col.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Black);
            Line($"TOTAL ({inv.CurrencyCode})", Money(inv.TotalAmount, inv.CurrencyCode), emph: true);
        });
    }

    private static void RenderBeneficiaryBank(IContainer container, CustomerInvoiceDto inv)
    {
        var b = inv.BeneficiaryBank!;
        container.Background(Colors.LightBlue.Lighten4).Padding(6).Column(col =>
        {
            col.Item().Text("BENEFICIARY BANK").FontSize(7).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().Row(r =>
            {
                r.RelativeItem().Text($"Beneficiary: {b.AccountName}").FontSize(9);
                r.RelativeItem().Text($"Bank: {b.BankName}").FontSize(9);
                r.RelativeItem().Text($"Currency: {b.Currency}").FontSize(9);
            });
            col.Item().Row(r =>
            {
                r.RelativeItem().Text($"Account #: {b.AccountNumber}").FontSize(9).FontFamily("Courier New");
                if (!string.IsNullOrWhiteSpace(b.SwiftCode))
                    r.RelativeItem().Text($"SWIFT: {b.SwiftCode}").FontSize(9).FontFamily("Courier New");
                if (!string.IsNullOrWhiteSpace(b.RoutingNumber))
                    r.RelativeItem().Text($"Routing #: {b.RoutingNumber}").FontSize(9).FontFamily("Courier New");
            });
        });
    }

    private static void RenderShippingMarks(IContainer container, string marks)
    {
        container.Background(Colors.Amber.Lighten5).Padding(6).Column(col =>
        {
            col.Item().Text("SHIPPING MARKS & NUMBERS").FontSize(7).Bold().FontColor(Colors.Amber.Darken3);
            col.Item().Text(marks).FontSize(9).FontFamily("Courier New").FontColor(Colors.Brown.Darken2);
        });
    }

    private static void RenderPackingSummary(IContainer container, CustomerInvoiceDto inv)
    {
        container.Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
        {
            void Cell(string label, string value)
                => row.RelativeItem().Column(c =>
                {
                    c.Item().Text(label).FontSize(7).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().Text(value).FontSize(11).Bold();
                });
            Cell("Total Packages", inv.TotalPackages?.ToString() ?? "—");
            Cell("Gross Weight (kg)", inv.GrossWeightKg?.ToString("0.###") ?? "—");
            Cell("Net Weight (kg)", inv.NetWeightKg?.ToString("0.###") ?? "—");
        });
    }

    private static void RenderSignatures(IContainer container, string left, string right)
    {
        container.PaddingTop(20).Row(row =>
        {
            void Sig(IContainer c, string label)
            {
                c.Column(col =>
                {
                    col.Item().PaddingTop(20).LineHorizontal(0.5f).LineColor(Colors.Black);
                    col.Item().AlignCenter().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            }
            Sig(row.RelativeItem(), left);
            row.ConstantItem(20);
            Sig(row.RelativeItem(), right);
        });
    }

    private static void RenderFooter(IContainer container, CustomerInvoiceDto inv)
    {
        container.PaddingTop(6).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2)
            .PaddingTop(4).AlignCenter()
            .Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Darken1));
                t.Span($"Computer-generated — reference {inv.Code} dated {inv.InvoiceDate:yyyy-MM-dd}");
            });
    }

    private static string CartonRange(CustomerInvoiceLineDto l)
    {
        if (l.CartonNumberFrom is null && l.CartonNumberTo is null) return "—";
        if (l.CartonNumberFrom is { } a && l.CartonNumberTo is { } b && a != b) return $"C{a}–C{b}";
        return $"C{l.CartonNumberFrom ?? l.CartonNumberTo}";
    }

    private static string Money(decimal amount, string currency)
        => $"{amount.ToString("N2")} {currency}";
}
