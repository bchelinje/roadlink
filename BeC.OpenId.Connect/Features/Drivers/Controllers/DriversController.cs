

// Controllers/DriversController.cs
// Complete C# backend for Driver Portal

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeC.OpenId.Connect.Dto;
using System.Security.Claims;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;

namespace BeC.OpenId.Connect.Features.Drivers.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class DriversController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DriversController> _logger;
    private readonly IActivityLogService _activityLogService;

    public DriversController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<DriversController> logger,
        IActivityLogService activityLogService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    #region Driver Profile Management

    /// <summary>
    /// Get current logged-in driver's profile
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverDto>> GetCurrentDriver()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token");

        var driver = await _context.Drivers
            .Include(d => d.Vehicles)
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        await _activityLogService.LogActivityAsync(
            action: "driver.profile_viewed",
            entityType: "Driver",
            entityId: driver.Id.ToString(),
            entityName: $"{driver.FirstName} {driver.LastName}",
            description: "Driver viewed own profile",
            severity: "INFO"
        );

        return Ok(MapToDto(driver));
    }

    /// <summary>
    /// Get driver by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DriverDto>> GetDriver(Guid id)
    {
        var driver = await _context.Drivers
            .Include(d => d.Vehicles)
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (driver == null)
            return NotFound();

        return Ok(MapToDto(driver));
    }

    /// <summary>
    /// Update driver profile
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Driver,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DriverDto>> UpdateDriver(Guid id, [FromBody] UpdateDriverDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FindAsync(id);

        if (driver == null)
            return NotFound();

        // Drivers can only update their own profile, unless admin
        if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin") && driver.UserId != userId)
            return Forbid();

        // Update fields
        driver.Phone = dto.Phone ?? driver.Phone;
        driver.LicenseNumber = dto.LicenseNumber ?? driver.LicenseNumber;
        driver.LicenseExpiry = dto.LicenseExpiry ?? driver.LicenseExpiry;
        driver.VehicleType = dto.VehicleType ?? driver.VehicleType;
        driver.VehicleRegistration = dto.VehicleRegistration ?? driver.VehicleRegistration;
        
        if (dto.Address != null)
            driver.Address = dto.Address;
        
        if (dto.EmergencyContact != null)
            driver.EmergencyContact = dto.EmergencyContact;

        driver.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "driver.profile_updated",
            entityType: "Driver",
            entityId: id.ToString(),
            entityName: $"{driver.FirstName} {driver.LastName}",
            description: "Updated driver profile",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "UpdatedFields", new[] { 
                    dto.Phone != null ? "Phone" : null,
                    dto.LicenseNumber != null ? "LicenseNumber" : null,
                    dto.VehicleType != null ? "VehicleType" : null
                }.Where(f => f != null).ToArray() }
            }
        );

        return Ok(MapToDto(driver));
    }

    /// <summary>
    /// Upload driver profile image
    /// </summary>
    [HttpPost("{id}/profile-image")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> UploadProfileImage(Guid id, IFormFile image)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FindAsync(id);

        if (driver == null)
            return NotFound();

        if (driver.UserId != userId)
            return Forbid();

        // Validate file
        if (image.Length == 0)
            return BadRequest("Empty file");

        if (image.Length > 5 * 1024 * 1024) // 5MB limit
            return BadRequest("File too large (max 5MB)");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
        if (!allowedTypes.Contains(image.ContentType.ToLower()))
            return BadRequest("Invalid file type. Only JPEG and PNG allowed");

        // Save file
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "drivers");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        var imageUrl = $"/uploads/drivers/{uniqueFileName}";
        driver.ProfileImage = imageUrl;
        driver.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "driver.profile_image_uploaded",
            entityType: "Driver",
            entityId: id.ToString(),
            entityName: $"{driver.FirstName} {driver.LastName}",
            description: "Uploaded profile image",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "ImageUrl", imageUrl },
                { "FileSize", image.Length },
                { "ContentType", image.ContentType }
            }
        );

        return Ok(imageUrl);
    }

    /// <summary>
    /// Update driver status
    /// </summary>
    [HttpPatch("me/status")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DriverDto>> UpdateStatus([FromBody] UpdateStatusDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        driver.Status = dto.Status;
        driver.LastActiveDate = DateTime.UtcNow;
        driver.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "driver.status_updated",
            entityType: "Driver",
            entityId: driver.Id.ToString(),
            entityName: $"{driver.FirstName} {driver.LastName}",
            description: $"Driver status changed to {dto.Status}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "NewStatus", dto.Status },
                { "PreviousStatus", driver.Status }
            }
        );

        return Ok(MapToDto(driver));
    }

    #endregion

    #region Driver Jobs

    /// <summary>
    /// Get driver's jobs with filtering
    /// </summary>
    [HttpGet("me/jobs")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(PaginatedResult<JobDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<JobDto>>> GetMyJobs(
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var query = _context.Jobs.Where(j => j.DriverId == driver.Id);

        // Apply filters
        if (!string.IsNullOrEmpty(status))
            query = query.Where(j => j.Status == status);

        if (startDate.HasValue)
            query = query.Where(j => j.ScheduledDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(j => j.ScheduledDate <= endDate.Value);

        var total = await query.CountAsync();
        var jobs = await query
            .OrderByDescending(j => j.ScheduledDate)
            .ThenByDescending(j => j.ScheduledTime)
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
    /// Get specific job details
    /// </summary>
    [HttpGet("me/jobs/{jobId}")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> GetJob(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.DriverId == driver.Id);

        if (job == null)
            return NotFound("Job not found or not assigned to you");

        await _activityLogService.LogActivityAsync(
            action: "job.viewed",
            entityType: "Job",
            entityId: jobId.ToString(),
            entityName: job.JobNumber,
            description: $"Driver viewed job {job.JobNumber}",
            severity: "INFO"
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Accept a job assignment
    /// </summary>
    [HttpPost("me/jobs/{jobId}/accept")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> AcceptJob(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.DriverId == driver.Id);

        if (job == null)
            return NotFound("Job not found or not assigned to you");

        if (job.Status != "assigned")
            return BadRequest($"Job cannot be accepted in current status: {job.Status}");

        job.Status = "accepted";
        job.UpdatedAt = DateTime.UtcNow;

        // Add to status history
        AddStatusHistory(job, "accepted", userId!, "Driver accepted job");

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.accepted",
            entityType: "Job",
            entityId: jobId.ToString(),
            entityName: job.JobNumber,
            description: $"Driver accepted job {job.JobNumber}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "CustomerId", job.CustomerId },
                { "CustomerName", job.CustomerName },
                { "ScheduledDate", job.ScheduledDate }
            }
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Start a job
    /// </summary>
    [HttpPost("me/jobs/{jobId}/start")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> StartJob(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.DriverId == driver.Id);

        if (job == null)
            return NotFound();

        if (job.Status != "assigned" && job.Status != "accepted" && job.Status != "confirmed")
            return BadRequest($"Job cannot be started in current status: {job.Status}");

        job.Status = "in_progress";
        job.ActualStartTime = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;

        // Update driver status
        driver.Status = "on_job";
        driver.ActiveJobs += 1;

        AddStatusHistory(job, "in_progress", userId!, "Driver started job");

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.started",
            entityType: "Job",
            entityId: jobId.ToString(),
            entityName: job.JobNumber,
            description: $"Driver started job {job.JobNumber}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "ActualStartTime", job.ActualStartTime! },
                { "ScheduledStartTime", $"{job.ScheduledDate:yyyy-MM-dd} {job.ScheduledTime}" },
                { "Location", job.PickupLocation }
            }
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Complete a job
    /// </summary>
    [HttpPost("me/jobs/{jobId}/complete")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> CompleteJob(Guid jobId, [FromBody] CompleteJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.DriverId == driver.Id);

        if (job == null)
            return NotFound();

        if (job.Status != "in_progress")
            return BadRequest($"Job cannot be completed in current status: {job.Status}");

        job.Status = "completed";
        job.ActualEndTime = DateTime.UtcNow;
        job.CompletedAt = DateTime.UtcNow;
        job.InternalNotes = dto.Notes;
        job.UpdatedAt = DateTime.UtcNow;

        // Update driver stats
        driver.CompletedJobs += 1;
        driver.ActiveJobs = Math.Max(0, driver.ActiveJobs - 1);
        
        if (driver.ActiveJobs == 0)
            driver.Status = "available";

        AddStatusHistory(job, "completed", userId!, dto.Notes ?? "Job completed");

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.completed",
            entityType: "Job",
            entityId: jobId.ToString(),
            entityName: job.JobNumber,
            description: $"Driver completed job {job.JobNumber}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "ActualStartTime", job.ActualStartTime! },
                { "ActualEndTime", job.ActualEndTime! },
                { "Duration", (job.ActualEndTime!.Value - job.ActualStartTime!.Value).TotalMinutes },
                { "EstimatedDuration", job.EstimatedDuration },
                { "Notes", dto.Notes ?? "" }
            }
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Update job status
    /// </summary>
    [HttpPatch("me/jobs/{jobId}/status")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> UpdateJobStatus(
        Guid jobId, 
        [FromBody] UpdateJobStatusDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.DriverId == driver.Id);

        if (job == null)
            return NotFound();

        var oldStatus = job.Status;
        job.Status = dto.Status;
        job.UpdatedAt = DateTime.UtcNow;

        AddStatusHistory(job, dto.Status, userId!, dto.Notes ?? $"Status changed from {oldStatus} to {dto.Status}");

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.status_updated",
            entityType: "Job",
            entityId: jobId.ToString(),
            entityName: job.JobNumber,
            description: $"Changed job {job.JobNumber} status from {oldStatus} to {dto.Status}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "OldStatus", oldStatus },
                { "NewStatus", dto.Status },
                { "Notes", dto.Notes ?? "" }
            }
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Add note to job
    /// </summary>
    [HttpPost("me/jobs/{jobId}/notes")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> AddJobNote(Guid jobId, [FromBody] AddNoteDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.DriverId == driver.Id);

        if (job == null)
            return NotFound();

        job.InternalNotes = string.IsNullOrEmpty(job.InternalNotes)
            ? dto.Note
            : $"{job.InternalNotes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {dto.Note}";

        job.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.note_added",
            entityType: "Job",
            entityId: jobId.ToString(),
            entityName: job.JobNumber,
            description: $"Added note to job {job.JobNumber}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "NotePreview", dto.Note.Length > 100 ? dto.Note.Substring(0, 100) + "..." : dto.Note }
            }
        );

        return Ok(MapJobToDto(job));
    }

  /// <summary>
/// Upload job photo
/// </summary>
[HttpPost("me/jobs/{jobId}/photos")]
[Authorize(Roles = "Driver")]
[Consumes("multipart/form-data")]
[ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
public async Task<ActionResult<JobDto>> UploadJobPhoto(
    Guid jobId,
    [FromForm] UploadJobPhotoRequest request)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

    if (driver == null)
        return NotFound("Driver profile not found");

    var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.DriverId == driver.Id);

    if (job == null)
        return NotFound();

    var photo = request.Photo;

    // Validate file
    if (photo == null || photo.Length == 0)
        return BadRequest("Empty file");

    if (photo.Length > 10 * 1024 * 1024) // 10MB limit
        return BadRequest("File too large (max 10MB)");

    var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
    if (!allowedTypes.Contains(photo.ContentType.ToLower()))
        return BadRequest("Invalid file type");

    // Save file
    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "jobs");
    Directory.CreateDirectory(uploadsFolder);

    var uniqueFileName = $"{jobId}_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await photo.CopyToAsync(stream);
    }

    var photoUrl = $"/uploads/jobs/{uniqueFileName}";

    // Add to job photos
    var photoData = new JobPhotoData
    {
        Id = Guid.NewGuid().ToString(),
        Url = photoUrl,
        Caption = request.Caption,
        UploadedBy = userId!,
        UploadedAt = DateTime.UtcNow,
        Type = request.Type
    };

    var photos = System.Text.Json.JsonSerializer.Deserialize<List<JobPhotoData>>(job.Photos ?? "[]") ?? new List<JobPhotoData>();
    photos.Add(photoData);
    job.Photos = System.Text.Json.JsonSerializer.Serialize(photos);
    job.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    await _activityLogService.LogActivityAsync(
        action: "job.photo_uploaded",
        entityType: "Job",
        entityId: jobId.ToString(),
        entityName: job.JobNumber,
        description: $"Uploaded {request.Type} photo to job {job.JobNumber}",
        severity: "INFO",
        metadata: new Dictionary<string, object>
        {
            { "PhotoType", request.Type },
            { "PhotoUrl", photoUrl },
            { "FileSize", photo.Length },
            { "Caption", request.Caption ?? "" }
        }
    );

    return Ok(MapJobToDto(job));
}


    #endregion

    #region Driver Statistics

    /// <summary>
    /// Get driver statistics
    /// </summary>
    [HttpGet("me/stats")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(DriverStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DriverStatsDto>> GetMyStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

        var stats = new DriverStatsDto
        {
            TodayJobs = await _context.Jobs.CountAsync(j => 
                j.DriverId == driver.Id && 
                j.ScheduledDate.Date == today),
            
            WeekJobs = await _context.Jobs.CountAsync(j => 
                j.DriverId == driver.Id && 
                j.ScheduledDate >= startOfWeek),
            
            Pending = await _context.Jobs.CountAsync(j => 
                j.DriverId == driver.Id && 
                j.Status == "assigned"),
            
            Completed = await _context.Jobs.CountAsync(j => 
                j.DriverId == driver.Id && 
                j.ScheduledDate.Date == today && 
                j.Status == "completed"),
            
            Total = driver.TotalJobs,
            Rating = driver.Rating
        };

        return Ok(stats);
    }

    #endregion

    #region Helper Methods

    private static DriverDto MapToDto(Driver driver)
    {
        return new DriverDto
        {
            Id = driver.Id,
            UserId = driver.UserId,
            FirstName = driver.FirstName,
            LastName = driver.LastName,
            Email = driver.Email,
            Phone = driver.Phone,
            LicenseNumber = driver.LicenseNumber,
            LicenseExpiry = driver.LicenseExpiry,
            VehicleType = driver.VehicleType,
            VehicleRegistration = driver.VehicleRegistration,
            Status = driver.Status,
            Rating = driver.Rating,
            TotalJobs = driver.TotalJobs,
            CompletedJobs = driver.CompletedJobs,
            ActiveJobs = driver.ActiveJobs,
            JoinedDate = driver.JoinedDate,
            LastActiveDate = driver.LastActiveDate,
            ProfileImage = driver.ProfileImage,
            Address = driver.Address,
            EmergencyContact = driver.EmergencyContact
        };
    }

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

    #endregion
}

#region DTOs

public class UpdateDriverDto
{
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public string? VehicleType { get; set; }
    public string? VehicleRegistration { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
}

public class UpdateStatusDto
{
    public required string Status { get; set; }
}

public class CompleteJobDto
{
    public string? Notes { get; set; }
}

public class UpdateJobStatusDto
{
    public required string Status { get; set; }
    public string? Notes { get; set; }
}

public class AddNoteDto
{
    public required string Note { get; set; }
}

public class DriverDto
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string LicenseNumber { get; set; }
    public DateTime LicenseExpiry { get; set; }
    public string? VehicleType { get; set; }
    public string? VehicleRegistration { get; set; }
    public required string Status { get; set; }
    public decimal Rating { get; set; }
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int ActiveJobs { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime? LastActiveDate { get; set; }
    public string? ProfileImage { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
}

public class JobDto
{
    public Guid Id { get; set; }
    public required string JobNumber { get; set; }
    public required string CustomerId { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerPhone { get; set; }
    public required string CustomerEmail { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public required string JobType { get; set; }
    public required string Status { get; set; }
    public required string Priority { get; set; }
    public DateTime ScheduledDate { get; set; }
    public required string ScheduledTime { get; set; }
    public int EstimatedDuration { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public required string PickupLocation { get; set; }
    public required string DeliveryLocation { get; set; }
    public decimal? Distance { get; set; }
    public required string Items { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? InternalNotes { get; set; }
    public string? CustomerNotes { get; set; }
    public string? Photos { get; set; }
    public required string StatusHistory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class DriverStatsDto
{
    public int TodayJobs { get; set; }
    public int WeekJobs { get; set; }
    public int Pending { get; set; }
    public int Completed { get; set; }
    public int Total { get; set; }
    public decimal Rating { get; set; }
}

public class PaginatedResult<T>
{
    public required List<T> Items { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int TotalPages { get; set; }
}

public class JobPhotoData
{
    public required string Id { get; set; }
    public required string Url { get; set; }
    public string? Caption { get; set; }
    public required string UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    public required string Type { get; set; }
}

public class StatusHistoryItem
{
    public required string Status { get; set; }
    public required string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
}

public class UploadJobPhotoRequest
{
    public IFormFile Photo { get; set; }

    public string Type { get; set; }

    public string? Caption { get; set; }
}

#endregion