using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RWMS.Models.ViewModels.Supply;

namespace RWMS.Services.Implementations;

public class SupplyOrderPdf : IDocument
{
    private readonly List<SupplyListViewModel> _items;
    private readonly DateTime _generatedAt;

    public SupplyOrderPdf(List<SupplyListViewModel> items, DateTime generatedAt)
    {
        _items = items;
        _generatedAt = generatedAt;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var groups = _items
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Category) ? "Other" : i.Category)
            .OrderBy(g => g.Key)
            .ToList();

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text("Shopping List").Bold().FontSize(18);
                        inner.Item().Text($"{_generatedAt:dddd, MMMM d, yyyy}").FontColor(Colors.Grey.Darken1).FontSize(10);
                    });
                    row.ConstantItem(160).AlignRight().Column(inner =>
                    {
                        inner.Item().Text($"{_items.Count} items to buy").FontSize(10).FontColor(Colors.Grey.Darken1);
                        inner.Item().Text($"Generated {_generatedAt:HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
                col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Colors.Grey.Darken2);
            });

            page.Content().PaddingTop(16).Column(content =>
            {
                foreach (var group in groups)
                {
                    var groupItems = group.ToList();

                    content.Item().PaddingTop(12).Column(section =>
                    {
                        section.Item().Row(row =>
                        {
                            row.RelativeItem()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(6)
                                .Text(group.Key.ToUpperInvariant())
                                .Bold()
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken2)
                                .LetterSpacing(0.08f);
                            row.ConstantItem(60)
                                .Background(Colors.Grey.Lighten3)
                                .AlignRight()
                                .Padding(6)
                                .Text($"{groupItems.Count} item{(groupItems.Count == 1 ? "" : "s")}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Medium);
                        });

                        section.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(22);
                                cols.RelativeColumn(4);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                IContainer HeaderCell(IContainer c) =>
                                    c.DefaultTextStyle(t => t.Bold().FontSize(9).FontColor(Colors.Grey.Darken2))
                                     .BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                     .PaddingVertical(5).PaddingHorizontal(4);

                                header.Cell().Element(HeaderCell).Text("");
                                header.Cell().Element(HeaderCell).Text("Item");
                                header.Cell().Element(HeaderCell).Text("Needed");
                                header.Cell().Element(HeaderCell).Text("Notes");
                            });

                            foreach (var (item, i) in groupItems.Select((x, i) => (x, i)))
                            {
                                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                                IContainer DataCell(IContainer c) =>
                                    c.Background(bg)
                                     .BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                     .PaddingVertical(7).PaddingHorizontal(4);

                                table.Cell().Element(DataCell).AlignCenter()
                                    .Text("□").FontSize(13).FontColor(Colors.Grey.Darken1);

                                table.Cell().Element(DataCell).Column(col =>
                                {
                                    col.Item().Text(item.Name).SemiBold().FontSize(10);
                                    col.Item().Text(item.Unit).FontSize(8).FontColor(Colors.Grey.Medium);
                                });

                                table.Cell().Element(DataCell)
                                    .Text($"{item.QuantityNeeded} {item.Unit}").SemiBold();

                                table.Cell().Element(DataCell)
                                    .Text(item.Notes ?? "").FontColor(Colors.Grey.Darken1).FontSize(9);
                            }
                        });
                    });
                }
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                t.Span(" of ").FontSize(9).FontColor(Colors.Grey.Medium);
                t.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
            });
        });
    }
}
