using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;

namespace StoryCoffee.Infrastructure.Documents;

public sealed class QuestPdfGenerator(IClock clock) : IPdfGenerator
{
    public byte[] Generate(PdfDocumentResult document)
    {
        QuestPDF.Settings.License = LicenseType.Community;

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
}
