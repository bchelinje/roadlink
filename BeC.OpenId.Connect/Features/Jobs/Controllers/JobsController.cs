using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Drivers.Controllers;
using BeC.OpenId.Connect.Features.Jobs.Dtos;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Notifications.Services.Interfaces;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Abstractions;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace BeC.OpenId.Connect.Features.Jobs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class JobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    private readonly INotificationService _notificationService;

    public JobsController(ApplicationDbContext context, IActivityLogService activityLogService, INotificationService notificationService)
    {
        _context = context;
        _activityLogService = activityLogService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get all jobs with filtering and pagination
    /// Admins see all jobs, Customers see only their jobs, Drivers see their assigned jobs
    /// </summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin,Customer,Driver")]
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

        // Get current user's role and email
        var userRole = User.FindFirst(Claims.Role)?.Value;
        var userEmail = User.FindFirst(Claims.Email)?.Value;
        var userId = User.FindFirst(Claims.Subject)?.Value;

        // Filter based on user role
        if (userRole == "Customer" && !string.IsNullOrEmpty(userEmail))
        {
            // Customers only see their own jobs
            query = query.Where(j => j.CustomerEmail == userEmail);
        }
        else if (userRole == "Driver" && !string.IsNullOrEmpty(userId))
        {
            // Drivers only see jobs assigned to them
            if (Guid.TryParse(userId, out var driverGuid))
            {
                query = query.Where(j => j.DriverId == driverGuid);
            }
        }
        // Admins and SuperAdmins see all jobs (no filter needed)

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
    /// Get job by ID
    /// Admins see any job, Customers see only their jobs, Drivers see their assigned jobs
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin,Customer,Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> GetJob(Guid id)
    {
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound("Job not found");

        // Get current user's role and email
        var userRole = User.FindFirst(Claims.Role)?.Value;
        var userEmail = User.FindFirst(Claims.Email)?.Value;
        var userId = User.FindFirst(Claims.Subject)?.Value;

        // Validate access based on role
        if (userRole == "Customer")
        {
            // Customers can only view their own jobs
            if (job.CustomerEmail != userEmail)
                return Forbid("You can only view your own jobs");
        }
        else if (userRole == "Driver")
        {
            // Drivers can only view jobs assigned to them
            if (Guid.TryParse(userId, out var driverGuid))
            {
                if (job.DriverId != driverGuid)
                    return Forbid("You can only view jobs assigned to you");
            }
            else
            {
                return Forbid("Invalid driver ID");
            }
        }
        // Admins and SuperAdmins can view any job

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Create new job
    /// Public endpoint - allows anyone to create a job request
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
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

        // Send notification to driver
        if (!string.IsNullOrEmpty(driver.UserId))
        {
            _ = _notificationService.SendJobNotificationAsync(
                driver.UserId,
                job.Id,
                job.JobNumber,
                "job_assigned",
                $"You have been assigned to job {job.JobNumber}. Pickup: {job.PickupLocation}, Delivery: {job.DeliveryLocation}",
                new Dictionary<string, object>
                {
                    { "scheduledDate", job.ScheduledDate },
                    { "scheduledTime", job.ScheduledTime },
                    { "pickupLocation", job.PickupLocation },
                    { "deliveryLocation", job.DeliveryLocation }
                }
            );
        }

        // Send notification to customer
        if (!string.IsNullOrEmpty(job.CustomerId))
        {
            _ = _notificationService.SendJobNotificationAsync(
                job.CustomerId,
                job.Id,
                job.JobNumber,
                "job_updated",
                $"Your job {job.JobNumber} has been assigned to driver {driver.FirstName} {driver.LastName}",
                new Dictionary<string, object>
                {
                    { "driverName", $"{driver.FirstName} {driver.LastName}" },
                    { "driverPhone", driver.PhoneNumber ?? "" }
                }
            );
        }

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

    /// <summary>
    /// Update job status (Driver can update their assigned jobs, Admin can update any job)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin,Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobDto>> UpdateJobStatus(Guid id, UpdateJobStatusDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirst(Claims.Role)?.Value;

        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound("Job not found");

        // Authorization check - drivers can only update their own assigned jobs
        if (userRole == "Driver")
        {
            if (job.Driver == null || job.Driver.UserId != userId)
                return Forbid("You can only update jobs assigned to you");
        }

        // Validate status transition
        var validStatuses = new[] { "pending", "assigned", "confirmed", "in_progress", "completed", "cancelled", "on_hold" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest($"Invalid status. Valid statuses: {string.Join(", ", validStatuses)}");

        var oldStatus = job.Status;
        job.Status = dto.Status;
        job.UpdatedAt = DateTime.UtcNow;

        // Update timestamps based on status
        if (dto.Status == "in_progress" && dto.ActualStartTime.HasValue)
        {
            job.ActualStartTime = dto.ActualStartTime.Value;
        }
        else if (dto.Status == "in_progress" && !job.ActualStartTime.HasValue)
        {
            job.ActualStartTime = DateTime.UtcNow;
        }

        if (dto.Status == "completed")
        {
            if (dto.ActualEndTime.HasValue)
            {
                job.ActualEndTime = dto.ActualEndTime.Value;
            }
            else
            {
                job.ActualEndTime = DateTime.UtcNow;
            }
            job.CompletedAt = DateTime.UtcNow;

            // Update driver stats
            if (job.DriverId.HasValue)
            {
                var driver = await _context.Drivers.FindAsync(job.DriverId.Value);
                if (driver != null)
                {
                    driver.CompletedJobs += 1;
                    driver.ActiveJobs -= 1;
                    if (driver.ActiveJobs < 0) driver.ActiveJobs = 0;
                    driver.Status = driver.ActiveJobs > 0 ? "on_job" : "available";
                }
            }
        }

        // Add to status history
        AddStatusHistory(job, dto.Status, userId!, dto.Notes ?? $"Status changed from {oldStatus} to {dto.Status}");

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.status_updated",
            entityType: "Job",
            entityId: id.ToString(),
            entityName: job.JobNumber,
            description: $"Job {job.JobNumber} status changed from {oldStatus} to {dto.Status}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "OldStatus", oldStatus },
                { "NewStatus", dto.Status },
                { "UpdatedBy", userRole ?? "Unknown" }
            }
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Get available jobs for drivers to claim
    /// </summary>
    [HttpGet("available")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Driver")]
    [ProducesResponseType(typeof(PaginatedResult<JobDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<JobDto>>> GetAvailableJobs(
        [FromQuery] string? vehicleType,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        // Get jobs that are pending or unassigned
        var query = _context.Jobs
            .Where(j => j.Status == "pending" && j.DriverId == null)
            .AsQueryable();

        // Filter by vehicle type if specified
        if (!string.IsNullOrEmpty(vehicleType))
        {
            query = query.Where(j => j.VehicleTypeRequired == vehicleType);
        }

        var total = await query.CountAsync();
        var jobs = await query
            .OrderBy(j => j.ScheduledDate)
            .ThenBy(j => j.Priority == "urgent" ? 0 : j.Priority == "high" ? 1 : j.Priority == "normal" ? 2 : 3)
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
    /// Upload proof of delivery or job photos
    /// </summary>
    [HttpPost("{id}/photos")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Driver,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<JobDto>> UploadJobPhotos(Guid id, [FromBody] List<JobPhotoDto> photos)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirst(Claims.Role)?.Value;

        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound("Job not found");

        // Authorization check
        if (userRole == "Driver")
        {
            if (job.Driver == null || job.Driver.UserId != userId)
                return Forbid("You can only upload photos for jobs assigned to you");
        }

        // Deserialize existing photos or create new list
        var existingPhotos = string.IsNullOrEmpty(job.Photos)
            ? new List<JobPhotoDto>()
            : System.Text.Json.JsonSerializer.Deserialize<List<JobPhotoDto>>(job.Photos) ?? new List<JobPhotoDto>();

        // Add new photos
        existingPhotos.AddRange(photos);

        // Serialize back to JSON
        job.Photos = System.Text.Json.JsonSerializer.Serialize(existingPhotos);
        job.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.photos_uploaded",
            entityType: "Job",
            entityId: id.ToString(),
            entityName: job.JobNumber,
            description: $"{photos.Count} photo(s) uploaded for job {job.JobNumber}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "PhotoCount", photos.Count },
                { "UploadedBy", userRole ?? "Unknown" }
            }
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Reschedule a job to a new date/time
    /// </summary>
    [HttpPost("{id}/reschedule")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin,Customer,Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<JobDto>> RescheduleJob(Guid id, RescheduleJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirst(Claims.Role)?.Value;
        var userEmail = User.FindFirst(Claims.Email)?.Value;

        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound("Job not found");

        // Authorization check
        if (userRole == "Customer" && job.CustomerEmail != userEmail)
            return Forbid("You can only reschedule your own jobs");

        if (userRole == "Driver" && (job.Driver == null || job.Driver.UserId != userId))
            return Forbid("You can only reschedule jobs assigned to you");

        // Validate job status - cannot reschedule completed or cancelled jobs
        if (job.Status == "completed")
            return BadRequest("Cannot reschedule completed jobs");

        if (job.Status == "cancelled")
            return BadRequest("Cannot reschedule cancelled jobs");

        // Store old values for history
        var oldScheduledDate = job.ScheduledDate;
        var oldScheduledTime = job.ScheduledTime;

        // Update job
        job.ScheduledDate = dto.NewScheduledDate;
        job.ScheduledTime = dto.NewScheduledTime;
        job.UpdatedAt = DateTime.UtcNow;

        // Add to status history
        var rescheduleNote = $"Job rescheduled from {oldScheduledDate:yyyy-MM-dd} {oldScheduledTime} to {dto.NewScheduledDate:yyyy-MM-dd} {dto.NewScheduledTime}";
        if (!string.IsNullOrEmpty(dto.Reason))
        {
            rescheduleNote += $". Reason: {dto.Reason}";
        }
        if (!string.IsNullOrEmpty(dto.Notes))
        {
            rescheduleNote += $". Notes: {dto.Notes}";
        }

        AddStatusHistory(job, job.Status, userId!, rescheduleNote);

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.rescheduled",
            entityType: "Job",
            entityId: id.ToString(),
            entityName: job.JobNumber,
            description: rescheduleNote,
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "OldDate", oldScheduledDate },
                { "OldTime", oldScheduledTime },
                { "NewDate", dto.NewScheduledDate },
                { "NewTime", dto.NewScheduledTime },
                { "RescheduledBy", userRole ?? "Unknown" }
            }
        );

        // Send notification to customer (if rescheduled by driver or admin)
        if (userRole != "Customer" && !string.IsNullOrEmpty(job.CustomerId))
        {
            _ = _notificationService.SendJobNotificationAsync(
                job.CustomerId,
                job.Id,
                job.JobNumber,
                "job_rescheduled",
                $"Your job {job.JobNumber} has been rescheduled to {dto.NewScheduledDate:yyyy-MM-dd} at {dto.NewScheduledTime}",
                new Dictionary<string, object>
                {
                    { "oldDate", oldScheduledDate },
                    { "oldTime", oldScheduledTime },
                    { "newDate", dto.NewScheduledDate },
                    { "newTime", dto.NewScheduledTime }
                }
            );
        }

        // Send notification to driver (if rescheduled by customer or admin and driver is assigned)
        if (userRole != "Driver" && job.Driver != null && !string.IsNullOrEmpty(job.Driver.UserId))
        {
            _ = _notificationService.SendJobNotificationAsync(
                job.Driver.UserId,
                job.Id,
                job.JobNumber,
                "job_rescheduled",
                $"Job {job.JobNumber} has been rescheduled to {dto.NewScheduledDate:yyyy-MM-dd} at {dto.NewScheduledTime}",
                new Dictionary<string, object>
                {
                    { "oldDate", oldScheduledDate },
                    { "oldTime", oldScheduledTime },
                    { "newDate", dto.NewScheduledDate },
                    { "newTime", dto.NewScheduledTime }
                }
            );
        }

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Cancel a job with a reason
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
        Roles = "Admin,SuperAdmin,Customer,Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<JobDto>> CancelJob(Guid id, CancelJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirst(Claims.Role)?.Value;
        var userEmail = User.FindFirst(Claims.Email)?.Value;

        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound("Job not found");

        // Authorization check
        if (userRole == "Customer" && job.CustomerEmail != userEmail)
            return Forbid("You can only cancel your own jobs");

        if (userRole == "Driver" && (job.Driver == null || job.Driver.UserId != userId))
            return Forbid("You can only cancel jobs assigned to you");

        // Validate job status
        if (job.Status == "completed")
            return BadRequest("Cannot cancel completed jobs");

        if (job.Status == "cancelled")
            return BadRequest("Job is already cancelled");

        var oldStatus = job.Status;

        // Update job status
        job.Status = "cancelled";
        job.UpdatedAt = DateTime.UtcNow;

        // Update driver stats if job was assigned
        if (job.DriverId.HasValue)
        {
            var driver = await _context.Drivers.FindAsync(job.DriverId.Value);
            if (driver != null)
            {
                driver.ActiveJobs -= 1;
                if (driver.ActiveJobs < 0) driver.ActiveJobs = 0;
                driver.Status = driver.ActiveJobs > 0 ? "on_job" : "available";
            }
        }

        // Add cancellation notes
        var cancellationNote = $"Job cancelled. Reason: {dto.Reason}";
        if (!string.IsNullOrEmpty(dto.CancellationNotes))
        {
            cancellationNote += $". Notes: {dto.CancellationNotes}";
        }

        job.CustomerNotes = string.IsNullOrEmpty(job.CustomerNotes)
            ? cancellationNote
            : $"{job.CustomerNotes}\n{cancellationNote}";

        AddStatusHistory(job, "cancelled", userId!, cancellationNote);

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.cancelled",
            entityType: "Job",
            entityId: id.ToString(),
            entityName: job.JobNumber,
            description: cancellationNote,
            severity: "WARNING",
            metadata: new Dictionary<string, object>
            {
                { "PreviousStatus", oldStatus },
                { "Reason", dto.Reason },
                { "CancelledBy", userRole ?? "Unknown" },
                { "RequestRefund", dto.RequestRefund }
            }
        );

        // Send notification to customer (if cancelled by driver or admin)
        if (userRole != "Customer" && !string.IsNullOrEmpty(job.CustomerId))
        {
            _ = _notificationService.SendJobNotificationAsync(
                job.CustomerId,
                job.Id,
                job.JobNumber,
                "job_cancelled",
                $"Your job {job.JobNumber} has been cancelled. Reason: {dto.Reason}",
                new Dictionary<string, object>
                {
                    { "reason", dto.Reason },
                    { "cancelledBy", userRole ?? "Unknown" }
                }
            );
        }

        // Send notification to driver (if cancelled by customer or admin and driver is assigned)
        if (userRole != "Driver" && job.Driver != null && !string.IsNullOrEmpty(job.Driver.UserId))
        {
            _ = _notificationService.SendJobNotificationAsync(
                job.Driver.UserId,
                job.Id,
                job.JobNumber,
                "job_cancelled",
                $"Job {job.JobNumber} has been cancelled. Reason: {dto.Reason}",
                new Dictionary<string, object>
                {
                    { "reason", dto.Reason },
                    { "cancelledBy", userRole ?? "Unknown" }
                }
            );
        }

        return Ok(MapJobToDto(job));
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
