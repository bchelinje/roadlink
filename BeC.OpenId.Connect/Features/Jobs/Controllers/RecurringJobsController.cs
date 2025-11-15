using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Jobs.Dtos;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Authorization;

namespace BeC.OpenId.Connect.Features.Jobs.Controllers;

/// <summary>
/// Recurring/Scheduled Jobs management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class RecurringJobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<RecurringJobsController> _logger;

    public RecurringJobsController(
        ApplicationDbContext context,
        IActivityLogService activityLogService,
        ILogger<RecurringJobsController> logger)
    {
        _context = context;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get my recurring jobs
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(typeof(List<RecurringJob>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RecurringJob>>> GetMyRecurringJobs()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var recurringJobs = await _context.RecurringJobs
            .Where(r => r.CustomerId == userId && r.Status != "completed" && r.Status != "cancelled")
            .OrderBy(r => r.NextScheduledDate)
            .ToListAsync();

        return Ok(recurringJobs);
    }

    /// <summary>
    /// Get recurring job by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RecurringJob), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringJob>> GetRecurringJob(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var recurringJob = await _context.RecurringJobs.FindAsync(id);
        if (recurringJob == null)
            return NotFound();

        if (recurringJob.CustomerId != userId)
            return Forbid("You can only view your own recurring jobs");

        return Ok(recurringJob);
    }

    /// <summary>
    /// Create a recurring job
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(typeof(RecurringJob), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringJob>> CreateRecurringJob([FromBody] CreateRecurringJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var recurringJob = new RecurringJob
        {
            CustomerId = userId,
            Name = dto.Name,
            Description = dto.Description,
            JobType = dto.JobType,
            VehicleTypeRequired = dto.VehicleTypeRequired,
            Priority = dto.Priority,
            PickupLocation = dto.PickupLocation,
            DeliveryLocation = dto.DeliveryLocation,
            Distance = dto.Distance,
            Items = dto.Items != null ? JsonSerializer.Serialize(dto.Items) : null,
            SpecialInstructions = dto.SpecialInstructions,
            Frequency = dto.Frequency,
            RecurrenceDays = dto.RecurrenceDays != null ? JsonSerializer.Serialize(dto.RecurrenceDays) : null,
            DayOfMonth = dto.DayOfMonth,
            PreferredTime = dto.PreferredTime,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            OccurrenceCount = dto.OccurrenceCount,
            Status = "active",
            NextScheduledDate = CalculateNextDate(dto.Frequency, dto.StartDate, dto.RecurrenceDays, dto.DayOfMonth)
        };

        _context.RecurringJobs.Add(recurringJob);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "recurring_job.created",
            "RecurringJob",
            recurringJob.Id.ToString(),
            recurringJob.Name,
            $"Created recurring job: {recurringJob.Name} ({recurringJob.Frequency})"
        );

        return CreatedAtAction(nameof(GetRecurringJob), new { id = recurringJob.Id }, recurringJob);
    }

    /// <summary>
    /// Update a recurring job
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(typeof(RecurringJob), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringJob>> UpdateRecurringJob(Guid id, [FromBody] UpdateRecurringJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var recurringJob = await _context.RecurringJobs.FindAsync(id);
        if (recurringJob == null)
            return NotFound();

        if (recurringJob.CustomerId != userId)
            return Forbid("You can only update your own recurring jobs");

        // Update fields
        if (!string.IsNullOrEmpty(dto.Name)) recurringJob.Name = dto.Name;
        if (dto.Description != null) recurringJob.Description = dto.Description;
        if (dto.Items != null) recurringJob.Items = JsonSerializer.Serialize(dto.Items);
        if (dto.SpecialInstructions != null) recurringJob.SpecialInstructions = dto.SpecialInstructions;
        if (dto.PreferredTime != null) recurringJob.PreferredTime = dto.PreferredTime;
        if (dto.EndDate.HasValue) recurringJob.EndDate = dto.EndDate;
        if (dto.OccurrenceCount.HasValue) recurringJob.OccurrenceCount = dto.OccurrenceCount;

        recurringJob.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "recurring_job.updated",
            "RecurringJob",
            recurringJob.Id.ToString(),
            recurringJob.Name,
            $"Updated recurring job: {recurringJob.Name}"
        );

        return Ok(recurringJob);
    }

    /// <summary>
    /// Pause/Resume a recurring job
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(typeof(RecurringJob), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringJob>> UpdateStatus(Guid id, [FromBody] UpdateRecurringJobStatusDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var recurringJob = await _context.RecurringJobs.FindAsync(id);
        if (recurringJob == null)
            return NotFound();

        if (recurringJob.CustomerId != userId)
            return Forbid("You can only update your own recurring jobs");

        recurringJob.Status = dto.Status;
        recurringJob.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "recurring_job.status_updated",
            "RecurringJob",
            recurringJob.Id.ToString(),
            recurringJob.Name,
            $"Set recurring job status to: {dto.Status}"
        );

        return Ok(recurringJob);
    }

    /// <summary>
    /// Cancel a recurring job
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelRecurringJob(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var recurringJob = await _context.RecurringJobs.FindAsync(id);
        if (recurringJob == null)
            return NotFound();

        if (recurringJob.CustomerId != userId)
            return Forbid("You can only cancel your own recurring jobs");

        recurringJob.Status = "cancelled";
        recurringJob.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "recurring_job.cancelled",
            "RecurringJob",
            recurringJob.Id.ToString(),
            recurringJob.Name,
            $"Cancelled recurring job: {recurringJob.Name}"
        );

        return NoContent();
    }

    /// <summary>
    /// Generate jobs from active recurring schedules (Admin/Background task)
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Admin + "," + Infrastructure.Authorization.Roles.SuperAdmin)]
    [ProducesResponseType(typeof(GenerateJobsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerateJobsResponse>> GenerateJobs()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var today = DateTime.UtcNow.Date;

        // Get all active recurring jobs that need generation
        var recurringJobs = await _context.RecurringJobs
            .Where(r => r.Status == "active" &&
                       r.NextScheduledDate.HasValue &&
                       r.NextScheduledDate.Value.Date <= today)
            .ToListAsync();

        var jobsCreated = 0;
        var errors = new List<string>();

        foreach (var recurring in recurringJobs)
        {
            try
            {
                // Check if we've reached the occurrence limit
                if (recurring.OccurrenceCount.HasValue && recurring.JobsCreated >= recurring.OccurrenceCount.Value)
                {
                    recurring.Status = "completed";
                    continue;
                }

                // Check if we've reached the end date
                if (recurring.EndDate.HasValue && DateTime.UtcNow > recurring.EndDate.Value)
                {
                    recurring.Status = "completed";
                    continue;
                }

                // Get customer details
                var customer = await _context.Users.FindAsync(recurring.CustomerId);
                if (customer == null)
                {
                    errors.Add($"Customer not found for recurring job {recurring.Name}");
                    continue;
                }

                // Create the job
                var jobNumber = $"JOB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

                var job = new Job
                {
                    JobNumber = jobNumber,
                    CustomerId = recurring.CustomerId,
                    CustomerName = customer.DisplayName ?? customer.UserName ?? "Unknown",
                    CustomerEmail = customer.Email ?? "",
                    CustomerPhone = customer.PhoneNumber ?? "",
                    JobType = recurring.JobType,
                    VehicleTypeRequired = recurring.VehicleTypeRequired,
                    Priority = recurring.Priority ?? "normal",
                    ScheduledDate = recurring.NextScheduledDate!.Value,
                    ScheduledTime = recurring.PreferredTime ?? "09:00",
                    PickupLocation = recurring.PickupLocation,
                    DeliveryLocation = recurring.DeliveryLocation,
                    Distance = recurring.Distance,
                    Items = recurring.Items,
                    SpecialInstructions = recurring.SpecialInstructions,
                    Status = "pending",
                    InternalNotes = $"Auto-generated from recurring job: {recurring.Name}"
                };

                _context.Jobs.Add(job);
                recurring.JobsCreated++;
                recurring.LastGeneratedDate = DateTime.UtcNow;
                recurring.NextScheduledDate = CalculateNextDate(
                    recurring.Frequency,
                    recurring.NextScheduledDate.Value,
                    recurring.RecurrenceDays != null ? JsonSerializer.Deserialize<List<string>>(recurring.RecurrenceDays) : null,
                    recurring.DayOfMonth
                );

                jobsCreated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating job for recurring job {RecurringJobId}", recurring.Id);
                errors.Add($"Error for {recurring.Name}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId!,
            "recurring_jobs.generated",
            "System",
            "bulk",
            "Recurring Jobs",
            $"Generated {jobsCreated} jobs from {recurringJobs.Count} recurring schedules"
        );

        return Ok(new GenerateJobsResponse
        {
            JobsCreated = jobsCreated,
            RecurringJobsProcessed = recurringJobs.Count,
            Errors = errors
        });
    }

    private DateTime? CalculateNextDate(string frequency, DateTime currentDate, List<string>? recurrenceDays, int? dayOfMonth)
    {
        return frequency.ToLower() switch
        {
            "daily" => currentDate.AddDays(1),
            "weekly" => currentDate.AddDays(7),
            "biweekly" => currentDate.AddDays(14),
            "monthly" => currentDate.AddMonths(1),
            _ => currentDate.AddDays(1)
        };
    }
}

#region DTOs

public class CreateRecurringJobDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string JobType { get; set; }
    public string? VehicleTypeRequired { get; set; }
    public string? Priority { get; set; }
    public required string PickupLocation { get; set; }
    public required string DeliveryLocation { get; set; }
    public double? Distance { get; set; }
    public List<object>? Items { get; set; }
    public string? SpecialInstructions { get; set; }
    public required string Frequency { get; set; }
    public List<string>? RecurrenceDays { get; set; }
    public int? DayOfMonth { get; set; }
    public string? PreferredTime { get; set; }
    public required DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? OccurrenceCount { get; set; }
}

public class UpdateRecurringJobDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<object>? Items { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? PreferredTime { get; set; }
    public DateTime? EndDate { get; set; }
    public int? OccurrenceCount { get; set; }
}

public class UpdateRecurringJobStatusDto
{
    public required string Status { get; set; } // active, paused, completed, cancelled
}

public class GenerateJobsResponse
{
    public int JobsCreated { get; set; }
    public int RecurringJobsProcessed { get; set; }
    public List<string> Errors { get; set; } = new();
}

#endregion
