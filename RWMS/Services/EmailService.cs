using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using QuestPDF.Fluent;
using RWMS.Models.ViewModels.Delivery;
using RWMS.Services.Implementations;
using RWMS.Services.Interfaces;

namespace RWMS.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var smtpSettings = _configuration.GetSection("Smtp");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            smtpSettings["SenderName"] ?? "RWMS",
            smtpSettings["SenderEmail"] ?? string.Empty));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(
            smtpSettings["Host"],
            int.Parse(smtpSettings["Port"] ?? "587"),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            smtpSettings["Username"],
            smtpSettings["Password"]);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {Email} — Subject: {Subject}", toEmail, subject);
    }

    public async Task SendOrderConfirmationAsync(string toEmail, string toName, int orderId)
    {
        var subject = $"Order #{orderId} Received — RWMS";
        var body = $"""
            <p>Hi {toName},</p>
            <p>Your order <strong>#{orderId}</strong> has been received and is currently <strong>pending review</strong>.</p>
            <p>You will be notified once the status is updated.</p>
            <br/>
            <p>Restaurant Wholesale Management System</p>
            """;

        await SendEmailAsync(toEmail, toName, subject, body);
    }

    public async Task SendOrderStatusUpdateAsync(string toEmail, string toName, int orderId, string newStatus)
    {
        var subject = $"Order #{orderId} Status Updated — RWMS";
        var body = $"""
            <p>Hi {toName},</p>
            <p>The status of your order <strong>#{orderId}</strong> has been updated to: <strong>{newStatus}</strong>.</p>
            <p>Log in to your account to view full order details.</p>
            <br/>
            <p>Restaurant Wholesale Management System</p>
            """;

        await SendEmailAsync(toEmail, toName, subject, body);
    }

    public async Task SendDeliveryNotificationAsync(string toEmail, string toName, DeliveryDetailViewModel delivery)
    {
        var subject = $"Your Delivery Run — {delivery.Date:dddd, dd MMM yyyy}";

        var stopLines = string.Join("", delivery.Stops.Select((stop, i) =>
            $"""
            <div style="margin-bottom:12px;padding:10px;border:1px solid #e0e0e0;border-radius:8px;">
              <strong>Stop {i + 1} — {stop.CustomerName}</strong><br/>
              {(string.IsNullOrWhiteSpace(stop.CustomerAddress) ? "" : $"<span style='color:#666'>{stop.CustomerAddress}</span><br/>")}
              <ul style="margin:8px 0 0 0;padding-left:20px;">
                {string.Join("", stop.ItemSummaries.Select(s => $"<li>{s}</li>"))}
              </ul>
            </div>
            """));

        var body = $"""
            <p>Hi {toName},</p>
            <p>Here is your delivery run for <strong>{delivery.Date:dddd, dd MMM yyyy}</strong> — {delivery.Stops.Count} stop(s).</p>
            {stopLines}
            {(string.IsNullOrWhiteSpace(delivery.Notes) ? "" : $"<p><strong>Notes:</strong> {delivery.Notes}</p>")}
            <br/>
            <p>Restaurant Wholesale Management System</p>
            """;

        var pdfBytes = new DeliverySheetPdf(delivery).GeneratePdf();

        var smtpSettings = _configuration.GetSection("Smtp");
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtpSettings["SenderName"] ?? "RWMS", smtpSettings["SenderEmail"] ?? string.Empty));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = body };
        builder.Attachments.Add($"delivery-{delivery.Date:yyyy-MM-dd}.pdf", pdfBytes, new MimeKit.ContentType("application", "pdf"));
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpSettings["Host"], int.Parse(smtpSettings["Port"] ?? "587"), SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Delivery notification sent to {Email} for {Date}", toEmail, delivery.Date);
    }

    public async Task SendPasswordResetAsync(string toEmail, string toName, string resetUrl)
    {
        var subject = "Reset your RWMS password";
        var body = $"""
            <p>Hi {toName},</p>
            <p>We received a request to reset your password. Click the link below to set a new one.</p>
            <p><a href="{resetUrl}">Reset Password</a></p>
            <p>This link expires in 1 hour. If you didn't request this, you can ignore this email.</p>
            <br/>
            <p>Restaurant Wholesale Management System</p>
            """;

        await SendEmailAsync(toEmail, toName, subject, body);
    }

    public async Task SendInviteAsync(string toEmail, string role, string inviteUrl)
    {
        var subject = "You've been invited to join RWMS";
        var body = $"""
            <p>You have been invited to join the Restaurant Wholesale Management System as a <strong>{role}</strong>.</p>
            <p>Click the link below to create your account. This invitation expires in 7 days.</p>
            <p><a href="{inviteUrl}">{inviteUrl}</a></p>
            <br/>
            <p>Restaurant Wholesale Management System</p>
            """;

        await SendEmailAsync(toEmail, toEmail, subject, body);
    }
}
