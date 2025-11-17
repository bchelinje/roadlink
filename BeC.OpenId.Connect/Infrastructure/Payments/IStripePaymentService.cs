using BeC.OpenId.Connect.Features.Payments.Dtos;

namespace BeC.OpenId.Connect.Infrastructure.Payments;

/// <summary>
/// Service for handling Stripe payment operations with escrow marketplace functionality
/// </summary>
public interface IStripePaymentService
{
    /// <summary>
    /// Create a payment intent for a job booking (captures funds immediately)
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="amount">Total amount to charge (including platform fee)</param>
    /// <param name="customerId">Customer's user ID</param>
    /// <param name="description">Payment description</param>
    /// <returns>Payment intent ID and client secret</returns>
    Task<(string PaymentIntentId, string ClientSecret)> CreatePaymentIntentAsync(
        Guid jobId,
        decimal amount,
        string customerId,
        string description);

    /// <summary>
    /// Confirm a payment intent (called after customer provides payment method)
    /// </summary>
    Task<bool> ConfirmPaymentIntentAsync(string paymentIntentId);

    /// <summary>
    /// Hold funds in escrow when job starts
    /// </summary>
    Task<bool> HoldFundsInEscrowAsync(Guid paymentId);

    /// <summary>
    /// Release funds from escrow when job completes (splits commission and driver earnings)
    /// </summary>
    Task<bool> ReleaseFundsFromEscrowAsync(Guid paymentId, Guid jobId, Guid driverId);

    /// <summary>
    /// Process refund when job is cancelled
    /// </summary>
    Task<string> ProcessRefundAsync(Guid paymentId, decimal? partialAmount = null, string? reason = null);

    /// <summary>
    /// Create a payout to driver's bank account or debit card
    /// </summary>
    Task<string> CreateDriverPayoutAsync(Guid driverId, decimal amount, string description);

    /// <summary>
    /// Get platform commission amount from total payment
    /// </summary>
    decimal CalculatePlatformFee(decimal amount);

    /// <summary>
    /// Get driver earnings after platform fee
    /// </summary>
    decimal CalculateDriverEarnings(decimal amount);

    /// <summary>
    /// Verify Stripe webhook signature
    /// </summary>
    bool VerifyWebhookSignature(string json, string signature);

    /// <summary>
    /// Handle Stripe webhook event
    /// </summary>
    Task HandleWebhookEventAsync(string json);
}
