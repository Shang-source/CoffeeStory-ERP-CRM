using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;

namespace StoryCoffee.Infrastructure.Documents;

public sealed class QuestPdfGenerator(IClock clock) : IPdfGenerator
{
    private const string TextColor = "#1E2A32";
    private const string MutedTextColor = "#4B5560";
    private const string HeaderBackground = "#EAF5F8";
    private const string TableHeaderBackground = "#E3F2FD";
    private const string NotesBackground = "#F7F3EE";

    private const string StoryCoffeeLogoSvg = """
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 120">
  <circle cx="60" cy="60" r="49" fill="none" stroke="#007A3D" stroke-width="7"/>
  <path d="M70 11C53 28 52 43 70 63C84 78 83 95 67 110C93 103 108 83 107 60C106 34 91 17 70 11Z" fill="#007A3D"/>
  <path d="M46 18C35 34 35 52 49 68C62 83 65 96 57 111C36 99 24 80 25 59C26 39 34 25 46 18Z" fill="#007A3D"/>
  <path d="M55 21C47 38 50 51 64 67C75 80 78 93 70 105" fill="none" stroke="#FFFFFF" stroke-width="6" stroke-linecap="round"/>
  <path d="M44 32C40 48 46 61 59 75C69 86 71 98 64 109" fill="none" stroke="#FFFFFF" stroke-width="5" stroke-linecap="round"/>
</svg>
""";

    public byte[] Generate(PdfDocumentResult document)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        if (document.Invoice is not null)
        {
            return GenerateInvoice(document);
        }

