using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Payments.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Authorization;

namespace BeC.OpenId.Connect.Features.Payments.Controllers;

/// <summary>
/// Payment and payout management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    #region Payment CRUD

    /// <summary>
    /// Create a payment
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Payment), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] CreatePaymentDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Generate unique payment number
        var paymentNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // Calculate platform fee and driver earnings
        var platformFeePercentage = 0.15m; // 15% platform fee
        var platformFee = request.Amount * platformFeePercentage;
        var driverEarnings = request.Amount - platformFee + (request.TipAmount ?? 0);

        var payment = new Payment
        {
            PaymentNumber = paymentNumber,
            CustomerId = request.CustomerId ?? userId,
            CustomerName = user.DisplayName ?? user.UserName ?? user.Email ?? "Unknown",
            CustomerEmail = user.Email ?? "unknown@example.com",
            DriverId = request.DriverId,
            JobId = request.JobId,
            Amount = request.Amount,
            TipAmount = request.TipAmount ?? 0,
            PlatformFee = platformFee,
            DriverEarnings = driverEarnings,
            TotalAmount = request.Amount + (request.TipAmount ?? 0),
            Currency = request.Currency ?? "GBP",
            PaymentMethod = request.PaymentMethod,
            PaymentMethodDetails = request.PaymentMethodDetails,
            Status = "pending",
            Description = request.Description,
            Notes = request.Notes,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
        };

        // If driver is specified, get driver details
        if (request.DriverId.HasValue)
        {
            var driver = await _context.Drivers.FindAsync(request.DriverId.Value);
            if (driver != null)
            {
                payment.DriverName = $"{driver.FirstName} {driver.LastName}";
            }
        }

        // If job is specified, get job details
        if (request.JobId.HasValue)
        {
            var job = await _context.Jobs.FindAsync(request.JobId.Value);
            if (job != null)
            {
                payment.JobNumber = job.JobNumber;
            }
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            userId,
            "payment_created",
            "Payment",
            payment.Id.ToString(),
            paymentNumber,
            $"Payment created: {payment.TotalAmount:C} {payment.Currency}"
        );

        return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
    }

    /// <summary>
    /// Get payment by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Payment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Payment>> GetPayment(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var payment = await _context.Payments
            .Include(p => p.Driver)
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
            return NotFound();

        // Check permissions
        var user = await _userManager.FindByIdAsync(userId);
        var userRoles = await _userManager.GetRolesAsync(user!);
        var isAdmin = userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.SuperAdmin);
        var isCustomer = payment.CustomerId == userId;

        var isDriver = false;
        if (payment.DriverId.HasValue && payment.Driver != null)
        {
            isDriver = payment.Driver.UserId == userId;
        }

        if (!isAdmin && !isCustomer && !isDriver)
            return Forbid("You can only view payments you're involved in");

        return Ok(payment);
    }

    /// <summary>
    /// Get payments for a specific job
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    [ProducesResponseType(typeof(List<Payment>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Payment>>> GetJobPayments(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
            return NotFound("Job not found");

        // Check if user is involved in this job
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        var isCustomer = job.CustomerId == userId;
        var isDriver = driver != null && job.DriverId == driver.Id;

        var user = await _userManager.FindByIdAsync(userId);
        var userRoles = await _userManager.GetRolesAsync(user!);
        var isAdmin = userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.SuperAdmin);

        if (!isAdmin && !isCustomer && !isDriver)
            return Forbid("You can only view payments for jobs you're involved in");

        var payments = await _context.Payments
            .Where(p => p.JobId == jobId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(payments);
    }

    #endregion

    #region Customer Endpoints

    /// <summary>
    /// Get my payment history (Customer)
    /// </summary>
    [HttpGet("~/api/customers/me/payments")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(List<Payment>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Payment>>> GetMyPayments(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = _context.Payments
            .Where(p => p.CustomerId == userId)
            .Include(p => p.Driver)
            .Include(p => p.Job)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var totalCount = await query.CountAsync();
        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(payments);
    }

    #endregion

    #region Driver Endpoints

    /// <summary>
    /// Get my earnings (Driver)
    /// </summary>
    [HttpGet("~/api/drivers/me/earnings")]
    [Authorize(Roles = Roles.Driver)]
    [ProducesResponseType(typeof(EarningsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EarningsDto>> GetMyEarnings(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound("Driver profile not found");

        var query = _context.Payments
            .Where(p => p.DriverId == driver.Id && p.Status == "completed")
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(p => p.PaidAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.PaidAt <= endDate.Value);
        }

        var payments = await query.ToListAsync();

        var earnings = new EarningsDto
        {
            TotalEarnings = payments.Sum(p => p.DriverEarnings),
            TotalTips = payments.Sum(p => p.TipAmount ?? 0),
            TotalJobs = payments.Count,
            AverageEarningsPerJob = payments.Any() ? payments.Average(p => p.DriverEarnings) : 0,
            PeriodStart = startDate ?? payments.MinBy(p => p.CreatedAt)?.CreatedAt,
            PeriodEnd = endDate ?? payments.MaxBy(p => p.CreatedAt)?.CreatedAt
        };

        return Ok(earnings);
    }

    /// <summary>
    /// Get my payout history (Driver)
    /// </summary>
    [HttpGet("~/api/drivers/me/payouts")]
    [Authorize(Roles = Roles.Driver)]
    [ProducesResponseType(typeof(List<Payout>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Payout>>> GetMyPayouts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound("Driver profile not found");

        var query = _context.Payouts
            .Where(p => p.DriverId == driver.Id)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var payouts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(payouts);
    }

    #endregion

    #region Admin Endpoints

    /// <summary>
    /// Process a refund (Admin)
    /// </summary>
    [HttpPost("{id}/refund")]
    [Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin)]
    [ProducesResponseType(typeof(Payment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Payment>> ProcessRefund(Guid id, [FromBody] RefundPaymentDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
            return NotFound();

        if (payment.Status != "completed")
            return BadRequest("Can only refund completed payments");

        if (request.Amount > payment.TotalAmount)
            return BadRequest("Refund amount cannot exceed payment amount");

        var isFullRefund = request.Amount == payment.TotalAmount;

        payment.RefundAmount = (payment.RefundAmount ?? 0) + request.Amount;
        payment.Status = isFullRefund ? "refunded" : "partially_refunded";
        payment.RefundedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.Notes = (payment.Notes ?? "") + $"\n[REFUND] {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {request.Amount:C} - {request.Reason}";

        // TODO: Implement actual Stripe refund
        // payment.StripeRefundId = stripeRefundId;

        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            userId,
            "payment_refunded",
            "Payment",
            payment.Id.ToString(),
            payment.PaymentNumber,
            $"Refund processed: {request.Amount:C} {payment.Currency}. Reason: {request.Reason}"
        );

        return Ok(payment);
    }

    /// <summary>
    /// Get payment statistics (Admin)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin)]
    [ProducesResponseType(typeof(PaymentStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentStatistics>> GetPaymentStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _context.Payments.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= endDate.Value);
        }

        var payments = await query.ToListAsync();

        var stats = new PaymentStatistics
        {
            TotalPayments = payments.Count,
            TotalRevenue = payments.Where(p => p.Status == "completed").Sum(p => p.TotalAmount),
            TotalPlatformFees = payments.Where(p => p.Status == "completed").Sum(p => p.PlatformFee),
            TotalDriverEarnings = payments.Where(p => p.Status == "completed").Sum(p => p.DriverEarnings),
            TotalTips = payments.Where(p => p.Status == "completed").Sum(p => p.TipAmount ?? 0),
            TotalRefunds = payments.Sum(p => p.RefundAmount ?? 0),
            PendingPayments = payments.Count(p => p.Status == "pending"),
            CompletedPayments = payments.Count(p => p.Status == "completed"),
            FailedPayments = payments.Count(p => p.Status == "failed"),
            RefundedPayments = payments.Count(p => p.Status == "refunded" || p.Status == "partially_refunded"),
            AveragePaymentAmount = payments.Where(p => p.Status == "completed").Any()
                ? payments.Where(p => p.Status == "completed").Average(p => p.TotalAmount)
                : 0,
            PaymentsByMethod = payments
                .GroupBy(p => p.PaymentMethod)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return Ok(stats);
    }

    /// <summary>
    /// Stripe webhook handler (placeholder for Stripe integration)
    /// </summary>
    [HttpPost("webhooks/stripe")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StripeWebhook()
    {
        // TODO: Implement Stripe webhook handling
        // 1. Verify webhook signature
        // 2. Parse event data
        // 3. Update payment status based on event type
        // 4. Send notifications

        _logger.LogInformation("Stripe webhook received");

        return Ok();
    }

    /// <summary>
    /// Create a payout for a driver (Admin)
    /// </summary>
    [HttpPost("payouts")]
    [Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin)]
    [ProducesResponseType(typeof(Payout), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Payout>> CreatePayout([FromBody] CreatePayoutDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var driver = await _context.Drivers.FindAsync(request.DriverId);
        if (driver == null)
            return NotFound("Driver not found");

        // Get unpaid earnings for the driver
        var unpaidPayments = await _context.Payments
            .Where(p => p.DriverId == request.DriverId &&
                       p.Status == "completed" &&
                       p.PayoutProcessedAt == null &&
                       p.PaidAt >= request.PeriodStart &&
                       p.PaidAt <= request.PeriodEnd)
            .ToListAsync();

        if (unpaidPayments.Count == 0)
            return BadRequest("No unpaid payments found for the specified period");

        var totalAmount = unpaidPayments.Sum(p => p.DriverEarnings);
        var payoutNumber = $"PAYOUT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var payout = new Payout
        {
            PayoutNumber = payoutNumber,
            DriverId = request.DriverId,
            DriverName = $"{driver.FirstName} {driver.LastName}",
            Amount = totalAmount,
            Currency = request.Currency ?? "GBP",
            Status = "pending",
            PaymentMethod = request.PaymentMethod,
            BankAccountDetails = request.BankAccountDetails,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            PaymentIds = JsonSerializer.Serialize(unpaidPayments.Select(p => p.Id).ToList()),
            TotalJobs = unpaidPayments.Count,
            Notes = request.Notes
        };

        _context.Payouts.Add(payout);

        // Mark payments as processed
        foreach (var payment in unpaidPayments)
        {
            payment.PayoutProcessedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            userId,
            "payout_created",
            "Payout",
            payout.Id.ToString(),
            payoutNumber,
            $"Payout created for {driver.FirstName} {driver.LastName}: {totalAmount:C} {payout.Currency} ({unpaidPayments.Count} jobs)"
        );

        return CreatedAtAction(nameof(GetPayout), new { id = payout.Id }, payout);
    }

    /// <summary>
    /// Get payout by ID (Admin)
    /// </summary>
    [HttpGet("payouts/{id}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin + "," + Roles.Driver)]
    [ProducesResponseType(typeof(Payout), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Payout>> GetPayout(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var payout = await _context.Payouts
            .Include(p => p.Driver)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payout == null)
            return NotFound();

        // Check permissions
        var user = await _userManager.FindByIdAsync(userId);
        var userRoles = await _userManager.GetRolesAsync(user!);
        var isAdmin = userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.SuperAdmin);
        var isDriver = payout.Driver.UserId == userId;

        if (!isAdmin && !isDriver)
            return Forbid("You can only view your own payouts");

        return Ok(payout);
    }

    #endregion
}

#region DTOs

public class CreatePaymentDto
{
    public string? CustomerId { get; set; } // Optional - defaults to current user
    public Guid? DriverId { get; set; }
    public Guid? JobId { get; set; }
    public required decimal Amount { get; set; }
    public decimal? TipAmount { get; set; }
    public string? Currency { get; set; }
    public required string PaymentMethod { get; set; }
    public string? PaymentMethodDetails { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public object? Metadata { get; set; }
}

public class RefundPaymentDto
{
    public required decimal Amount { get; set; }
    public required string Reason { get; set; }
}

public class CreatePayoutDto
{
    public required Guid DriverId { get; set; }
    public required DateTime PeriodStart { get; set; }
    public required DateTime PeriodEnd { get; set; }
    public required string PaymentMethod { get; set; }
    public string? BankAccountDetails { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
}

public class EarningsDto
{
    public decimal TotalEarnings { get; set; }
    public decimal TotalTips { get; set; }
    public int TotalJobs { get; set; }
    public decimal AverageEarningsPerJob { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

public class PaymentStatistics
{
    public int TotalPayments { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalPlatformFees { get; set; }
    public decimal TotalDriverEarnings { get; set; }
    public decimal TotalTips { get; set; }
    public decimal TotalRefunds { get; set; }
    public int PendingPayments { get; set; }
    public int CompletedPayments { get; set; }
    public int FailedPayments { get; set; }
    public int RefundedPayments { get; set; }
    public decimal AveragePaymentAmount { get; set; }
    public Dictionary<string, int> PaymentsByMethod { get; set; } = new();
}

#endregion
