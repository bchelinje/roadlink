using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Drivers.Controllers;
using BeC.OpenId.Connect.Features.Jobs.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;

namespace BeC.OpenId.Connect.Features.Jobs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class JobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;

    public JobsController(ApplicationDbContext context, IActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Get all jobs with filtering and pagination (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PaginatedResult<JobDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<JobDto>>> GetJobs(
        [FromQuery] string? status,
        [FromQuery] string? jobType,
        [FromQuery] string? vehicleType,
        [FromQuery] Guid? driverId,
        [FromQuery] string? customerId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? priority,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        var query = _context.Jobs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(status))
            query = query.Where(j => j.Status == status);

        if (!string.IsNullOrEmpty(jobType))
            query = query.Where(j => j.JobType == jobType);

        if (!string.IsNullOrEmpty(vehicleType))
            query = query.Where(j => j.VehicleTypeRequired == vehicleType);

        if (driverId.HasValue)
            query = query.Where(j => j.DriverId == driverId);

        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(j => j.CustomerId == customerId);

        if (!string.IsNullOrEmpty(priority))
            query = query.Where(j => j.Priority == priority);

        if (startDate.HasValue)
            query = query.Where(j => j.ScheduledDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(j => j.ScheduledDate <= endDate.Value);

        // Search term (job number, customer name, email)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(j =>
                j.JobNumber.Contains(searchTerm) ||
                j.CustomerName.Contains(searchTerm) ||
                j.CustomerEmail.Contains(searchTerm));
        }

        var total = await query.CountAsync();
        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return Ok(new PaginatedResult<JobDto>
        {
            Items = jobs.Select(MapJobToDto).ToList(),
            Total = total,
            Page = page,
            Limit = limit,
            TotalPages = (int)Math.Ceiling(total / (double)limit)
        });
    }

    /// <summary>
    /// Get job by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> GetJob(Guid id)
    {
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound("Job not found");

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Create new job (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<JobDto>> CreateJob(CreateJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Generate unique job number
        var jobNumber = await GenerateJobNumber();

        var job = new Job
        {
            JobNumber = jobNumber,
            CustomerId = Guid.NewGuid().ToString(), // TODO: Implement proper customer management
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            CustomerEmail = dto.CustomerEmail,
            JobType = dto.JobType,
            VehicleTypeRequired = dto.VehicleTypeRequired,
            Status = "pending",
            Priority = dto.Priority,
            ScheduledDate = dto.ScheduledDate,
            ScheduledTime = dto.ScheduledTime,
            EstimatedDuration = dto.EstimatedDuration,
            PickupLocation = dto.PickupLocation,
            DeliveryLocation = dto.DeliveryLocation,
            Distance = dto.Distance,
            Items = dto.Items,
            SpecialInstructions = dto.SpecialInstructions,
            InternalNotes = dto.InternalNotes,
            StatusHistory = "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // If driver is specified, assign immediately
        if (dto.DriverId.HasValue)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == dto.DriverId);
            if (driver != null)
            {
                job.DriverId = driver.Id;
                job.DriverName = $"{driver.FirstName} {driver.LastName}";
                job.Status = "assigned";

                // Update driver stats
                driver.TotalJobs += 1;
                driver.ActiveJobs += 1;
                driver.LastActiveDate = DateTime.UtcNow;
            }
        }

        AddStatusHistory(job, job.Status, userId!, $"Job created by admin");

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.created",
            entityType: "Job",
            entityId: job.Id.ToString(),
            entityName: job.JobNumber,
            description: $"Job {job.JobNumber} created for {job.CustomerName}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "CustomerName", job.CustomerName },
                { "JobType", job.JobType },
                { "ScheduledDate", job.ScheduledDate },
                { "AssignedToDriver", job.DriverId.HasValue }
            }
        );

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, MapJobToDto(job));
    }

    /// <summary>
    /// Update job (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> UpdateJob(Guid id, UpdateJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound("Job not found");

        var changes = new List<string>();

        // Update only provided fields
        if (dto.CustomerName != null && dto.CustomerName != job.CustomerName)
        {
            changes.Add($"Customer name: {job.CustomerName} → {dto.CustomerName}");
            job.CustomerName = dto.CustomerName;
        }

        if (dto.CustomerPhone != null && dto.CustomerPhone != job.CustomerPhone)
        {
            changes.Add($"Customer phone: {job.CustomerPhone} → {dto.CustomerPhone}");
            job.CustomerPhone = dto.CustomerPhone;
        }

        if (dto.CustomerEmail != null && dto.CustomerEmail != job.CustomerEmail)
        {
            changes.Add($"Customer email: {job.CustomerEmail} → {dto.CustomerEmail}");
            job.CustomerEmail = dto.CustomerEmail;
        }

        if (dto.JobType != null && dto.JobType != job.JobType)
        {
            changes.Add($"Job type: {job.JobType} → {dto.JobType}");
            job.JobType = dto.JobType;
        }

        if (dto.VehicleTypeRequired != null && dto.VehicleTypeRequired != job.VehicleTypeRequired)
        {
            changes.Add($"Vehicle type: {job.VehicleTypeRequired} → {dto.VehicleTypeRequired}");
            job.VehicleTypeRequired = dto.VehicleTypeRequired;
        }

        if (dto.Priority != null && dto.Priority != job.Priority)
        {
            changes.Add($"Priority: {job.Priority} → {dto.Priority}");
            job.Priority = dto.Priority;
        }

        if (dto.Status != null && dto.Status != job.Status)
        {
            changes.Add($"Status: {job.Status} → {dto.Status}");
            AddStatusHistory(job, dto.Status, userId!, $"Status updated by admin");
            job.Status = dto.Status;
        }

        if (dto.ScheduledDate.HasValue && dto.ScheduledDate != job.ScheduledDate)
        {
            changes.Add($"Scheduled date: {job.ScheduledDate:yyyy-MM-dd} → {dto.ScheduledDate:yyyy-MM-dd}");
            job.ScheduledDate = dto.ScheduledDate.Value;
        }

        if (dto.ScheduledTime != null && dto.ScheduledTime != job.ScheduledTime)
        {
            changes.Add($"Scheduled time: {job.ScheduledTime} → {dto.ScheduledTime}");
            job.ScheduledTime = dto.ScheduledTime;
        }

        if (dto.EstimatedDuration.HasValue && dto.EstimatedDuration != job.EstimatedDuration)
        {
            changes.Add($"Duration: {job.EstimatedDuration} → {dto.EstimatedDuration} min");
            job.EstimatedDuration = dto.EstimatedDuration.Value;
        }

        if (dto.PickupLocation != null && dto.PickupLocation != job.PickupLocation)
        {
            changes.Add("Pickup location updated");
            job.PickupLocation = dto.PickupLocation;
        }

        if (dto.DeliveryLocation != null && dto.DeliveryLocation != job.DeliveryLocation)
        {
            changes.Add("Delivery location updated");
            job.DeliveryLocation = dto.DeliveryLocation;
        }

        if (dto.Distance.HasValue && dto.Distance != job.Distance)
        {
            changes.Add($"Distance: {job.Distance} → {dto.Distance} miles");
            job.Distance = dto.Distance;
        }

        if (dto.Items != null && dto.Items != job.Items)
        {
            changes.Add("Items list updated");
            job.Items = dto.Items;
        }

        if (dto.SpecialInstructions != null && dto.SpecialInstructions != job.SpecialInstructions)
        {
            changes.Add("Special instructions updated");
            job.SpecialInstructions = dto.SpecialInstructions;
        }

        if (dto.InternalNotes != null && dto.InternalNotes != job.InternalNotes)
        {
            changes.Add("Internal notes updated");
            job.InternalNotes = dto.InternalNotes;
        }

        if (dto.CustomerNotes != null && dto.CustomerNotes != job.CustomerNotes)
        {
            changes.Add("Customer notes updated");
            job.CustomerNotes = dto.CustomerNotes;
        }

        if (changes.Count == 0)
            return Ok(MapJobToDto(job)); // No changes

        job.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.updated",
            entityType: "Job",
            entityId: id.ToString(),
            entityName: job.JobNumber,
            description: $"Job {job.JobNumber} updated: {string.Join(", ", changes)}",
            severity: "INFO"
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Assign job to driver (Admin only)
    /// </summary>
    [HttpPost("{id}/assign")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> AssignJob(Guid id, AssignJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound("Job not found");

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == dto.DriverId);

        if (driver == null)
            return NotFound("Driver not found");

        // Check if driver is available
        if (driver.Status != "active" && driver.Status != "available")
            return BadRequest($"Driver is not available. Current status: {driver.Status}");

        // Unassign from previous driver if needed
        if (job.DriverId.HasValue && job.DriverId != dto.DriverId)
        {
            var previousDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == job.DriverId);
            if (previousDriver != null)
            {
                previousDriver.TotalJobs -= 1;
                if (previousDriver.TotalJobs < 0) previousDriver.TotalJobs = 0;
                previousDriver.ActiveJobs -= 1;
                if (previousDriver.ActiveJobs < 0) previousDriver.ActiveJobs = 0;
            }
        }

        // Assign to new driver
        job.DriverId = driver.Id;
        job.DriverName = $"{driver.FirstName} {driver.LastName}";
        job.Status = "assigned";
        job.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(dto.InternalNotes))
            job.InternalNotes = dto.InternalNotes;

        // Update driver stats
        if (job.DriverId != dto.DriverId)
        {
            driver.TotalJobs += 1;
            driver.ActiveJobs += 1;
        }
        driver.LastActiveDate = DateTime.UtcNow;

        AddStatusHistory(job, "assigned", userId!,
            $"Job assigned to {driver.FirstName} {driver.LastName} by admin");

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.assigned",
            entityType: "Job",
            entityId: id.ToString(),
            entityName: job.JobNumber,
            description: $"Job {job.JobNumber} assigned to driver {driver.FirstName} {driver.LastName}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "DriverId", driver.Id },
                { "DriverName", $"{driver.FirstName} {driver.LastName}" },
                { "DriverEmail", driver.Email }
            }
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Delete job (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound("Job not found");

        // Don't allow deletion of completed jobs
        if (job.Status == "completed")
            return BadRequest("Cannot delete completed jobs");

        // Don't allow deletion of in-progress jobs
        if (job.Status == "in_progress")
            return BadRequest("Cannot delete jobs that are in progress");

        var jobNumber = job.JobNumber;

        // Update driver stats if job was assigned
        if (job.DriverId.HasValue)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == job.DriverId);
            if (driver != null)
            {
                driver.TotalJobs -= 1;
                if (driver.TotalJobs < 0) driver.TotalJobs = 0;
                driver.ActiveJobs -= 1;
                if (driver.ActiveJobs < 0) driver.ActiveJobs = 0;
            }
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.deleted",
            entityType: "Job",
            entityId: id.ToString(),
            entityName: jobNumber,
            description: $"Job {jobNumber} deleted by admin",
            severity: "WARNING"
        );

        return NoContent();
    }

    #region Helper Methods

    private static JobDto MapJobToDto(Job job)
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
            CustomerNotes = job.CustomerNotes,
            Photos = job.Photos,
            StatusHistory = job.StatusHistory,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            CompletedAt = job.CompletedAt
        };
    }

    private static void AddStatusHistory(Job job, string status, string userId, string notes)
    {
        var history = System.Text.Json.JsonSerializer.Deserialize<List<StatusHistoryItem>>(job.StatusHistory ?? "[]")
            ?? new List<StatusHistoryItem>();

        history.Add(new StatusHistoryItem
        {
            Status = status,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            Notes = notes
        });

        job.StatusHistory = System.Text.Json.JsonSerializer.Serialize(history);
    }

    private async Task<string> GenerateJobNumber()
    {
        var today = DateTime.UtcNow;
        var prefix = $"JOB-{today:yyyyMMdd}";

        // Get the count of jobs created today
        var todayStart = today.Date;
        var todayEnd = todayStart.AddDays(1);

        var count = await _context.Jobs
            .Where(j => j.CreatedAt >= todayStart && j.CreatedAt < todayEnd)
            .CountAsync();

        return $"{prefix}-{(count + 1):D4}";
    }

    #endregion
}

public class StatusHistoryItem
{
    public required string Status { get; set; }
    public required string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
}
