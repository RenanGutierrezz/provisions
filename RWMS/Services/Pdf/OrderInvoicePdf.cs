using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RWMS.Models.Enums;
using RWMS.Models.ViewModels.Order;

namespace RWMS.Services.Implementations;

public class OrderInvoicePdf : IDocument
{
    private static readonly string Brand = "#1A4645";
    private static readonly string BrandLight = "#E8F0F0";
    private static readonly string TextDark = "#1E1E1E";
    private static readonly string TextMuted = "#6B7280";
    private static readonly string BorderLight = "#E5E7EB";

    private readonly OrderDetailViewModel _order;

    public OrderInvoicePdf(OrderDetailViewModel order)
    {
        _order = order;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginVertical(1.4f, Unit.Centimetre);
            page.MarginHorizontal(1.6f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(TextDark));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            // Top bar — brand accent
            col.Item().Height(4).Background(Brand);

            col.Item().PaddingTop(16).PaddingBottom(12).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("RWMS").Bold().FontSize(26).FontColor(Brand);
                    left.Item().PaddingTop(2).Text("Restaurant Wholesale Management").FontSize(8.5f).FontColor(TextMuted);
                });

                row.ConstantItem(170).AlignRight().Column(right =>
                {
                    right.Item().AlignRight().Text("INVOICE").Bold().FontSize(20).FontColor(Brand).LetterSpacing(0.05f);
                    right.Item().AlignRight().PaddingTop(4).Text($"# {_order.Id:D5}").FontSize(11).FontColor(TextMuted);
                });
            });

            col.Item().LineHorizontal(1.5f).LineColor(Brand);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(18).Column(col =>
        {
            // Info cards row
            col.Item().Row(row =>
            {
                // Bill To
                row.RelativeItem().Background(BrandLight).Padding(14).Column(left =>
                {
                    left.Item().Text("BILL TO").Bold().FontSize(8).FontColor(Brand).LetterSpacing(0.1f);
                    left.Item().PaddingTop(6).Text(_order.CustomerName).Bold().FontSize(11).FontColor(TextDark);

                    if (!string.IsNullOrWhiteSpace(_order.CustomerEmail))
                        left.Item().PaddingTop(3).Text(_order.CustomerEmail).FontSize(9).FontColor(TextMuted);

                    if (!string.IsNullOrWhiteSpace(_order.CustomerAddress))
                        left.Item().PaddingTop(3).Text(_order.CustomerAddress).FontSize(9).FontColor(TextMuted);
                });

                row.ConstantItem(16);

                // Order details
                row.ConstantItem(190).Background(BrandLight).Padding(14).Column(right =>
                {
                    right.Item().Text("ORDER DETAILS").Bold().FontSize(8).FontColor(Brand).LetterSpacing(0.1f);

                    right.Item().PaddingTop(8).Row(r =>
                    {
                        r.RelativeItem().Text("Date:").FontSize(9).FontColor(TextMuted);
                        r.ConstantItem(100).AlignRight().Text(_order.CreatedAt.ToString("MMM d, yyyy")).FontSize(9).Bold();
                    });

                    if (_order.RequestedDeliveryDate.HasValue)
                    {
                        right.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Delivery:").FontSize(9).FontColor(TextMuted);
                            r.ConstantItem(100).AlignRight().Text(_order.RequestedDeliveryDate.Value.ToString("MMM d, yyyy")).FontSize(9).Bold();
                        });
                    }

                    right.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text("Status:").FontSize(9).FontColor(TextMuted);
                        r.ConstantItem(100).AlignRight().Text(StatusLabel(_order.Status)).FontSize(9).Bold().FontColor(StatusColor(_order.Status));
                    });
                });
            });

            // Items table
            col.Item().PaddingTop(22).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(32);
                    cols.RelativeColumn(4);
                    cols.RelativeColumn(0.8f);
                    cols.RelativeColumn(1.2f);
                    cols.RelativeColumn(1.2f);
                });

                table.Header(h =>
                {
                    static IContainer HeaderCell(IContainer c) =>
                        c.DefaultTextStyle(t => t.Bold().FontColor(Colors.White).FontSize(8.5f))
                         .Background(Brand)
                         .PaddingVertical(8)
                         .PaddingHorizontal(10);

                    h.Cell().Element(HeaderCell).AlignCenter().Text("#");
                    h.Cell().Element(HeaderCell).Text("Product");
                    h.Cell().Element(HeaderCell).AlignCenter().Text("Qty");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Unit Price");
                    h.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                });

                var index = 0;
                foreach (var item in _order.Items)
                {
                    index++;
                    var stripe = index % 2 == 0;
                    var bg = stripe ? BrandLight : "#FFFFFF";
                    var isRejected = item.Status == OrderItemStatus.Rejected;
                    var textColor = isRejected ? TextMuted : TextDark;

                    IContainer Cell(IContainer c) =>
                        c.Background(bg)
                         .PaddingVertical(7)
                         .PaddingHorizontal(10)
                         .DefaultTextStyle(t => t.FontColor(textColor));

                    table.Cell().Element(Cell).AlignCenter().Text(index.ToString()).FontSize(9);

                    // Product name + status tag for non-accepted items
                    table.Cell().Element(Cell).Row(r =>
                    {
                        r.RelativeItem().Text(text =>
                        {
                            text.Span(item.ProductName);
                            if (isRejected)
                                text.Span("  REJECTED").FontSize(7).Bold().FontColor(Colors.Red.Darken2);
                        });
                    });

                    table.Cell().Element(Cell).AlignCenter().Text(item.Quantity.ToString());
                    table.Cell().Element(Cell).AlignRight().Text(item.UnitPrice.ToString("C"));
                    table.Cell().Element(Cell).AlignRight().Text(isRejected ? "—" : item.Subtotal.ToString("C"));
                }
            });

            // Summary box — right aligned
            col.Item().PaddingTop(4).AlignRight().Width(230).Column(summary =>
            {
                var accepted = _order.Items.Where(i => i.Status != OrderItemStatus.Rejected).ToList();
                var rejected = _order.Items.Where(i => i.Status == OrderItemStatus.Rejected).ToList();
                var subtotal = _order.Items.Sum(i => i.Subtotal);

                // Subtotal
                summary.Item().PaddingVertical(6).PaddingHorizontal(10).Row(row =>
                {
                    row.RelativeItem().Text("Subtotal").FontColor(TextMuted);
                    row.ConstantItem(90).AlignRight().Text(subtotal.ToString("C"));
                });

                if (rejected.Count > 0)
                {
                    var rejectedAmount = rejected.Sum(i => i.Subtotal);
                    summary.Item().PaddingVertical(4).PaddingHorizontal(10).Row(row =>
                    {
                        row.RelativeItem().Text("Rejected Items").FontColor(Colors.Red.Darken2).FontSize(9);
                        row.ConstantItem(90).AlignRight().Text($"- {rejectedAmount:C}").FontColor(Colors.Red.Darken2).FontSize(9);
                    });
                }

                // Total
                summary.Item().Background(Brand).PaddingVertical(10).PaddingHorizontal(10).Row(row =>
                {
                    row.RelativeItem().Text("TOTAL").Bold().FontSize(12).FontColor(Colors.White);
                    row.ConstantItem(90).AlignRight().Text(_order.TotalAmount.ToString("C")).Bold().FontSize(12).FontColor(Colors.White);
                });
            });

            // Notes
            if (!string.IsNullOrWhiteSpace(_order.Notes))
            {
                col.Item().PaddingTop(22).Column(notes =>
                {
                    notes.Item().Text("NOTES").Bold().FontSize(8).FontColor(Brand).LetterSpacing(0.1f);
                    notes.Item().PaddingTop(4).Border(1).BorderColor(BorderLight).Padding(10)
                        .Text(_order.Notes).FontSize(9).FontColor(TextMuted).LineHeight(1.5f);
                });
            }

            // Signature section
            col.Item().PaddingTop(40).Row(row =>
            {
                // Delivered by
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("DELIVERED BY").Bold().FontSize(8).FontColor(Brand).LetterSpacing(0.1f);
                    left.Item().PaddingTop(40).LineHorizontal(1).LineColor(TextDark);
                    left.Item().PaddingTop(4).Text("Signature").FontSize(8).FontColor(TextMuted);
                    left.Item().PaddingTop(16).LineHorizontal(1).LineColor(BorderLight);
                    left.Item().PaddingTop(4).Text("Print Name").FontSize(8).FontColor(TextMuted);
                    left.Item().PaddingTop(16).LineHorizontal(1).LineColor(BorderLight);
                    left.Item().PaddingTop(4).Text("Date").FontSize(8).FontColor(TextMuted);
                });

                row.ConstantItem(40);

                // Received by
                row.RelativeItem().Column(right =>
                {
                    right.Item().Text("RECEIVED BY").Bold().FontSize(8).FontColor(Brand).LetterSpacing(0.1f);
                    right.Item().PaddingTop(40).LineHorizontal(1).LineColor(TextDark);
                    right.Item().PaddingTop(4).Text("Signature").FontSize(8).FontColor(TextMuted);
                    right.Item().PaddingTop(16).LineHorizontal(1).LineColor(BorderLight);
                    right.Item().PaddingTop(4).Text("Print Name").FontSize(8).FontColor(TextMuted);
                    right.Item().PaddingTop(16).LineHorizontal(1).LineColor(BorderLight);
                    right.Item().PaddingTop(4).Text("Date").FontSize(8).FontColor(TextMuted);
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(BorderLight);
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("RWMS — Restaurant Wholesale Management System").FontSize(7.5f).FontColor(TextMuted);
                row.ConstantItem(140).AlignRight().Text(t =>
                {
                    t.Span($"Generated {DateTime.Now:MMM d, yyyy}  |  Page ").FontSize(7.5f).FontColor(TextMuted);
                    t.CurrentPageNumber().FontSize(7.5f).FontColor(TextMuted);
                    t.Span(" of ").FontSize(7.5f).FontColor(TextMuted);
                    t.TotalPages().FontSize(7.5f).FontColor(TextMuted);
                });
            });
        });
    }

    private static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.ReadyForDelivery => "Ready for Delivery",
        _ => status.ToString()
    };

    private static string StatusColor(OrderStatus status) => status switch
    {
        OrderStatus.Pending => Colors.Amber.Darken2,
        OrderStatus.Accepted => Colors.Green.Darken2,
        OrderStatus.ReadyForDelivery => Colors.Blue.Darken2,
        OrderStatus.Rejected => Colors.Red.Darken2,
        _ => Colors.Grey.Darken1
    };
}