        if (document.Statement is not null)
        {
            return GenerateStatement(document);
        }

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(style => style.FontSize(10).FontFamily(Fonts.Arial));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("StoryCoffee").FontSize(20).Bold().FontColor(Colors.Brown.Darken2);
                        column.Item().Text(document.Title).FontSize(14).SemiBold();
                    });

                page.Content()
                    .PaddingTop(24)
                    .Column(column =>
                    {
                        column.Spacing(6);
                        foreach (var line in document.Lines)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                column.Item().PaddingVertical(4);
                                continue;
                            }

                            column.Item().Text(line);
                        }
                    });

                page.Footer()
                    .AlignRight()
                    .Text($"Generated {clock.UtcNow:yyyy-MM-dd HH:mm 'UTC'}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();
    }

    private byte[] GenerateInvoice(PdfDocumentResult result)
    {
        var invoice = result.Invoice!;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(9).FontFamily(Fonts.Arial).FontColor(TextColor));

                page.Header().Element(content => HeroHeader(content, invoice.Company.Name, $"Invoice {invoice.InvoiceNumber}", invoice.AmountDue, invoice.DueDate));
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(9);
                    column.Item().Element(content => BrandBlock(content, invoice.Company));
                    column.Item().Element(content => DocumentSummary(content, [
                        ("Invoice number", invoice.InvoiceNumber),
                        ("GST number", invoice.Company.GstNumber),
                        ("Invoice date", invoice.IssueDate.ToString("MMM d, yyyy")),
                        ("Payment due", invoice.DueDate.ToString("MMM d, yyyy")),
                        ("Bill to", invoice.CustomerName),
                        ("Billing address", invoice.BillingAddress)
                    ]));

                    column.Item().Text("Items").FontSize(13).Bold().FontColor(TextColor);
                    column.Item().Element(content => BrandedInvoiceItemsTable(content, invoice.Items));
                    column.Item().Element(content => InvoiceClosingRow(content, invoice));
                });
                page.Footer().Element(Footer);
            });
        }).GeneratePdf();
    }

    private byte[] GenerateStatement(PdfDocumentResult result)
    {
        var statement = result.Statement!;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(9).FontFamily(Fonts.Arial).FontColor(TextColor));

                page.Header().Element(content => HeroHeader(content, statement.Company.Name, $"Statement {statement.StatementNumber}", statement.TotalOutstanding, statement.StatementDate));
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(9);
                    column.Item().Element(content => BrandBlock(content, statement.Company));
                    column.Item().Element(content => DocumentSummary(content, [
                        ("Statement number", statement.StatementNumber),
                        ("GST number", statement.Company.GstNumber),
                        ("Statement date", statement.StatementDate.ToString("MMM d, yyyy")),
                        ("Period", $"{statement.PeriodStart:MMM d, yyyy} - {statement.PeriodEnd:MMM d, yyyy}"),
                        ("Bill to", statement.CustomerName),
                        ("Billing address", statement.BillingAddress)
                    ]));

                    column.Item().Text("Outstanding invoices").FontSize(13).Bold().FontColor(TextColor);
                    column.Item().Element(content => StatementItemsTable(content, statement.Invoices));
                    column.Item().Element(content => StatementClosingRow(content, statement));
                });
                page.Footer().Element(Footer);
            });
        }).GeneratePdf();
    }

    private static void HeroHeader(IContainer container, string companyName, string documentTitle, decimal amountDue, DateTimeOffset dueDate)
    {
        container.Background(HeaderBackground).PaddingVertical(10).Column(hero =>
        {
            hero.Spacing(1);
            hero.Item().AlignCenter().Text(companyName).FontSize(22).Bold();
            hero.Item().AlignCenter().Text(documentTitle).FontSize(12).SemiBold();
            hero.Item().AlignCenter().Text(Money(amountDue)).FontSize(24);
            hero.Item().AlignCenter().Text($"Due on {dueDate:MMM d, yyyy}").FontSize(10);
        });
    }

    private static void BrandBlock(IContainer container, CompanyDocumentProfile company)
    {
        container.AlignCenter().Column(brand =>
        {
            brand.Spacing(1);
            brand.Item().AlignCenter().Width(58).Svg(StoryCoffeeLogoSvg);
            brand.Item().AlignCenter().Text("STORY").FontSize(18).Bold().FontColor(TextColor);
            brand.Item().AlignCenter().Text("COFFEE").FontSize(10).FontColor(Colors.Grey.Darken1);
            brand.Item().PaddingTop(5).AlignCenter().Text(company.Name).FontSize(9).Bold();
            brand.Item().AlignCenter().Text(company.PostalAddressLine1).FontSize(8);
            brand.Item().AlignCenter().Text(company.PostalAddressLine2).FontSize(8);
            brand.Item().AlignCenter().Text(company.Country).FontSize(8);
            brand.Item().AlignCenter().Text(company.Website).FontSize(8);
        });
    }

    private static void DocumentSummary(IContainer container, IReadOnlyList<(string Label, string Value)> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(95);
                columns.RelativeColumn();
                columns.ConstantColumn(95);
                columns.RelativeColumn();
            });

            for (var index = 0; index < rows.Count; index += 2)
            {
                SummaryCell(table, rows[index].Label, true);
                SummaryCell(table, rows[index].Value, false);
                if (index + 1 < rows.Count)
                {
                    SummaryCell(table, rows[index + 1].Label, true);
                    SummaryCell(table, rows[index + 1].Value, false);
                }
                else
                {
                    table.Cell();
                    table.Cell();
                }
            }
        });
    }

    private static void SummaryCell(TableDescriptor table, string value, bool isLabel)
    {
        var cell = table.Cell().PaddingVertical(2);
        if (isLabel)
        {
            cell.Text($"{value}:").FontColor(Colors.Grey.Darken2);
        }
        else
        {
            cell.Text(value).SemiBold();
        }
    }

    private static void BrandedInvoiceItemsTable(IContainer container, IReadOnlyList<InvoicePdfItem> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(85);
                columns.ConstantColumn(90);
                columns.ConstantColumn(95);
            });
            TableHeader(table, "Items", "Quantity", "Price", "Amount");
            foreach (var item in items)
            {
                table.Cell().Element(TableCell).Column(column =>
                {
                    column.Item().Text(item.Description).SemiBold();
                    if (!string.IsNullOrWhiteSpace(item.Note))
                    {
                        column.Item().PaddingTop(2).Text(item.Note).FontColor(MutedTextColor);
                    }
                });
                table.Cell().Element(TableCell).AlignRight().Text(item.Quantity.ToString());
                table.Cell().Element(TableCell).AlignRight().Text(Money(item.UnitPrice));
                table.Cell().Element(TableCell).AlignRight().Text(Money(item.LineTotal));
            }
        });
    }

    private static void StatementItemsTable(IContainer container, IReadOnlyList<StatementInvoicePdfLine> invoices)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(80);
                columns.ConstantColumn(80);
                columns.ConstantColumn(90);
                columns.ConstantColumn(90);
            });
            TableHeader(table, "Invoice", "Issued", "Due", "Status", "Outstanding");
            foreach (var invoice in invoices)
            {
                table.Cell().Element(TableCell).Text(invoice.InvoiceNumber);
                table.Cell().Element(TableCell).Text(invoice.IssueDate.ToString("MMM d"));
                table.Cell().Element(TableCell).Text(invoice.DueDate.ToString("MMM d"));
                table.Cell().Element(TableCell).Text(invoice.Status.ToString());
                table.Cell().Element(TableCell).AlignRight().Text(Money(invoice.OutstandingAmount));
            }
        });
    }

    private static void TableHeader(TableDescriptor table, params string[] labels)
    {
        foreach (var label in labels)
        {
            table.Cell().Background(TableHeaderBackground).PaddingVertical(5).PaddingHorizontal(6).Text(label).Bold();
        }
    }

    private static IContainer TableCell(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(5);
    }

    private static void InvoiceClosingRow(IContainer container, InvoicePdfDocument invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(content => PaymentNotes(content, invoice.Company, invoice.InvoiceNumber));
            row.ConstantItem(16);
            row.ConstantItem(220).Element(content => InvoiceTotals(content, invoice));
        });
    }

    private static void StatementClosingRow(IContainer container, StatementPdfDocument statement)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(content => PaymentNotes(content, statement.Company, statement.StatementNumber));
            row.ConstantItem(16);
            row.ConstantItem(240).Column(column =>
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Row(total =>
                {
                    total.RelativeItem().Text("Total outstanding (NZD)").SemiBold();
                    total.ConstantItem(100).AlignRight().Text(Money(statement.TotalOutstanding)).Bold();
                });
            });
        });
    }

    private static void InvoiceTotals(IContainer container, InvoicePdfDocument invoice)
    {
        container.Column(column =>
        {
            TotalRow(column, "Subtotal", invoice.Subtotal);
            TotalRow(column, "GST (15%)", invoice.GstAmount);
            TotalRow(column, "Total (NZD)", invoice.TotalAmount, true);
            TotalRow(column, "Amount due", invoice.AmountDue, true);
        });
    }

    private static void TotalRow(ColumnDescriptor column, string label, decimal value, bool bold = false)
    {
        column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Row(row =>
        {
            row.RelativeItem().Text(label).SemiBold();
            var amount = row.ConstantItem(95).AlignRight().Text(Money(value));
            if (bold)
            {
                amount.Bold();
            }
        });
    }

    private static void PaymentNotes(IContainer container, CompanyDocumentProfile company, string reference)
    {
        container.Background(NotesBackground).Padding(9).Column(column =>
        {
            column.Spacing(2);
            column.Item().Text("Notes").FontSize(10).Bold();
            column.Item().Text($"Please use your company name or {reference} as the reference.");
            column.Item().PaddingTop(4).Text($"Bank: {company.BankName}");
            column.Item().Text($"Account number: {company.BankAccountNumber}");
        });
    }

    private void Footer(IContainer container)
    {
        container.AlignRight().Text($"Generated {clock.UtcNow:yyyy-MM-dd HH:mm 'UTC'}").FontSize(8).FontColor(Colors.Grey.Darken1);
    }

    private static string Money(decimal amount)
    {
        return $"${amount:F2}";
    }
}
