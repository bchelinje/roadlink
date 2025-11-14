using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.ActivityLogs;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Reviews.Dtos;
using BeC.OpenId.Connect.Features.Reviews.Models;
using BeC.OpenId.Connect.Shared.Dtos;
using BeC.OpenId.Connect.Shared.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BeC.OpenId.Connect.Features.Reviews.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReviewsController : ControllerBase, IScoped
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;

    public ReviewsController(
        ApplicationDbContext context,
        IActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Create a new review for a completed job
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> CreateReview(CreateReviewDto dto)
    {
        // Get the job
        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == dto.JobId);

        if (job == null)
        {
            return NotFound("Job not found");
        }

        if (job.DriverId == null)
        {
            return BadRequest("Job has no assigned driver");
        }

        // Check if job is completed
        if (job.Status != "completed")
        {
            return BadRequest("Can only review completed jobs");
        }

        // Check if review already exists for this job
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.JobId == dto.JobId);

        if (existingReview != null)
        {
            return BadRequest("A review has already been submitted for this job");
        }

        // Create the review
        var review = new Review
        {
            JobId = dto.JobId,
            DriverId = job.DriverId.Value,
            CustomerEmail = job.CustomerEmail,
            CustomerName = job.CustomerName,
            Rating = dto.Rating,
            Comment = dto.Comment,
            PunctualityRating = dto.PunctualityRating,
            ProfessionalismRating = dto.ProfessionalismRating,
            CareOfItemsRating = dto.CareOfItemsRating,
            CommunicationRating = dto.CommunicationRating,
            Status = "published"
        };

        _context.Reviews.Add(review);

        // Update driver's average rating
        await UpdateDriverRating(job.DriverId.Value);

        await _context.SaveChangesAsync();

        // Log activity
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _activityLogService.LogActivityAsync(
            action: "review.created",
            entityType: "Review",
            entityId: review.Id.ToString(),
            entityName: job.JobNumber,
            description: $"Review created for job {job.JobNumber} - Rating: {review.Rating}/5",
            userId: userId ?? "system"
        );

        var reviewDto = MapToDto(review, job);
        return CreatedAtAction(nameof(GetReview), new { id = review.Id }, reviewDto);
    }

    /// <summary>
    /// Get a specific review by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> GetReview(Guid id)
    {
        var review = await _context.Reviews
            .Include(r => r.Job)
            .Include(r => r.Driver)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            return NotFound();
        }

        return MapToDto(review, review.Job!);
    }

    /// <summary>
    /// Get all reviews for a specific driver
    /// </summary>
    [HttpGet("driver/{driverId}")]
    [ProducesResponseType(typeof(PaginatedResult<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ReviewDto>>> GetDriverReviews(
        Guid driverId,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var query = _context.Reviews
            .Include(r => r.Job)
            .Include(r => r.Driver)
            .Where(r => r.DriverId == driverId);

        // Filter by status if provided
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        // Order by creation date (newest first)
        query = query.OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        var dtos = reviews.Select(r => MapToDto(r, r.Job!)).ToList();

        return new PaginatedResult<ReviewDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            Limit = limit
        };
    }

    /// <summary>
    /// Get driver rating summary with statistics
    /// </summary>
    [HttpGet("driver/{driverId}/summary")]
    [ProducesResponseType(typeof(DriverRatingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverRatingSummaryDto>> GetDriverRatingSummary(Guid driverId)
    {
        var driver = await _context.Drivers.FindAsync(driverId);
        if (driver == null)
        {
            return NotFound("Driver not found");
        }

        var reviews = await _context.Reviews
            .Include(r => r.Job)
            .Where(r => r.DriverId == driverId && r.Status == "published")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var summary = new DriverRatingSummaryDto
        {
            DriverId = driverId,
            DriverName = $"{driver.FirstName} {driver.LastName}",
            TotalReviews = reviews.Count,
            AverageRating = reviews.Count > 0 ? (decimal)reviews.Average(r => r.Rating) : 0,

            // Category averages
            AveragePunctualityRating = reviews.Any(r => r.PunctualityRating.HasValue)
                ? (decimal?)reviews.Where(r => r.PunctualityRating.HasValue).Average(r => r.PunctualityRating!.Value)
                : null,
            AverageProfessionalismRating = reviews.Any(r => r.ProfessionalismRating.HasValue)
                ? (decimal?)reviews.Where(r => r.ProfessionalismRating.HasValue).Average(r => r.ProfessionalismRating!.Value)
                : null,
            AverageCareOfItemsRating = reviews.Any(r => r.CareOfItemsRating.HasValue)
                ? (decimal?)reviews.Where(r => r.CareOfItemsRating.HasValue).Average(r => r.CareOfItemsRating!.Value)
                : null,
            AverageCommunicationRating = reviews.Any(r => r.CommunicationRating.HasValue)
                ? (decimal?)reviews.Where(r => r.CommunicationRating.HasValue).Average(r => r.CommunicationRating!.Value)
                : null,

            // Rating distribution
            FiveStarCount = reviews.Count(r => r.Rating == 5),
            FourStarCount = reviews.Count(r => r.Rating == 4),
            ThreeStarCount = reviews.Count(r => r.Rating == 3),
            TwoStarCount = reviews.Count(r => r.Rating == 2),
            OneStarCount = reviews.Count(r => r.Rating == 1),

            // Recent reviews (top 10)
            RecentReviews = reviews.Take(10).Select(r => MapToDto(r, r.Job!)).ToList()
        };

        return summary;
    }

    /// <summary>
    /// Add driver response to a review (drivers only)
    /// </summary>
    [Authorize(Roles = "Driver")]
    [HttpPost("{reviewId}/response")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> AddDriverResponse(Guid reviewId, AddDriverResponseDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
        {
            return NotFound("Driver profile not found");
        }

        var review = await _context.Reviews
            .Include(r => r.Job)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
        {
            return NotFound("Review not found");
        }

        // Ensure the driver is responding to their own review
        if (review.DriverId != driver.Id)
        {
            return Forbid();
        }

        review.DriverResponse = dto.Response;
        review.ResponseDate = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogActivityAsync(
            action: "review.response_added",
            entityType: "Review",
            entityId: review.Id.ToString(),
            entityName: review.Job!.JobNumber,
            description: $"Driver responded to review for job {review.Job!.JobNumber}",
            userId: userId
        );

        return MapToDto(review, review.Job!);
    }

    // Helper method to update driver rating
    private async Task UpdateDriverRating(Guid driverId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.DriverId == driverId && r.Status == "published")
            .ToListAsync();

        if (reviews.Count > 0)
        {
            var averageRating = reviews.Average(r => r.Rating);
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver != null)
            {
                driver.Rating = (decimal)averageRating;
                driver.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    // Helper method to map Review to ReviewDto
    private ReviewDto MapToDto(Review review, Job job)
    {
        return new ReviewDto
        {
            Id = review.Id,
            JobId = review.JobId,
            JobNumber = job.JobNumber,
            DriverId = review.DriverId,
            DriverName = review.Driver != null ? $"{review.Driver.FirstName} {review.Driver.LastName}" : "",
            CustomerEmail = review.CustomerEmail,
            CustomerName = review.CustomerName,
            Rating = review.Rating,
            Comment = review.Comment,
            PunctualityRating = review.PunctualityRating,
            ProfessionalismRating = review.ProfessionalismRating,
            CareOfItemsRating = review.CareOfItemsRating,
            CommunicationRating = review.CommunicationRating,
            Status = review.Status,
            DriverResponse = review.DriverResponse,
            ResponseDate = review.ResponseDate,
            HelpfulVotes = review.HelpfulVotes,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}
