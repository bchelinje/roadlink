using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Jobs.Dtos;
using BeC.OpenId.Connect.Features.Payments.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Notifications.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Payments;
using BeC.OpenId.Connect.Features.Pricing.Services.Interfaces;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace BeC.OpenId.Connect.Features.Jobs.Controllers;

/// <summary>
/// Customer-facing job booking endpoints with integrated payment
/// </summary>
[ApiController]
[Route("api/customers/jobs")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Roles = "Customer")]
public class CustomerJobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    private readonly INotificationService _notificationService;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IPricingCalculatorService _pricingCalculator;

    public CustomerJobsController(
        ApplicationDbContext context,
        IActivityLogService activityLogService,
        INotificationService notificationService,
        IStripePaymentService stripePaymentService,
        IPricingCalculatorService pricingCalculator)
    {
        _context = context;
        _activityLogService = activityLogService;
        _notificationService = notificationService;
        _stripePaymentService = stripePaymentService;
        _pricingCalculator = pricingCalculator;
    }

    /// <summary>
    /// Book a job with payment (Customer endpoint)
    /// Creates job and captures payment in one transaction
    /// </summary>
    /// <remarks>
    /// This endpoint:
    /// 1. Calculates job pricing based on distance/vehicle type
    /// 2. Creates a Stripe payment intent (captures customer's card immediately)
    /// 3. Creates the job in "pending" status
    /// 4. Returns job details + payment client secret for frontend confirmation
    ///
    /// Frontend flow:
    /// 1. POST /api/customers/jobs/book with job details
    /// 2. Receive clientSecret in response
    /// 3. Use Stripe.js to confirm payment with clientSecret
    /// 4. Display confirmation to customer
    /// </remarks>
    [HttpPost("book")]
    [ProducesResponseType(typeof(BookJobResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookJobResponseDto>> BookJob([FromBody] BookJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Customer";

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            // 1. Calculate pricing
            var pricingResult = await _pricingCalculator.CalculatePriceAsync(new Features.Pricing.Services.PricingCalculationRequest
            {
                JobType = dto.JobType,
                VehicleType = dto.VehicleTypeRequired ?? "van",
                Distance = (double?)dto.Distance ?? 0,
                Duration = dto.EstimatedDuration ?? 60,
                ScheduledDate = dto.ScheduledDate
            });

            var jobAmount = pricingResult.TotalPrice;
            var platformFee = _stripePaymentService.CalculatePlatformFee(jobAmount);
            var driverEarnings = _stripePaymentService.CalculateDriverEarnings(jobAmount);

            // 2. Generate job number
            var jobNumber = await GenerateJobNumber();

            // 3. Create job
            var job = new Job
            {
                JobNumber = jobNumber,
                CustomerId = userId,
                CustomerName = userName,
                CustomerPhone = dto.CustomerPhone,
                CustomerEmail = userEmail ?? dto.CustomerEmail,
                JobType = dto.JobType,
                VehicleTypeRequired = dto.VehicleTypeRequired,
                Status = "pending",
                Priority = dto.Priority ?? "normal",
                ScheduledDate = dto.ScheduledDate,
                ScheduledTime = dto.ScheduledTime,
                EstimatedDuration = dto.EstimatedDuration ?? 60,
                PickupLocation = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Address = dto.PickupLocation,
                    Latitude = dto.PickupLatitude,
                    Longitude = dto.PickupLongitude
                }),
                DeliveryLocation = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Address = dto.DeliveryLocation,
                    Latitude = dto.DeliveryLatitude,
                    Longitude = dto.DeliveryLongitude
                }),
                Distance = dto.Distance,
                Items = dto.Items != null ? System.Text.Json.JsonSerializer.Serialize(dto.Items) : null,
                SpecialInstructions = dto.SpecialInstructions,
                StatusHistory = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        Status = "pending",
                        Timestamp = DateTime.UtcNow,
                        UpdatedBy = userId,
                        Notes = "Job booked by customer"
                    }
                }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            // 4. Create Stripe payment intent (captures card immediately)
            var paymentResult = await _stripePaymentService.CreatePaymentIntentAsync(
                job.Id,
                jobAmount,
                userId,
                $"Delivery from {dto.PickupLocation} to {dto.DeliveryLocation}"
            );
            var paymentIntentId = paymentResult.Item1;
            var clientSecret = paymentResult.Item2;

            // 5. Create payment record
            var payment = new Payment
            {
                PaymentNumber = GeneratePaymentNumber(),
                JobId = job.Id,
                JobNumber = job.JobNumber,
                CustomerId = userId,
                CustomerName = userName,
                CustomerEmail = userEmail ?? dto.CustomerEmail,
                Amount = jobAmount,
                TipAmount = 0m,
                PlatformFee = platformFee,
                DriverEarnings = driverEarnings,
                TotalAmount = jobAmount,
                Currency = "usd",
                Status = "pending", // Will change to "completed" via webhook
                PaymentMethod = "card",
                StripePaymentIntentId = paymentIntentId,
                Description = $"Job {jobNumber} - {dto.JobType}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // 6. Log activity
            await _activityLogService.LogActivityAsync(
                userId,
                "job.booked_with_payment",
                "Job",
                job.Id.ToString(),
                job.JobNumber,
                $"Job {job.JobNumber} booked by {userName} with payment ${jobAmount}",
                metadata: new Dictionary<string, object>
                {
                    { "Amount", jobAmount },
                    { "PlatformFee", platformFee },
                    { "DriverEarnings", driverEarnings },
                    { "PaymentIntentId", paymentIntentId }
                }
            );

            // 7. Send notification to admins
            await _notificationService.SendNotificationAsync(
                userId: "admin", // TODO: Send to all admins
                title: "New Job Booking",
                message: $"New job booking: {job.JobNumber} - {dto.JobType} - ${jobAmount}",
                type: "new_job_booking",
                entityType: "Job",
                entityId: job.Id.ToString()
            );

            // 8. Return response with client secret for frontend payment confirmation
            return CreatedAtAction(
                nameof(GetBookedJob),
                new { id = job.Id },
                new BookJobResponseDto
                {
                    JobId = job.Id,
                    JobNumber = job.JobNumber,
                    Amount = jobAmount,
                    PlatformFee = platformFee,
                    DriverEarnings = driverEarnings,
                    Currency = "usd",
                    PaymentId = payment.Id,
                    PaymentNumber = payment.PaymentNumber,
                    ClientSecret = clientSecret, // Frontend uses this to confirm payment
                    PublishableKey = "pk_test_...", // TODO: Get from settings
                    Job = MapJobToDto(job),
                    PricingBreakdown = new PricingBreakdownDto
                    {
                        BaseFare = pricingResult.BaseFare,
                        DistanceCharge = pricingResult.DistanceCharge,
                        TimeCharge = pricingResult.TimeCharge,
                        VehicleTypeCharge = pricingResult.VehicleTypeCharge,
                        SurgeMultiplier = pricingResult.SurgeMultiplier,
                        TotalPrice = pricingResult.TotalPrice
                    }
                });
        }
        catch (Exception ex)
        {
            await _activityLogService.LogActivityAsync(
                "job.booking_failed",
                "Job",
                null,
                null,
                $"Job booking failed: {ex.Message}",
                "ERROR",
                userId
            );

            return BadRequest(new { error = "Failed to book job with payment", details = ex.Message });
        }
    }

    /// <summary>
    /// Get booked job details (Customer)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDto>> GetBookedJob(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == id && j.CustomerId == userId);

        if (job == null)
            return NotFound("Job not found");

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Get my booked jobs (Customer)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<JobDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<JobDto>>> GetMyJobs(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.Jobs
            .Where(j => j.CustomerId == userId)
            .Include(j => j.Driver)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(j => j.Status == status);
        }

        var totalCount = await query.CountAsync();
        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(jobs.Select(MapJobToDto).ToList());
    }

    /// <summary>
    /// Cancel a booked job (triggers automatic refund)
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CancelBookedJob(Guid id, [FromBody] CancelJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.CustomerId == userId);

        if (job == null)
            return NotFound("Job not found");

        if (job.Status == "completed" || job.Status == "cancelled")
            return BadRequest($"Cannot cancel job with status: {job.Status}");

        var oldStatus = job.Status;
        job.Status = "cancelled";
        job.UpdatedAt = DateTime.UtcNow;

        // Add to status history
        var statusHistory = System.Text.Json.JsonSerializer.Deserialize<List<object>>(job.StatusHistory ?? "[]") ?? new List<object>();
        statusHistory.Add(new
        {
            Status = "cancelled",
            Timestamp = DateTime.UtcNow,
            UpdatedBy = userId,
            Notes = dto.Reason ?? "Cancelled by customer"
        });
        job.StatusHistory = System.Text.Json.JsonSerializer.Serialize(statusHistory);

        await _context.SaveChangesAsync();

        // Trigger automatic refund (handled by payment automation)
        // The JobsController already has this wired up, but we can trigger it here too
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.JobId == id && p.Status != "refunded");
        if (payment != null)
        {
            await _stripePaymentService.ProcessRefundAsync(
                payment.Id,
                partialAmount: null, // Full refund
                reason: dto.Reason ?? "Customer requested cancellation"
            );
        }

        await _activityLogService.LogActivityAsync(
            userId,
            "job.cancelled_by_customer",
            "Job",
            job.Id.ToString(),
            job.JobNumber,
            $"Job {job.JobNumber} cancelled by customer: {dto.Reason}",
            metadata: new Dictionary<string, object>
            {
                { "OldStatus", oldStatus },
                { "CancellationReason", dto.Reason ?? "Not specified" },
                { "RefundProcessed", payment != null }
            }
        );

        return Ok(new { message = "Job cancelled successfully. Refund will be processed.", jobNumber = job.JobNumber });
    }

    #region Helper Methods

    private async Task<string> GenerateJobNumber()
    {
        var date = DateTime.UtcNow;
        var prefix = $"JOB-{date:yyyyMMdd}";
        var count = await _context.Jobs.CountAsync(j => j.JobNumber.StartsWith(prefix));
        return $"{prefix}-{(count + 1):D4}";
    }

    private string GeneratePaymentNumber()
    {
        return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private JobDto MapJobToDto(Job job)
    {
        return new JobDto
        {
            Id = job.Id,
            JobNumber = job.JobNumber,
            CustomerId = job.CustomerId,
            CustomerName = job.CustomerName,
            CustomerPhone = job.CustomerPhone,
            CustomerEmail = job.CustomerEmail,
            DriverId = job.DriverId,
            DriverName = job.DriverName,
            JobType = job.JobType,
            VehicleTypeRequired = job.VehicleTypeRequired,
            Status = job.Status,
            Priority = job.Priority,
            ScheduledDate = job.ScheduledDate,
            ScheduledTime = job.ScheduledTime,
            EstimatedDuration = job.EstimatedDuration,
            ActualStartTime = job.ActualStartTime,
            ActualEndTime = job.ActualEndTime,
            PickupLocation = job.PickupLocation,
            DeliveryLocation = job.DeliveryLocation,
            Distance = job.Distance,
            Items = job.Items,
            SpecialInstructions = job.SpecialInstructions,
            InternalNotes = job.InternalNotes,
            StatusHistory = job.StatusHistory,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            CompletedAt = job.CompletedAt
        };
    }

    #endregion
}

