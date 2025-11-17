using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Payments.Dtos;
using BeC.OpenId.Connect.Dto;
using Microsoft.EntityFrameworkCore;

namespace BeC.OpenId.Connect.Infrastructure.Payments;

/// <summary>
/// Service that automates payment operations based on job status changes
/// - Job booked → Create payment
/// - Job starts → Hold funds in escrow
/// - Job completes → Release funds with commission split
/// - Job cancelled → Process refund
/// </summary>
public interface IJobPaymentAutomationService
{
    /// <summary>
    /// Handle payment when new job is created/booked
    /// </summary>
    Task<Payment> CreatePaymentForJobAsync(Guid jobId, string customerId, decimal amount);

    /// <summary>
    /// Handle escrow when job status changes to in_progress
    /// </summary>
    Task HandleJobStartedAsync(Guid jobId);

    /// <summary>
    /// Handle fund release when job status changes to completed
    /// </summary>
    Task HandleJobCompletedAsync(Guid jobId);

    /// <summary>
    /// Handle refund when job status changes to cancelled
    /// </summary>
    Task HandleJobCancelledAsync(Guid jobId, string cancellationReason);
}

public class JobPaymentAutomationService : IJobPaymentAutomationService
{
    private readonly ApplicationDbContext _context;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly ILogger<JobPaymentAutomationService> _logger;

    public JobPaymentAutomationService(
        ApplicationDbContext context,
        IStripePaymentService stripePaymentService,
        ILogger<JobPaymentAutomationService> logger)
    {
        _context = context;
        _stripePaymentService = stripePaymentService;
        _logger = logger;
    }

    /// <summary>
    /// Create payment intent when job is booked - captures customer's payment method
    /// </summary>
    public async Task<Payment> CreatePaymentForJobAsync(Guid jobId, string customerId, decimal amount)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            // Create Stripe payment intent
            var (paymentIntentId, clientSecret) = await _stripePaymentService.CreatePaymentIntentAsync(
                jobId,
                amount,
                customerId,
                $"Delivery from {job.PickupLocation} to {job.DeliveryLocation}");

            // Calculate commission split
            var platformFee = _stripePaymentService.CalculatePlatformFee(amount);
            var driverEarnings = _stripePaymentService.CalculateDriverEarnings(amount);

            // Create payment record
            var payment = new Payment
            {
                PaymentNumber = GeneratePaymentNumber(),
                JobId = jobId,
                CustomerId = customerId,
                Amount = amount,
                TipAmount = 0m,
                PlatformFee = platformFee, // 15% commission
                DriverEarnings = driverEarnings, // 85% to driver
                TotalAmount = amount,
                Currency = "usd",
                Status = "pending", // Pending until customer confirms payment
                StripePaymentIntentId = paymentIntentId,
                PaymentMethod = "card",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created payment {PaymentNumber} for job {JobId}: ${Amount} (Platform: ${PlatformFee}, Driver: ${DriverEarnings})",
                payment.PaymentNumber, jobId, amount, platformFee, driverEarnings);

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment for job {JobId}", jobId);
            throw;
        }
    }

    /// <summary>
    /// When job starts (status → in_progress), hold funds in escrow
    /// Funds are already captured, just update status to "processing"
    /// </summary>
    public async Task HandleJobStartedAsync(Guid jobId)
    {
        try
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.JobId == jobId && p.Status == "pending");

            if (payment == null)
            {
                _logger.LogWarning("No pending payment found for job {JobId}", jobId);
                return;
            }

            // Hold funds in escrow
            var success = await _stripePaymentService.HoldFundsInEscrowAsync(payment.Id);

            if (success)
            {
                _logger.LogInformation(
                    "Job {JobId} started - payment {PaymentId} funds held in escrow",
                    jobId, payment.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hold funds in escrow for job {JobId}", jobId);
            throw;
        }
    }

    /// <summary>
    /// When job completes (status → completed), release funds with commission split
    /// Platform takes 15%, driver gets 85%
    /// </summary>
    public async Task HandleJobCompletedAsync(Guid jobId)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found", jobId);
                return;
            }

            if (!job.DriverId.HasValue)
            {
                _logger.LogWarning("Job {JobId} has no assigned driver", jobId);
                return;
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.JobId == jobId && p.Status == "processing");

            if (payment == null)
            {
                _logger.LogWarning("No payment in escrow found for job {JobId}", jobId);
                return;
            }

            // Release funds from escrow with commission split
            var success = await _stripePaymentService.ReleaseFundsFromEscrowAsync(
                payment.Id,
                jobId,
                job.DriverId.Value);

            if (success)
            {
                _logger.LogInformation(
                    "Job {JobId} completed - released ${Amount} from escrow (Platform: ${PlatformFee}, Driver: ${DriverEarnings})",
                    jobId, payment.Amount, payment.PlatformFee, payment.DriverEarnings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release funds for completed job {JobId}", jobId);
            throw;
        }
    }

    /// <summary>
    /// When job is cancelled, process full refund to customer
    /// </summary>
    public async Task HandleJobCancelledAsync(Guid jobId, string cancellationReason)
    {
        try
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.JobId == jobId &&
                    (p.Status == "pending" || p.Status == "processing"));

            if (payment == null)
            {
                _logger.LogInformation("No refundable payment found for cancelled job {JobId}", jobId);
                return;
            }

            // Process full refund
            var refundId = await _stripePaymentService.ProcessRefundAsync(
                payment.Id,
                partialAmount: null, // Full refund
                reason: cancellationReason);

            _logger.LogInformation(
                "Job {JobId} cancelled - processed refund {RefundId} for ${Amount}",
                jobId, refundId, payment.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process refund for cancelled job {JobId}", jobId);
            throw;
        }
    }

    /// <summary>
    /// Generate unique payment number
    /// </summary>
    private string GeneratePaymentNumber()
    {
        return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
