using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using BeC.Common.Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Reviews.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;
using BeC.Common.Data.Repositories.Interfaces;

namespace BeC.OpenId.Connect.Features.Reviews.Controllers;

/// <summary>
/// Review and rating management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<ReviewsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Create a review (customer reviews driver or driver reviews customer)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Review), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Review>> CreateReview([FromBody] CreateReviewDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        var userRoles = await _userManager.GetRolesAsync(user);
        var reviewerType = userRoles.Contains(Infrastructure.Authorization.Roles.Driver) ? "driver" : "customer";

        // Verify job exists and user is part of it
        if (request.JobId.HasValue)
        {
            var job = await _context.Jobs
                .Include(j => j.Driver)
                .FirstOrDefaultAsync(j => j.Id == request.JobId);

            if (job == null)
                return NotFound("Job not found");

            if (job.Status != "completed")
                return BadRequest("Can only review completed jobs");

            // Verify user is part of this job
            var isCustomer = job.CustomerId == userId;
            var isDriver = job.DriverId != null && job.Driver?.UserId == userId;

            if (!isCustomer && !isDriver)
                return Forbid("You are not part of this job");

            // Check if already reviewed
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.JobId == request.JobId && r.ReviewerId == userId);

            if (existingReview != null)
                return BadRequest("You have already reviewed this job");
        }

        var review = new Review
        {
            ReviewerId = userId,
            ReviewerName = user.DisplayName ?? user.UserName ?? user.Email ?? "Unknown",
            ReviewerType = reviewerType,
            RevieweeId = request.RevieweeId,
            RevieweeName = request.RevieweeName,
            RevieweeType = request.RevieweeType,
            JobId = request.JobId,
            Rating = request.Rating,
            Comment = request.Comment,
            Photos = request.Photos != null ? JsonSerializer.Serialize(request.Photos) : null
        };

        _context.Reviews.Add(review);

        // Update driver rating if reviewing a driver
        if (request.RevieweeType == "driver")
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == request.RevieweeId);
            if (driver != null)
            {
                var driverReviews = await _context.Reviews
                    .Where(r => r.RevieweeId == request.RevieweeId && r.RevieweeType == "driver")
                    .ToListAsync();

                var totalRating = driverReviews.Sum(r => r.Rating) + review.Rating;
                var reviewCount = driverReviews.Count + 1;
                driver.Rating = Math.Round((decimal)totalRating / reviewCount, 2);
            }
        }

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "review_created",
            "Review",
            review.Id.ToString(),
            $"Review for {request.RevieweeName}",
            $"{reviewerType} created review ({review.Rating} stars)"
        );

        return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
    }

    /// <summary>
    /// Get review by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Review), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Review>> GetReview(Guid id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound();

        return Ok(review);
    }

    /// <summary>
    /// Get all reviews for a driver
    /// </summary>
    [HttpGet("drivers/{id}")]
    [ProducesResponseType(typeof(List<Review>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Review>>> GetDriverReviews(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Reviews
            .Where(r => r.RevieweeId == id && r.RevieweeType == "driver" && r.Status == "active")
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(reviews);
    }

    /// <summary>
    /// Get all reviews for a customer (Admin only)
    /// </summary>
    [HttpGet("customers/{id}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(List<Review>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Review>>> GetCustomerReviews(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Reviews
            .Where(r => r.RevieweeId == id && r.RevieweeType == "customer")
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());

        return Ok(reviews);
    }

    /// <summary>
    /// Update a review (only by the review creator)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Review), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Review>> UpdateReview(Guid id, [FromBody] UpdateReviewDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound();

        // Only the creator can update their review
        if (review.ReviewerId != userId)
            return Forbid();

        review.Rating = request.Rating;
        review.Comment = request.Comment;
        review.UpdatedAt = DateTime.UtcNow;

        if (request.Photos != null)
        {
            review.Photos = JsonSerializer.Serialize(request.Photos);
        }

        await _context.SaveChangesAsync();

        // Recalculate driver rating if this is a driver review
        if (review.RevieweeType == "driver")
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == review.RevieweeId);
            if (driver != null)
            {
                var driverReviews = await _context.Reviews
                    .Where(r => r.RevieweeId == review.RevieweeId && r.RevieweeType == "driver" && r.Status == "active")
                    .ToListAsync();

                if (driverReviews.Count > 0)
                {
                    var avgRating = driverReviews.Average(r => r.Rating);
                    driver.Rating = Math.Round((decimal)avgRating, 2);
                    await _context.SaveChangesAsync();
                }
            }
        }

        await _activityLogService.LogActivityAsync(
            userId,
            "review_updated",
            "Review",
            review.Id.ToString(),
            $"Review for {review.RevieweeName}",
            "Review updated"
        );

        return Ok(review);
    }

    /// <summary>
    /// Delete a review (owner or admin)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        var userRoles = await _userManager.GetRolesAsync(user!);
        var isAdmin = userRoles.Contains(Infrastructure.Authorization.Roles.Admin) || userRoles.Contains(Infrastructure.Authorization.Roles.SuperAdmin);

        // Only owner or admin can delete
        if (review.ReviewerId != userId && !isAdmin)
            return Forbid();

        review.Status = "deleted";
        review.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Recalculate driver rating
        if (review.RevieweeType == "driver")
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == review.RevieweeId);
            if (driver != null)
            {
                var activeReviews = await _context.Reviews
                    .Where(r => r.RevieweeId == review.RevieweeId &&
                               r.RevieweeType == "driver" &&
                               r.Status == "active")
                    .ToListAsync();

                driver.Rating = activeReviews.Count > 0
                    ? Math.Round((decimal)activeReviews.Average(r => r.Rating), 2)
                    : 0;

                await _context.SaveChangesAsync();
            }
        }

        await _activityLogService.LogActivityAsync(
            userId,
            "review_deleted",
            "Review",
            review.Id.ToString(),
            $"Review for {review.RevieweeName}",
            "Review deleted"
        );

        return NoContent();
    }

    /// <summary>
    /// Report a review as inappropriate
    /// </summary>
    [HttpPost("{id}/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportReview(Guid id, [FromBody] ReportReviewDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound();

        review.IsFlagged = true;
        review.FlagReason = request.Reason;
        review.FlaggedBy = userId;
        review.FlaggedDate = DateTime.UtcNow;
        review.Status = "reported";
        review.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "review_reported",
            "Review",
            review.Id.ToString(),
            $"Review for {review.RevieweeName}",
            $"Review reported: {request.Reason}"
        );

        return Ok(new { message = "Review has been reported and will be reviewed by moderators" });
    }

    /// <summary>
    /// Respond to a review (reviewee only)
    /// </summary>
    [HttpPost("{id}/response")]
    [ProducesResponseType(typeof(Review), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Review>> RespondToReview(Guid id, [FromBody] RespondToReviewDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound();

        // Only the reviewee (or their associated user) can respond
        var canRespond = review.RevieweeId == userId;

        // If reviewee is a driver, check driver's UserId
        if (review.RevieweeType == "driver")
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
            if (driver != null)
            {
                canRespond = review.RevieweeId == driver.Id.ToString() || review.RevieweeId == userId;
            }
        }

        if (!canRespond)
            return Forbid("You can only respond to reviews about you");

        review.Response = request.Response;
        review.ResponseDate = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "review_response",
            "Review",
            review.Id.ToString(),
            $"Review response",
            "Responded to review"
        );

        return Ok(review);
    }

    /// <summary>
    /// Get reviews pending moderation (Admin only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(List<Review>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Review>>> GetPendingReviews()
    {
        var reviews = await _context.Reviews
            .Where(r => r.Status == "reported" || r.IsFlagged)
            .OrderByDescending(r => r.FlaggedDate)
            .ToListAsync();

        return Ok(reviews);
    }
}

#region DTOs

public class CreateReviewDto
{
    public required string RevieweeId { get; set; }
    public required string RevieweeName { get; set; }
    public required string RevieweeType { get; set; } // "customer" or "driver"
    public Guid? JobId { get; set; }
    public required int Rating { get; set; }
    public required string Comment { get; set; }
    public List<string>? Photos { get; set; }
}

public class UpdateReviewDto
{
    public required int Rating { get; set; }
    public required string Comment { get; set; }
    public List<string>? Photos { get; set; }
}

public class ReportReviewDto
{
    public required string Reason { get; set; }
}

public class RespondToReviewDto
{
    public required string Response { get; set; }
}

#endregion