#region DTOs

/// <summary>
/// Book job request (customer provides job details)
/// </summary>
public class BookJobDto
{
    public required string JobType { get; set; } // local_move, long_distance, commercial
    public string? VehicleTypeRequired { get; set; } // van, small_truck, large_truck
    public string? Priority { get; set; } // normal, urgent

    // Schedule
    public required DateTime ScheduledDate { get; set; }
    public required string ScheduledTime { get; set; }
    public int? EstimatedDuration { get; set; }

    // Customer contact
    public required string CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }

    // Locations
    public required string PickupLocation { get; set; }
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public required string DeliveryLocation { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public decimal? Distance { get; set; }

    // Job details
    public List<object>? Items { get; set; }
    public string? SpecialInstructions { get; set; }
}

/// <summary>
/// Book job response (includes payment client secret)
/// </summary>
public class BookJobResponseDto
{
    public Guid JobId { get; set; }
    public string JobNumber { get; set; } = string.Empty;

    // Payment details
    public decimal Amount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal DriverEarnings { get; set; }
    public string Currency { get; set; } = "usd";
    public Guid PaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;

    // Stripe payment confirmation (frontend uses this)
    public string ClientSecret { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;

    // Job details
    public JobDto Job { get; set; } = null!;

    // Pricing breakdown
    public PricingBreakdownDto? PricingBreakdown { get; set; }
}

/// <summary>
/// Pricing breakdown details
/// </summary>
public class PricingBreakdownDto
{
    public decimal BaseFare { get; set; }
    public decimal DistanceCharge { get; set; }
    public decimal TimeCharge { get; set; }
    public decimal VehicleTypeCharge { get; set; }
    public decimal SurgeMultiplier { get; set; }
    public decimal TotalPrice { get; set; }
}

#endregion
