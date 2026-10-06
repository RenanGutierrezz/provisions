using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RWMS.Models.ViewModels.Delivery;

namespace RWMS.Services.Implementations;

public class DeliverySheetPdf : IDocument
{
    private readonly DeliveryDetailViewModel _delivery;

    public DeliverySheetPdf(DeliveryDetailViewModel delivery)
    {
        _delivery = delivery;
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
                col.Item().Text("RWMS — Delivery Sheet").Bold().FontSize(16);
                col.Item().PaddingTop(4).Text(
                    $"Hi {_delivery.DriverName}, today ({_delivery.Date:dddd, dd MMM yyyy}) you are delivering to {_delivery.Stops.Count} stop(s)."
                ).FontSize(10);
            });

            page.Content().PaddingTop(16).Column(col =>
            {
                if (_delivery.Stops.Count == 0)
                {
                    col.Item().Text("No orders assigned to this delivery.").FontColor(Colors.Grey.Medium);
                    return;
                }

                foreach (var (stop, index) in _delivery.Stops.Select((s, i) => (s, i)))
                {
                    col.Item().PaddingBottom(12).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(block =>
                    {
                        block.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Stop {index + 1} — {stop.CustomerName}").Bold().FontSize(12);
                            row.ConstantItem(80).AlignRight().Text($"Order #{stop.OrderId}").FontColor(Colors.Grey.Darken1);
                        });

                        if (!string.IsNullOrWhiteSpace(stop.CustomerAddress))
                            block.Item().PaddingTop(3).Text(stop.CustomerAddress).FontColor(Colors.Grey.Medium);

                        block.Item().PaddingTop(8).Text("Items to deliver:").Bold();

                        block.Item().PaddingTop(4).Column(items =>
                        {
                            foreach (var item in stop.ItemSummaries)
                            {
                                items.Item().Row(row =>
                                {
                                    row.ConstantItem(12).Text("•");
                                    row.RelativeItem().Text(item);
                                });
                            }
                        });

                    });
                }

                if (!string.IsNullOrWhiteSpace(_delivery.Notes))
                {
                    col.Item().PaddingTop(8).Border(1).BorderColor(Colors.Yellow.Medium).Background(Colors.Yellow.Lighten4).Padding(10).Column(note =>
                    {
                        note.Item().Text("Driver Notes").Bold();
                        note.Item().PaddingTop(4).Text(_delivery.Notes!);
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
