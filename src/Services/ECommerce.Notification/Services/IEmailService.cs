namespace ECommerce.Notification.Services;

/// <summary>
/// Defines email sending operations.
///
/// WHY AN INTERFACE:
/// In development we log emails to console instead of
/// actually sending them — no SMTP config needed.
/// In production we swap to a real email provider
/// (SendGrid, Mailgun, SMTP) without touching consumers.
/// </summary>
public interface IEmailService
{
    Task SendOrderConfirmedAsync(
        string toEmail,
        string fullName,
        Guid orderId,
        decimal total);

    Task SendOrderCancelledAsync(
        string toEmail,
        string fullName,
        Guid orderId,
        string reason);

    Task SendOrderShippedAsync(
        string toEmail,
        string fullName,
        Guid orderId,
        string trackingNumber,
        string courier);
}