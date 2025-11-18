using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using System.Security.Claims;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using OpenIddict.Validation.AspNetCore;

namespace BeC.OpenId.Connect.Features.Jobs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class JobBidsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<JobBidsController> _logger;
    private readonly IActivityLogService _activityLogService;

    public JobBidsController(
        ApplicationDbContext context,
        ILogger<JobBidsController> logger,
        IActivityLogService activityLogService)
    {
        _context = context;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Driver submits a bid for a job
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(JobBidDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobBidDto>> CreateBid([FromBody] CreateJobBidDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        // Get the job
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == dto.JobId);
        if (job == null)
            return NotFound("Job not found");

        // Validate job status
        if (job.Status != "pending")
            return BadRequest("Bids can only be submitted for pending jobs");

        // Check if job already has an assigned driver
        if (job.DriverId != null)
            return BadRequest("This job already has an assigned driver");

        // Check if driver already has a pending bid for this job
        var existingBid = await _context.JobBids
            .FirstOrDefaultAsync(b => b.JobId == dto.JobId && b.DriverId == driver.Id && b.Status == "pending");

        if (existingBid != null)
            return BadRequest("You already have a pending bid for this job");

        // Create the bid
        var bid = new JobBid
        {
            JobId = dto.JobId,
            DriverId = driver.Id,
            BidAmount = dto.BidAmount,
            Message = dto.Message,
            EstimatedDuration = dto.EstimatedDuration,
            ProposedPickupTime = dto.ProposedPickupTime,
            Status = "pending",
            ExpiresAt = dto.ExpiresAt ?? DateTime.UtcNow.AddDays(2), // Default 2 days expiry
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.JobBids.Add(bid);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job_bid.created",
            entityType: "JobBid",
            entityId: bid.Id.ToString(),
            entityName: $"Bid for {job.JobNumber}",
            description: $"Driver {driver.FirstName} {driver.LastName} submitted a bid of ${bid.BidAmount} for job {job.JobNumber}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "JobId", job.Id },
                { "JobNumber", job.JobNumber },
                { "DriverId", driver.Id },
                { "DriverName", $"{driver.FirstName} {driver.LastName}" },
                { "BidAmount", bid.BidAmount },
                { "CustomerId", job.CustomerId }
            }
        );

        // Load navigation properties
        bid.Job = job;
        bid.Driver = driver;

        return CreatedAtAction(nameof(GetBid), new { id = bid.Id }, MapBidToDto(bid));
    }

    /// <summary>
    /// Get a specific bid by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(JobBidDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobBidDto>> GetBid(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var bid = await _context.JobBids
            .Include(b => b.Job)
            .Include(b => b.Driver)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bid == null)
            return NotFound("Bid not found");

        // Authorization: Only the driver who made the bid or the job's customer can view it
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        var isDriver = driver != null && bid.DriverId == driver.Id;
        var isCustomer = bid.Job.CustomerId == userId;

        if (!isDriver && !isCustomer && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return Forbid();

        return Ok(MapBidToDto(bid));
    }

    /// <summary>
    /// Get all bids for a specific job (Customer or Admin only)
    /// </summary>
    [HttpGet("job/{jobId}")]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(List<JobBidDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<JobBidDto>>> GetJobBids(Guid jobId, [FromQuery] string? status = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null)
            return NotFound("Job not found");

        // Authorization: Only the job's customer or admin can view bids
        if (job.CustomerId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var query = _context.JobBids
            .Include(b => b.Driver)
            .Include(b => b.Job)
            .Where(b => b.JobId == jobId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(b => b.Status == status);

        var bids = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bids.Select(MapBidToDto).ToList());
    }

    /// <summary>
    /// Get driver's own bids
    /// </summary>
    [HttpGet("driver/me")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(List<JobBidDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<JobBidDto>>> GetMyBids([FromQuery] string? status = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var query = _context.JobBids
            .Include(b => b.Job)
            .Include(b => b.Driver)
            .Where(b => b.DriverId == driver.Id);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(b => b.Status == status);

        var bids = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bids.Select(MapBidToDto).ToList());
    }

    /// <summary>
    /// Driver withdraws their bid
    /// </summary>
    [HttpPost("{id}/withdraw")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(JobBidDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobBidDto>> WithdrawBid(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var bid = await _context.JobBids
            .Include(b => b.Job)
            .Include(b => b.Driver)
            .FirstOrDefaultAsync(b => b.Id == id && b.DriverId == driver.Id);

        if (bid == null)
            return NotFound("Bid not found");

        if (bid.Status != "pending")
            return BadRequest($"Cannot withdraw bid with status: {bid.Status}");

        bid.Status = "withdrawn";
        bid.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job_bid.withdrawn",
            entityType: "JobBid",
            entityId: bid.Id.ToString(),
            entityName: $"Bid for {bid.Job.JobNumber}",
            description: $"Driver {driver.FirstName} {driver.LastName} withdrew their bid for job {bid.Job.JobNumber}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "JobId", bid.JobId },
                { "JobNumber", bid.Job.JobNumber },
                { "BidAmount", bid.BidAmount }
            }
        );

        return Ok(MapBidToDto(bid));
    }

    /// <summary>
    /// Customer accepts a bid
    /// </summary>
    [HttpPost("{id}/accept")]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobBidDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobBidDto>> AcceptBid(Guid id, [FromBody] RespondToBidDto? dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var bid = await _context.JobBids
            .Include(b => b.Job)
            .Include(b => b.Driver)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bid == null)
            return NotFound("Bid not found");

        // Authorization
        if (bid.Job.CustomerId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return Forbid();

        if (bid.Status != "pending")
            return BadRequest($"Cannot accept bid with status: {bid.Status}");

        if (bid.Job.Status != "pending")
            return BadRequest("Job is no longer available for bidding");

        // Accept the bid
        bid.Status = "accepted";
        bid.ResponseMessage = dto?.Message;
        bid.RespondedAt = DateTime.UtcNow;
        bid.RespondedBy = userId;
        bid.UpdatedAt = DateTime.UtcNow;

        // Assign the job to the driver
        bid.Job.DriverId = bid.DriverId;
        bid.Job.DriverName = $"{bid.Driver.FirstName} {bid.Driver.LastName}";
        bid.Job.Status = "assigned";
        bid.Job.UpdatedAt = DateTime.UtcNow;

        // Update driver stats
        bid.Driver.TotalJobs += 1;
        bid.Driver.ActiveJobs += 1;

        // Reject all other pending bids for this job
        var otherBids = await _context.JobBids
            .Where(b => b.JobId == bid.JobId && b.Id != id && b.Status == "pending")
            .ToListAsync();

        foreach (var otherBid in otherBids)
        {
            otherBid.Status = "rejected";
            otherBid.ResponseMessage = "Another bid was accepted";
            otherBid.RespondedAt = DateTime.UtcNow;
            otherBid.RespondedBy = userId;
            otherBid.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job_bid.accepted",
            entityType: "JobBid",
            entityId: bid.Id.ToString(),
            entityName: $"Bid for {bid.Job.JobNumber}",
            description: $"Customer accepted bid from {bid.Driver.FirstName} {bid.Driver.LastName} for ${bid.BidAmount}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "JobId", bid.JobId },
                { "JobNumber", bid.Job.JobNumber },
                { "DriverId", bid.DriverId },
                { "DriverName", $"{bid.Driver.FirstName} {bid.Driver.LastName}" },
                { "BidAmount", bid.BidAmount },
                { "CustomerId", bid.Job.CustomerId }
            }
        );

        return Ok(MapBidToDto(bid));
    }

    /// <summary>
    /// Customer rejects a bid
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Customer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobBidDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobBidDto>> RejectBid(Guid id, [FromBody] RespondToBidDto? dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var bid = await _context.JobBids
            .Include(b => b.Job)
            .Include(b => b.Driver)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bid == null)
            return NotFound("Bid not found");

        // Authorization
        if (bid.Job.CustomerId != userId && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            return Forbid();

        if (bid.Status != "pending")
            return BadRequest($"Cannot reject bid with status: {bid.Status}");

        bid.Status = "rejected";
        bid.ResponseMessage = dto?.Message;
        bid.RespondedAt = DateTime.UtcNow;
        bid.RespondedBy = userId;
        bid.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job_bid.rejected",
            entityType: "JobBid",
            entityId: bid.Id.ToString(),
            entityName: $"Bid for {bid.Job.JobNumber}",
            description: $"Customer rejected bid from {bid.Driver.FirstName} {bid.Driver.LastName}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "JobId", bid.JobId },
                { "JobNumber", bid.Job.JobNumber },
                { "DriverId", bid.DriverId },
                { "BidAmount", bid.BidAmount }
            }
        );

        return Ok(MapBidToDto(bid));
    }

    private static JobBidDto MapBidToDto(JobBid bid)
    {
        return new JobBidDto
        {
            Id = bid.Id,
            JobId = bid.JobId,
            JobNumber = bid.Job?.JobNumber ?? "",
            DriverId = bid.DriverId,
            DriverName = bid.Driver != null ? $"{bid.Driver.FirstName} {bid.Driver.LastName}" : "",
            DriverRating = bid.Driver?.Rating ?? 0,
            BidAmount = bid.BidAmount,
            Message = bid.Message,
            EstimatedDuration = bid.EstimatedDuration,
            ProposedPickupTime = bid.ProposedPickupTime,
            Status = bid.Status,
            ResponseMessage = bid.ResponseMessage,
            RespondedAt = bid.RespondedAt,
            ExpiresAt = bid.ExpiresAt,
            CreatedAt = bid.CreatedAt,
            UpdatedAt = bid.UpdatedAt
        };
    }
}

#region DTOs

public class CreateJobBidDto
{
    public Guid JobId { get; set; }
    public decimal BidAmount { get; set; }
    public string? Message { get; set; }
    public int? EstimatedDuration { get; set; }
    public DateTime? ProposedPickupTime { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class RespondToBidDto
{
    public string? Message { get; set; }
}

public class JobBidDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobNumber { get; set; } = "";
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = "";
    public decimal DriverRating { get; set; }
    public decimal BidAmount { get; set; }
    public string? Message { get; set; }
    public int? EstimatedDuration { get; set; }
    public DateTime? ProposedPickupTime { get; set; }
    public string Status { get; set; } = "";
    public string? ResponseMessage { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

#endregion
