namespace ECommerce.Notification.Services;

/// <summary>
/// Simulates sending emails by logging to console and Seq.
///
/// WHY SIMULATION:
/// Setting up SMTP or SendGrid requires extra config.
/// For learning purposes we just log what WOULD be sent.
/// The architecture is identical to a real implementation —
/// just replace the Log.Information with actual email sending.
///
/// TO USE REAL EMAIL:
/// Install: dotnet add package SendGrid
/// Replace log statements with:
///   var client = new SendGridClient(apiKey);
///   var msg = MailHelper.CreateSingleEmail(from, to, subject, text, html);
///   await client.SendEmailAsync(msg);
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendOrderConfirmedAsync(
        string toEmail,
        string fullName,
        Guid orderId,
        decimal total)
    {
        // Simulate email sending delay
        await Task.Delay(100);

        _logger.LogInformation(
            "EMAIL SENT ─ Order Confirmed\n" +
            "  To:      {Email}\n" +
            "  Name:    {Name}\n" +
            "  OrderId: {OrderId}\n" +
            "  Total:   {Total}\n" +
            "  Subject: Your order has been confirmed!\n" +
            "  Body:    Hi {Name}, your order #{OrderId} " +
            "for ₹{Total} has been confirmed and is being prepared.",
            toEmail, fullName, orderId, total, fullName, orderId, total);
    }

    public async Task SendOrderCancelledAsync(
        string toEmail,
        string fullName,
        Guid orderId,
        string reason)
    {
        await Task.Delay(100);

        _logger.LogInformation(
            "EMAIL SENT ─ Order Cancelled\n" +
            "  To:      {Email}\n" +
            "  Name:    {Name}\n" +
            "  OrderId: {OrderId}\n" +
            "  Reason:  {Reason}\n" +
            "  Subject: Your order has been cancelled\n" +
            "  Body:    Hi {Name}, unfortunately your order #{OrderId} " +
            "has been cancelled. Reason: {Reason}. " +
            "You have not been charged.",
            toEmail, fullName, orderId, reason,
            fullName, orderId, reason);
    }

    public async Task SendOrderShippedAsync(
        string toEmail,
        string fullName,
        Guid orderId,
        string trackingNumber,
        string courier)
    {
        await Task.Delay(100);

        _logger.LogInformation(
            "EMAIL SENT ─ Order Shipped\n" +
            "  To:       {Email}\n" +
            "  Name:     {Name}\n" +
            "  OrderId:  {OrderId}\n" +
            "  Tracking: {TrackingNumber}\n" +
            "  Courier:  {Courier}\n" +
            "  Subject:  Your order is on the way!\n" +
            "  Body:     Hi {Name}, your order #{OrderId} " +
            "has been shipped via {Courier}. " +
            "Track it with: {TrackingNumber}",
            toEmail, fullName, orderId, trackingNumber, courier,
            fullName, orderId, courier, trackingNumber);
    }
}