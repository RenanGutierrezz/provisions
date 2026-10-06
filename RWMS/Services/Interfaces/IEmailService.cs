using RWMS.Models.ViewModels.Delivery;

namespace RWMS.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
    Task SendOrderConfirmationAsync(string toEmail, string toName, int orderId);
    Task SendOrderStatusUpdateAsync(string toEmail, string toName, int orderId, string newStatus);
    Task SendInviteAsync(string toEmail, string role, string inviteUrl);
    Task SendDeliveryNotificationAsync(string toEmail, string toName, DeliveryDetailViewModel delivery);
    Task SendPasswordResetAsync(string toEmail, string toName, string resetUrl);
}
