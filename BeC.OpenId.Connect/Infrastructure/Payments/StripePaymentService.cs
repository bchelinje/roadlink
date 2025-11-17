using BeC.Common.Data;
using BeC.Common.Data.Repositories.Interfaces;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Payments.Dtos;
using BeC.OpenId.Connect.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using PayoutDto = BeC.OpenId.Connect.Features.Payments.Dtos.Payout;

namespace BeC.OpenId.Connect.Infrastructure.Payments;

/// <summary>
/// Stripe payment service implementing escrow marketplace functionality
/// Handles: payment capture, escrow holding, commission splits, driver payouts, refunds
/// </summary>
public class StripePaymentService : IStripePaymentService
{
    private readonly StripeSettings _settings;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<PayoutDto, Guid> _payoutRepository;
    private readonly IRepository<Earning, Guid> _earningRepository;
    private readonly IRepository<Job, Guid> _jobRepository;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IOptions<StripeSettings> settings,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<PayoutDto, Guid> payoutRepository,
        IRepository<Earning, Guid> earningRepository,
        IRepository<Job, Guid> jobRepository,
        ILogger<StripePaymentService> logger)
    {
        _settings = settings.Value;
        _paymentRepository = paymentRepository;
        _payoutRepository = payoutRepository;
        _earningRepository = earningRepository;
        _jobRepository = jobRepository;
        _logger = logger;

        // Configure Stripe API key
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    /// <summary>
    /// Create payment intent for job booking - captures funds immediately into escrow
    /// </summary>
    public async Task<(string PaymentIntentId, string ClientSecret)> CreatePaymentIntentAsync(
        Guid jobId,
        decimal amount,
        string customerId,
        string description)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = ConvertToStripeAmount(amount),
                Currency = _settings.Currency,
                Description = $"{description} - Job #{jobId}",
                CaptureMethod = "automatic", // Capture funds immediately
                Metadata = new Dictionary<string, string>
                {
                    { "job_id", jobId.ToString() },
                    { "customer_id", customerId },
                    { "platform_fee_percent", _settings.PlatformFeePercent.ToString() }
                }
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created payment intent {PaymentIntentId} for job {JobId}, amount {Amount}",
                intent.Id, jobId, amount);

            return (intent.Id, intent.ClientSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create payment intent for job {JobId}", jobId);
            throw new InvalidOperationException($"Payment creation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Confirm payment intent after customer provides payment method
    /// </summary>
    public async Task<bool> ConfirmPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var intent = await service.ConfirmAsync(paymentIntentId);

            _logger.LogInformation(
                "Payment intent {PaymentIntentId} confirmed with status {Status}",
                paymentIntentId, intent.Status);

            return intent.Status == "succeeded" || intent.Status == "processing";
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to confirm payment intent {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }

    /// <summary>
    /// Hold funds in escrow when job starts (already captured, just update status)
    /// </summary>
    public async Task<bool> HoldFundsInEscrowAsync(Guid paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", paymentId);
                return false;
            }

            // Funds are already captured, just mark as held in escrow
            payment.Status = "processing"; // Processing = funds in escrow
            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation(
                "Payment {PaymentId} funds held in escrow (status: processing)",
                paymentId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hold funds in escrow for payment {PaymentId}", paymentId);
            return false;
        }
    }

    /// <summary>
    /// Release funds from escrow when job completes - splits commission and creates driver earning
    /// This is where the magic happens: 15% to platform, 85% to driver
    /// </summary>
    public async Task<bool> ReleaseFundsFromEscrowAsync(Guid paymentId, Guid jobId, Guid driverId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", paymentId);
                return false;
            }

            if (payment.Status != "processing")
            {
                _logger.LogWarning(
                    "Cannot release funds - payment {PaymentId} not in escrow (status: {Status})",
                    paymentId, payment.Status);
                return false;
            }

            // Calculate commission split
            var totalAmount = payment.Amount + (payment.TipAmount ?? 0);
            var platformFee = CalculatePlatformFee(payment.Amount);
            var driverEarnings = CalculateDriverEarnings(payment.Amount) + (payment.TipAmount ?? 0);

            // Update payment with split details
            payment.PlatformFee = platformFee;
            payment.DriverEarnings = driverEarnings;
            payment.Status = "completed";
            payment.PaidAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            // Create earning record for driver
            var earning = new Earning
            {
                DriverId = driverId,
                JobId = jobId,
                PaymentId = paymentId,
                BaseAmount = payment.Amount,
                BonusAmount = 0m,
                TipAmount = payment.TipAmount ?? 0m,
                NetAmount = driverEarnings,
                PaymentStatus = "pending", // Will be paid out in batch
                EarnedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _earningRepository.AddAsync(earning);

            _logger.LogInformation(
                "Released funds from escrow for payment {PaymentId}: Platform fee ${PlatformFee}, Driver earnings ${DriverEarnings}",
                paymentId, platformFee, driverEarnings);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release funds from escrow for payment {PaymentId}", paymentId);
            return false;
        }
    }

    /// <summary>
    /// Process full or partial refund when job is cancelled
    /// </summary>
    public async Task<string> ProcessRefundAsync(Guid paymentId, decimal? partialAmount = null, string? reason = null)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {paymentId} not found");
            }

            if (string.IsNullOrEmpty(payment.StripePaymentIntentId))
            {
                throw new InvalidOperationException("Payment has no Stripe payment intent ID");
            }

            // Determine refund amount
            var refundAmount = partialAmount ?? payment.Amount;

            var options = new RefundCreateOptions
            {
                PaymentIntent = payment.StripePaymentIntentId,
                Amount = ConvertToStripeAmount(refundAmount),
                Reason = reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    _ => "requested_by_customer"
                },
                Metadata = new Dictionary<string, string>
                {
                    { "payment_id", paymentId.ToString() },
                    { "job_id", payment.JobId.ToString() }
                }
            };

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            // Update payment status
            payment.RefundAmount = refundAmount;
            payment.Status = partialAmount.HasValue ? "partially_refunded" : "refunded";
            payment.RefundedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;
            payment.StripeRefundId = refund.Id;

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation(
                "Processed refund {RefundId} for payment {PaymentId}, amount ${Amount}",
                refund.Id, paymentId, refundAmount);

            return refund.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for payment {PaymentId}", paymentId);
            throw new InvalidOperationException($"Refund failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create payout to driver (batch payout on schedule)
    /// In production, this would use Stripe Connect for direct bank transfers
    /// </summary>
    public async Task<string> CreateDriverPayoutAsync(Guid driverId, decimal amount, string description)
    {
        try
        {
            // Get all pending earnings for this driver
            var pendingEarnings = await _earningRepository
                .AsQueryable()
                .Where(e => e.DriverId == driverId && e.PaymentStatus == "pending")
                .ToListAsync();

            if (!pendingEarnings.Any())
            {
                _logger.LogWarning("No pending earnings found for driver {DriverId}", driverId);
                return string.Empty;
            }

            var totalAmount = pendingEarnings.Sum(e => e.NetAmount);

            // NOTE: In production, you would create a Stripe Connect transfer here
            // For now, we'll create a payout record in our system
            var payout = new PayoutDto
            {
                PayoutNumber = GeneratePayoutNumber(),
                DriverId = driverId,
                Amount = totalAmount,
                Currency = _settings.Currency,
                Status = "pending",
                PeriodStart = pendingEarnings.Min(e => e.EarnedAt),
                PeriodEnd = pendingEarnings.Max(e => e.EarnedAt),
                TotalJobs = pendingEarnings.Count,
                PaymentIds = System.Text.Json.JsonSerializer.Serialize(
                    pendingEarnings.Select(e => e.PaymentId).ToList()),
                CreatedAt = DateTime.UtcNow
            };

            await _payoutRepository.AddAsync(payout);

            // Mark earnings as paid
            foreach (var earning in pendingEarnings)
            {
                earning.PaymentStatus = "paid";
                earning.PaidAt = DateTime.UtcNow;
                earning.PayoutId = payout.Id;
                await _earningRepository.UpdateAsync(earning);
            }

            _logger.LogInformation(
                "Created payout {PayoutNumber} for driver {DriverId}, amount ${Amount} ({Count} jobs)",
                payout.PayoutNumber, driverId, totalAmount, pendingEarnings.Count);

            return payout.PayoutNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payout for driver {DriverId}", driverId);
            throw;
        }
    }

    /// <summary>
    /// Calculate platform commission (15% by default)
    /// </summary>
    public decimal CalculatePlatformFee(decimal amount)
    {
        return Math.Round(amount * (_settings.PlatformFeePercent / 100m), 2);
    }

    /// <summary>
    /// Calculate driver earnings (85% by default)
    /// </summary>
    public decimal CalculateDriverEarnings(decimal amount)
    {
        var platformFee = CalculatePlatformFee(amount);
        return amount - platformFee;
    }

    /// <summary>
    /// Verify Stripe webhook signature for security
    /// </summary>
    public bool VerifyWebhookSignature(string json, string signature)
    {
        try
        {
            EventUtility.ConstructEvent(json, signature, _settings.WebhookSecret);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Webhook signature verification failed");
            return false;
        }
    }

    /// <summary>
    /// Handle Stripe webhook events (payment succeeded, failed, refunded, etc.)
    /// </summary>
    public async Task HandleWebhookEventAsync(string json)
    {
        try
        {
            var stripeEvent = EventUtility.ParseEvent(json);

            _logger.LogInformation("Processing Stripe webhook event: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:
                    await HandlePaymentSucceededAsync(stripeEvent);
                    break;

                case Events.PaymentIntentPaymentFailed:
                    await HandlePaymentFailedAsync(stripeEvent);
                    break;

                case Events.ChargeRefunded:
                    await HandleChargeRefundedAsync(stripeEvent);
                    break;

                case Events.PayoutPaid:
                    await HandlePayoutPaidAsync(stripeEvent);
                    break;

                case Events.PayoutFailed:
                    await HandlePayoutFailedAsync(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook event");
            throw;
        }
    }

    #region Webhook Event Handlers

    private async Task HandlePaymentSucceededAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var jobId = paymentIntent.Metadata.GetValueOrDefault("job_id");
        if (string.IsNullOrEmpty(jobId)) return;

        // Find payment by Stripe payment intent ID
        var payment = await _paymentRepository
            .AsQueryable()
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntent.Id);

        if (payment != null)
        {
            payment.Status = "completed";
            payment.StripeChargeId = paymentIntent.LatestChargeId;
            payment.PaidAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation(
                "Payment {PaymentId} marked as completed via webhook",
                payment.Id);
        }
    }

    private async Task HandlePaymentFailedAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        var payment = await _paymentRepository
            .AsQueryable()
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntent.Id);

        if (payment != null)
        {
            payment.Status = "failed";
            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogWarning(
                "Payment {PaymentId} marked as failed via webhook",
                payment.Id);
        }
    }

    private async Task HandleChargeRefundedAsync(Event stripeEvent)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null) return;

        var payment = await _paymentRepository
            .AsQueryable()
            .FirstOrDefaultAsync(p => p.StripeChargeId == charge.Id);

        if (payment != null)
        {
            payment.RefundAmount = ConvertFromStripeAmount(charge.AmountRefunded);
            payment.Status = charge.Refunded ? "refunded" : "partially_refunded";
            payment.RefundedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation(
                "Payment {PaymentId} refund processed via webhook, amount ${Amount}",
                payment.Id, payment.RefundAmount);
        }
    }

    private async Task HandlePayoutPaidAsync(Event stripeEvent)
    {
        // Handle when Stripe processes a payout to driver's bank account
        var stripePayout = stripeEvent.Data.Object as Stripe.Payout;
        if (stripePayout == null) return;

        var payout = await _payoutRepository
            .AsQueryable()
            .FirstOrDefaultAsync(p => p.StripePayoutId == stripePayout.Id);

        if (payout != null)
        {
            payout.Status = "paid";
            payout.PaidAt = DateTime.UtcNow;

            await _payoutRepository.UpdateAsync(payout);

            _logger.LogInformation(
                "Payout {PayoutId} marked as paid via webhook",
                payout.Id);
        }
    }

    private async Task HandlePayoutFailedAsync(Event stripeEvent)
    {
        var stripePayout = stripeEvent.Data.Object as Stripe.Payout;
        if (stripePayout == null) return;

        var payout = await _payoutRepository
            .AsQueryable()
            .FirstOrDefaultAsync(p => p.StripePayoutId == stripePayout.Id);

        if (payout != null)
        {
            payout.Status = "failed";

            await _payoutRepository.UpdateAsync(payout);

            _logger.LogWarning(
                "Payout {PayoutId} failed via webhook",
                payout.Id);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Convert decimal amount to Stripe's smallest currency unit (cents)
    /// </summary>
    private long ConvertToStripeAmount(decimal amount)
    {
        return (long)(amount * 100);
    }

    /// <summary>
    /// Convert Stripe amount (cents) to decimal
    /// </summary>
    private decimal ConvertFromStripeAmount(long amount)
    {
        return amount / 100m;
    }

    /// <summary>
    /// Generate unique payout number
    /// </summary>
    private string GeneratePayoutNumber()
    {
        return $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    #endregion
}
