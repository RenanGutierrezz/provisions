using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RWMS.Models.ViewModels.Order;

namespace RWMS.Services.Implementations;

public class OrdersDayPdf : IDocument
{
    private readonly List<OrderDetailViewModel> _orders;
    private readonly DateTime _date;

    public OrdersDayPdf(List<OrderDetailViewModel> orders, DateTime date)
    {
        _orders = orders;
        _date = date;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Text($"RWMS — Orders for {_date:dddd, dd MMM yyyy}").Bold().FontSize(16);
                col.Item().Text($"{_orders.Count} order(s)").FontColor(Colors.Grey.Medium).FontSize(9);
            });

            page.Content().PaddingTop(16).Column(col =>
            {
                if (_orders.Count == 0)
                {
                    col.Item().Text("No orders for this date.").FontColor(Colors.Grey.Medium);
                    return;
                }

                foreach (var order in _orders)
                {
                    col.Item().PaddingBottom(12).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(block =>
                    {
                        block.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Order #{order.Id} — {order.CustomerName}").Bold().FontSize(11);
                            row.ConstantItem(130).AlignRight().Text(order.Status.ToString()).FontColor(Colors.Grey.Darken1);
                        });

                        if (order.RequestedDeliveryDate.HasValue)
                            block.Item().PaddingTop(3).Text($"Delivery: {order.RequestedDeliveryDate.Value:dd MMM yyyy}").FontColor(Colors.Grey.Medium);

                        if (!string.IsNullOrWhiteSpace(order.Notes))
                            block.Item().PaddingTop(3).Text($"Notes: {order.Notes}").FontColor(Colors.Grey.Medium);

                        block.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(4);
                                cols.RelativeColumn();
                            });

                            table.Header(h =>
                            {
                                static IContainer HeaderCell(IContainer c) =>
                                    c.DefaultTextStyle(t => t.Bold()).BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingVertical(3);

                                h.Cell().Element(HeaderCell).Text("Product");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                            });

                            foreach (var item in order.Items)
                            {
                                table.Cell().PaddingVertical(3).Text(item.ProductName);
                                table.Cell().PaddingVertical(3).AlignRight().Text(item.Quantity.ToString());
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
