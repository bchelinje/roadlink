namespace BeC.OpenId.Connect.Infrastructure.Email;

/// <summary>
/// Email service for sending transactional emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a simple email
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Send email with CC and BCC
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body, string? cc = null, string? bcc = null, bool isHtml = true);

    /// <summary>
    /// Send job confirmation email to customer
    /// </summary>
    Task SendJobConfirmationEmailAsync(string to, string customerName, string jobNumber, DateTime scheduledDate, string pickupAddress, string deliveryAddress);

    /// <summary>
    /// Send job assignment email to driver
    /// </summary>
    Task SendJobAssignmentEmailAsync(string to, string driverName, string jobNumber, DateTime scheduledDate, string pickupAddress, string deliveryAddress);

    /// <summary>
    /// Send job completion email to customer
    /// </summary>
    Task SendJobCompletionEmailAsync(string to, string customerName, string jobNumber, decimal totalAmount);

    /// <summary>
    /// Send payment receipt email
    /// </summary>
    Task SendPaymentReceiptEmailAsync(string to, string customerName, string jobNumber, decimal amount, string paymentMethod, string transactionId);

    /// <summary>
    /// Send document verification email to driver
    /// </summary>
    Task SendDocumentStatusEmailAsync(string to, string driverName, string documentType, string status, string? reason = null);

    /// <summary>
    /// Send driver payout notification
    /// </summary>
    Task SendPayoutNotificationEmailAsync(string to, string driverName, decimal amount, string period);

    /// <summary>
    /// Send welcome email to new users
    /// </summary>
    Task SendWelcomeEmailAsync(string to, string userName, string role);

    /// <summary>
    /// Send password reset email
    /// </summary>
    Task SendPasswordResetEmailAsync(string to, string userName, string resetLink);
}
