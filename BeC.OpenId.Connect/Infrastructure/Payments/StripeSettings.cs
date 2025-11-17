namespace BeC.OpenId.Connect.Infrastructure.Payments;

/// <summary>
/// Stripe payment gateway configuration settings
/// </summary>
public class StripeSettings
{
    /// <summary>
    /// Stripe secret API key (server-side)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe publishable API key (client-side)
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook signature secret for verifying Stripe events
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Platform commission percentage (default 15%)
    /// </summary>
    public decimal PlatformFeePercent { get; set; } = 15m;

    /// <summary>
    /// Default currency code (ISO 4217)
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Default payment description
    /// </summary>
    public string PaymentDescription { get; set; } = "Delivery Service Payment";

    /// <summary>
    /// Number of days to hold funds in escrow before auto-release (if not completed)
    /// </summary>
    public int EscrowHoldDays { get; set; } = 1;

    /// <summary>
    /// Automatic payout schedule (daily, weekly, monthly)
    /// </summary>
    public string AutoPayoutSchedule { get; set; } = "weekly";
}
