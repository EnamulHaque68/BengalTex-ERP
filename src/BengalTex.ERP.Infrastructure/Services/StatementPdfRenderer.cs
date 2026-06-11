using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Reports.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// QuestPDF renderer for customer (AR) and supplier (AP) statements of account.
/// Both sides map to one internal view model and share a single layout; only the
/// title, balance label and per-line semantics ("owes us" vs "we owe") differ.
/// QuestPDF.Settings.License is initialised once at startup in Program.cs.
/// </summary>
public sealed class StatementPdfRenderer : IStatementPdfRenderer
{
    private sealed record Line(DateOnly Date, string Type, string Reference, string? DocRef, decimal Debit, decimal Credit, decimal RunningBalance);

    private sealed record View(
        string Title,
        string PartyLabel,
        string PartyName,
        string PartyCode,
        string? PartyEmail,
        DateOnly FromDate,
        DateOnly ToDate,
        string BalanceLabel,          // "Balance" (AR) | "Payable" (AP)
        decimal Opening,
        decimal TotalDebits,
        decimal TotalCredits,
        decimal Closing,
        string ClosingNote,
        IReadOnlyList<Line> Lines);

    public byte[] RenderCustomerStatement(CustomerStatementReportDto r, string companyName, string? companyAddress)
        => Render(new View(
            "STATEMENT OF ACCOUNT",
            "STATEMENT FOR",
            r.CustomerName, r.CustomerCode, r.CustomerEmail,
            r.FromDate, r.ToDate,
            "Balance",
            r.OpeningBalance, r.TotalDebits, r.TotalCredits, r.ClosingBalance,
            r.ClosingBalance > 0
                ? $"Balance due to {companyName}: {Money(r.ClosingBalance)}"
                : "No balance due — credit on account.",
            r.Lines.Select(l => new Line(l.Date, l.Type, l.Reference, l.DocumentRef, l.Debit, l.Credit, l.RunningBalance)).ToList()),
            companyName, companyAddress);

    public byte[] RenderSupplierStatement(SupplierStatementReportDto r, string companyName, string? companyAddress)
        => Render(new View(
            "SUPPLIER STATEMENT OF ACCOUNT",
            "STATEMENT FOR",
            r.SupplierName, r.SupplierCode, r.SupplierEmail,
            r.FromDate, r.ToDate,
            "Payable",
            r.OpeningBalance, r.TotalDebits, r.TotalCredits, r.ClosingBalance,
            r.ClosingBalance > 0
                ? $"Payable to {r.SupplierName}: {Money(r.ClosingBalance)}"
                : "No payable outstanding — credit held with supplier.",
            r.Lines.Select(l => new Line(l.Date, l.Type, l.Reference, l.DocumentRef, l.Debit, l.Credit, l.RunningBalance)).ToList()),
            companyName, companyAddress);

