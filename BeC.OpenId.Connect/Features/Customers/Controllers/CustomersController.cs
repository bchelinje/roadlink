using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.Reviews.Models;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Authorization;

namespace BeC.OpenId.Connect.Features.Customers.Controllers;

/// <summary>
/// Customer-specific endpoints for job management and profile
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<CustomersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    #region Job Management

    /// <summary>
    /// Create a new job request
    /// </summary>
    [HttpPost("me/jobs")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(Job), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Job>> CreateJob([FromBody] CreateJobRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Generate unique job number
        var jobNumber = $"JOB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var job = new Job
        {
            JobNumber = jobNumber,
            CustomerId = userId,
            CustomerName = user.DisplayName ?? user.UserName ?? user.Email ?? "Unknown",
            CustomerPhone = user.PhoneNumber ?? request.CustomerPhone,
            CustomerEmail = user.Email ?? "unknown@example.com",
            JobType = request.JobType,
            VehicleTypeRequired = request.VehicleTypeRequired,
            Priority = request.Priority ?? "normal",
            ScheduledDate = request.ScheduledDate,
            ScheduledTime = request.ScheduledTime,
            EstimatedDuration = request.EstimatedDuration ?? 120,
            PickupLocation = JsonSerializer.Serialize(request.PickupLocation),
            DeliveryLocation = JsonSerializer.Serialize(request.DeliveryLocation),
            Distance = request.Distance,
            Items = JsonSerializer.Serialize(request.Items),
            SpecialInstructions = request.SpecialInstructions,
            CustomerNotes = request.CustomerNotes,
            StatusHistory = JsonSerializer.Serialize(new[]
            {
                new
                {
                    Status = "pending",
                    Timestamp = DateTime.UtcNow,
                    ChangedBy = userId,
                    Note = "Job created by customer"
                }
            })
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            userId,
            "job_created",
            "Job",
            job.Id.ToString(),
            jobNumber,
            $"Customer created job {jobNumber}"
        );

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }

    /// <summary>
    /// Get my job history
    /// </summary>
    [HttpGet("me/jobs")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(List<Job>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Job>>> GetMyJobs(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

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

        return Ok(jobs);
    }

    /// <summary>
    /// Get specific job details
    /// </summary>
    [HttpGet("me/jobs/{id}")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(Job), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Job>> GetJob(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == id && j.CustomerId == userId);

        if (job == null)
            return NotFound();

        return Ok(job);
    }

    /// <summary>
    /// Cancel a job
    /// </summary>
    [HttpPatch("me/jobs/{id}/cancel")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(Job), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Job>> CancelJob(Guid id, [FromBody] CancelJobRequest? request = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.CustomerId == userId);
        if (job == null)
            return NotFound();

        // Can only cancel if not completed
        if (job.Status == "completed")
            return BadRequest("Cannot cancel a completed job");

        if (job.Status == "cancelled")
            return BadRequest("Job is already cancelled");

        var oldStatus = job.Status;
        job.Status = "cancelled";
        job.UpdatedAt = DateTime.UtcNow;

        // Update status history
        var statusHistory = JsonSerializer.Deserialize<List<object>>(job.StatusHistory) ?? new List<object>();
        statusHistory.Add(new
        {
            Status = "cancelled",
            Timestamp = DateTime.UtcNow,
            ChangedBy = userId,
            Note = request?.CancellationReason ?? "Cancelled by customer"
        });
        job.StatusHistory = JsonSerializer.Serialize(statusHistory);

        if (!string.IsNullOrWhiteSpace(request?.CancellationReason))
        {
            job.CustomerNotes = (job.CustomerNotes ?? "") + $"\n[CANCELLATION] {request.CancellationReason}";
        }

        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            userId,
            "job_cancelled",
            "Job",
            job.Id.ToString(),
            job.JobNumber,
            $"Customer cancelled job {job.JobNumber} (was {oldStatus})"
        );

        return Ok(job);
    }

    #endregion

    #region Reviews

    /// <summary>
    /// Review a driver after job completion
    /// </summary>
    [HttpPost("me/jobs/{jobId}/review")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(Review), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Review>> ReviewDriver(Guid jobId, [FromBody] CreateReviewRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.CustomerId == userId);

        if (job == null)
            return NotFound("Job not found");

        if (job.Status != "completed")
            return BadRequest("Can only review completed jobs");

        if (job.DriverId == null || job.Driver == null)
            return BadRequest("Job has no assigned driver");

        // Check if already reviewed
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.JobId == jobId);

        if (existingReview != null)
            return BadRequest("You have already reviewed this job");

        var review = new Review
        {
            JobId = jobId,
            DriverId = job.DriverId.Value,
            CustomerEmail = user.Email ?? "unknown@example.com",
            CustomerName = user.DisplayName ?? user.UserName ?? user.Email ?? "Unknown",
            Rating = request.Rating,
            Comment = request.Comment,
            PunctualityRating = request.PunctualityRating,
            ProfessionalismRating = request.ProfessionalismRating,
            CareOfItemsRating = request.CareOfItemsRating,
            CommunicationRating = request.CommunicationRating
        };

        _context.Reviews.Add(review);

        // Update driver rating
        var driverReviews = await _context.Reviews
            .Where(r => r.DriverId == job.DriverId.Value && r.Status == "published")
            .ToListAsync();

        var totalRating = driverReviews.Sum(r => r.Rating) + review.Rating;
        var reviewCount = driverReviews.Count + 1;
        job.Driver.Rating = Math.Round((decimal)totalRating / reviewCount, 2);

        await _context.SaveChangesAsync();

        await _activityLogService.LogAsync(
            userId,
            "review_created",
            "Review",
            review.Id.ToString(),
            $"Review for {job.JobNumber}",
            $"Customer reviewed driver {job.Driver.FirstName} {job.Driver.LastName} ({review.Rating} stars)"
        );

        return CreatedAtAction(nameof(GetMyReviews), new { id = review.Id }, review);
    }

    /// <summary>
    /// Get my reviews given
    /// </summary>
    [HttpGet("me/reviews")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(List<Review>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Review>>> GetMyReviews()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var reviews = await _context.Reviews
            .Include(r => r.Job)
            .Include(r => r.Driver)
            .Where(r => r.CustomerEmail == user.Email)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews);
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get customer statistics
    /// </summary>
    [HttpGet("me/stats")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(CustomerStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerStats>> GetMyStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var jobs = await _context.Jobs
            .Where(j => j.CustomerId == userId)
            .ToListAsync();

        var user = await _userManager.FindByIdAsync(userId);
        var reviewsGiven = user?.Email != null
            ? await _context.Reviews.CountAsync(r => r.CustomerEmail == user.Email)
            : 0;

        var stats = new CustomerStats
        {
            TotalJobs = jobs.Count,
            CompletedJobs = jobs.Count(j => j.Status == "completed"),
            ActiveJobs = jobs.Count(j => j.Status == "assigned" || j.Status == "in_progress" || j.Status == "confirmed"),
            CancelledJobs = jobs.Count(j => j.Status == "cancelled"),
            LastJobDate = jobs.OrderByDescending(j => j.CreatedAt).FirstOrDefault()?.CreatedAt,
            ReviewsGiven = reviewsGiven
        };

        return Ok(stats);
    }

    #endregion

    #region Favorites (Placeholder for future implementation)

    /// <summary>
    /// Get favorite drivers
    /// </summary>
    [HttpGet("me/favorites")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(List<Driver>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Driver>>> GetFavoriteDrivers()
    {
        // TODO: Implement favorites table and logic
        return Ok(new List<Driver>());
    }

    /// <summary>
    /// Add driver to favorites
    /// </summary>
    [HttpPost("me/favorites/{driverId}")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddFavoriteDriver(Guid driverId)
    {
        // TODO: Implement favorites table and logic
        return Ok(new { message = "Favorites feature coming soon" });
    }

    #endregion
}

#region DTOs

public class CreateJobRequest
{
    public required string JobType { get; set; }
    public string? VehicleTypeRequired { get; set; }
    public string? Priority { get; set; }
    public required DateTime ScheduledDate { get; set; }
    public required string ScheduledTime { get; set; }
    public int? EstimatedDuration { get; set; }
    public required LocationDto PickupLocation { get; set; }
    public required LocationDto DeliveryLocation { get; set; }
    public decimal? Distance { get; set; }
    public required List<JobItemDto> Items { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? CustomerNotes { get; set; }
    public required string CustomerPhone { get; set; }
}

public class LocationDto
{
    public required string Address { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Notes { get; set; }
}

public class JobItemDto
{
    public required string Name { get; set; }
    public int Quantity { get; set; }
    public string? Description { get; set; }
    public bool IsFragile { get; set; }
    public bool IsHeavy { get; set; }
}

public class CancelJobRequest
{
    public string? CancellationReason { get; set; }
}

public class CreateReviewRequest
{
    public required int Rating { get; set; }
    public string? Comment { get; set; }
    public int? PunctualityRating { get; set; }
    public int? ProfessionalismRating { get; set; }
    public int? CareOfItemsRating { get; set; }
    public int? CommunicationRating { get; set; }
}

public class CustomerStats
{
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int CancelledJobs { get; set; }
    public DateTime? LastJobDate { get; set; }
    public int ReviewsGiven { get; set; }
}

#endregion