    private static byte[] Render(View v, string companyName, string? companyAddress)
    {
        var doc = Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(28);
                p.PageColor(Colors.White);
                p.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial").FontColor(Colors.Grey.Darken4));

                p.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(cc =>
                        {
                            cc.Item().Text(companyName).Bold().FontSize(14);
                            if (!string.IsNullOrWhiteSpace(companyAddress))
                                cc.Item().Text(companyAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(230).AlignRight().Column(cc =>
                        {
                            cc.Item().AlignRight().Text(v.Title).Bold().FontSize(14);
                            cc.Item().AlignRight().Text($"{v.FromDate:yyyy-MM-dd} → {v.ToDate:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1.2f).LineColor(Colors.Black);
                });

                p.Content().Column(col =>
                {
                    col.Spacing(8);

                    // Party block
                    col.Item().Background(Colors.Grey.Lighten4).Padding(6).Column(cc =>
                    {
                        cc.Item().Text(v.PartyLabel).FontSize(7).Bold().FontColor(Colors.Grey.Darken1);
                        cc.Item().Text(v.PartyName).FontSize(11).Bold();
                        cc.Item().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                            t.Span($"Code: {v.PartyCode}");
                            if (!string.IsNullOrWhiteSpace(v.PartyEmail)) t.Span($"   ·   {v.PartyEmail}");
                        });
                    });

                    // Summary strip
                    col.Item().Background(Colors.Indigo.Lighten5).Padding(6).Row(row =>
                    {
                        void Cell(string label, decimal value, bool emph = false)
                            => row.RelativeItem().Column(cc =>
                            {
                                cc.Item().Text(label).FontSize(7).Bold().FontColor(Colors.Indigo.Darken2);
                                var t = cc.Item().Text(Money(value)).FontSize(emph ? 11 : 10);
                                if (emph) t.Bold();
                            });
                        Cell($"Opening {v.BalanceLabel}", v.Opening);
                        Cell("Debits", v.TotalDebits);
                        Cell("Credits", v.TotalCredits);
                        Cell($"Closing {v.BalanceLabel}", v.Closing, emph: true);
                    });

                    // Lines table
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(1.1f);   // Date
                            cd.RelativeColumn(0.9f);   // Type
                            cd.RelativeColumn(1.3f);   // Reference
                            cd.RelativeColumn(1.6f);   // Document
                            cd.RelativeColumn(1.2f);   // Debit
                            cd.RelativeColumn(1.2f);   // Credit
                            cd.RelativeColumn(1.4f);   // Balance
                        });

                        t.Header(h =>
                        {
                            IContainer Th() => h.Cell().Background(Colors.Grey.Darken4)
                                .Padding(4).DefaultTextStyle(s => s.FontColor(Colors.White).FontSize(8).Bold());
                            Th().Text("Date");
                            Th().Text("Type");
                            Th().Text("Reference");
                            Th().Text("Document");
                            Th().AlignRight().Text("Debit (BDT)");
                            Th().AlignRight().Text("Credit (BDT)");
                            Th().AlignRight().Text($"Running {v.BalanceLabel}");
                        });

                        IContainer Td(bool shaded = false)
                        {
                            var cell = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3);
                            return shaded ? cell.Background(Colors.Indigo.Lighten5) : cell;
                        }

                        // Opening row
                        Td(true).Text("");
                        Td(true).Text("Opening").Bold();
                        Td(true).Text("Brought forward").FontColor(Colors.Grey.Darken1);
                        Td(true).Text("");
                        Td(true).Text("");
                        Td(true).Text("");
                        Td(true).AlignRight().Text(Money(v.Opening)).Bold();

                        foreach (var l in v.Lines)
                        {
                            Td().Text(l.Date.ToString("yyyy-MM-dd"));
                            Td().Text(l.Type);
                            Td().Text(l.Reference);
                            Td().Text(l.DocRef ?? "—").FontColor(Colors.Grey.Darken1);
                            Td().AlignRight().Text(l.Debit > 0 ? Money(l.Debit) : "—");
                            Td().AlignRight().Text(l.Credit > 0 ? Money(l.Credit) : "—");
                            Td().AlignRight().Text(Money(l.RunningBalance));
                        }

                        // Closing row
                        Td(true).Text("");
                        Td(true).Text("Closing").Bold();
                        Td(true).Text("Carried forward").FontColor(Colors.Grey.Darken1);
                        Td(true).Text("");
                        Td(true).AlignRight().Text(Money(v.TotalDebits)).Bold();
                        Td(true).AlignRight().Text(Money(v.TotalCredits)).Bold();
                        Td(true).AlignRight().Text(Money(v.Closing)).Bold();
                    });

                    col.Item().PaddingTop(4).Text(v.ClosingNote).Italic().FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                p.Footer().PaddingTop(6).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .PaddingTop(4).AlignCenter()
                    .Text($"Computer-generated statement — {v.PartyCode} · period {v.FromDate:yyyy-MM-dd} to {v.ToDate:yyyy-MM-dd}")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });
        return doc.GeneratePdf();
    }

    private static string Money(decimal amount) => $"{amount:N2} BDT";
}
