

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
using OpenIddict.Validation.AspNetCore;
using BeC.Common.Data.Repositories.Interfaces;

namespace BeC.OpenId.Connect.Features.Drivers.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)] // Use Bearer token authentication
public class DriversController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DriversController> _logger;
    private readonly IActivityLogService _activityLogService;

    public DriversController(
        ApplicationDbContext context,
        IRepository repository,
        UserManager<ApplicationUser> userManager,
        ILogger<DriversController> logger,
        IActivityLogService activityLogService)
    {
        _context = context;
        _repository = repository;
        _userManager = userManager;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    #region Driver Profile Management

    /// <summary>
    /// Get current logged-in driver's profile
    /// </summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverDto>> GetCurrentDriver()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token");

        // Using Repository: GetEntity with filter and includes
        var driver = await _repository.GetEntity<Driver>(
            predicate: d => d.UserId == userId,
            includeProperties: "Vehicles,Documents"
        );

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DriverDto>> GetDriver(Guid id)
    {
        // Using Repository: GetEntity by ID with includes
        var driver = await _repository.GetEntity<Driver>(
            predicate: d => d.Id == id,
            includeProperties: "Vehicles,Documents"
        );

        if (driver == null)
            return NotFound();

        return Ok(MapToDto(driver));
    }

    /// <summary>
    /// Get all drivers with pagination and filtering (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(DriverDtoPaginatedResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<DriverDtoPaginatedResult>> GetDrivers(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? vehicleType = null,
        [FromQuery] string? location = null,
        [FromQuery] double? minRating = null,
        [FromQuery] bool? availability = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // Build filter predicate
        System.Linq.Expressions.Expression<Func<Driver, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(status))
            predicate = d => d.Status == status;

        if (!string.IsNullOrEmpty(search))
        {
            if (predicate == null)
                predicate = d => (d.FirstName != null && d.FirstName.Contains(search)) ||
                               (d.LastName != null && d.LastName.Contains(search)) ||
                               (d.Email != null && d.Email.Contains(search)) ||
                               (d.Phone != null && d.Phone.Contains(search));
            else
            {
                var searchPredicate = predicate;
                predicate = d => searchPredicate.Compile()(d) &&
                               ((d.FirstName != null && d.FirstName.Contains(search)) ||
                                (d.LastName != null && d.LastName.Contains(search)) ||
                                (d.Email != null && d.Email.Contains(search)) ||
                                (d.Phone != null && d.Phone.Contains(search)));
            }
        }

        if (!string.IsNullOrEmpty(vehicleType))
        {
            if (predicate == null)
                predicate = d => d.VehicleType == vehicleType;
            else
            {
                var vtPredicate = predicate;
                predicate = d => vtPredicate.Compile()(d) && d.VehicleType == vehicleType;
            }
        }

        if (!string.IsNullOrEmpty(location))
        {
            if (predicate == null)
                predicate = d => d.Address != null && d.Address.Contains(location);
            else
            {
                var locPredicate = predicate;
                predicate = d => locPredicate.Compile()(d) && d.Address != null && d.Address.Contains(location);
            }
        }

        if (minRating.HasValue)
        {
            if (predicate == null)
                predicate = d => d.Rating >= (decimal)minRating.Value;
            else
            {
                var ratingPredicate = predicate;
                predicate = d => ratingPredicate.Compile()(d) && d.Rating >= (decimal)minRating.Value;
            }
        }

        // Build query with filters (using DbContext for Include support)
        var query = _context.Drivers.Include(d => d.Vehicles).Where(predicate);

        var totalCount = await query.CountAsync();
        var drivers = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var driverDtos = drivers.Select(MapToDto).ToList();

        return Ok(new DriverDtoPaginatedResult
        {
            Items = driverDtos,
            Total = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Create a new driver profile (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DriverDto>> CreateDriver([FromBody] CreateDriverDto dto)
    {
        // Validate that user exists
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return BadRequest("User not found");

        // Using Repository: Check if driver profile already exists
        var existingDriver = await _repository.Exists<Driver>(d => d.UserId == dto.UserId);
        if (existingDriver)
            return BadRequest("Driver profile already exists for this user");

        // Using Repository: Check if email is already used
        if (await _repository.Exists<Driver>(d => d.Email == dto.Email))
            return BadRequest("Email is already in use by another driver");

        var driver = new Driver
        {
            UserId = dto.UserId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            LicenseNumber = dto.LicenseNumber,
            LicenseExpiry = dto.LicenseExpiry,
            VehicleType = dto.VehicleType,
            VehicleRegistration = dto.VehicleRegistration,
            Status = dto.Status ?? "inactive",
            Address = dto.Address,
            EmergencyContact = dto.EmergencyContact,
            JoinedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Using Repository: InsertEntity
        await _repository.InsertEntity(driver);

        await _activityLogService.LogActivityAsync(
            action: "driver.created",
            entityType: "Driver",
            entityId: driver.Id.ToString(),
            entityName: $"{driver.FirstName} {driver.LastName}",
            description: "New driver profile created",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "UserId", driver.UserId },
                { "Email", driver.Email },
                { "Status", driver.Status }
            }
        );

        return CreatedAtAction(nameof(GetDriver), new { id = driver.Id }, MapToDto(driver));
    }

    /// <summary>
    /// Update driver profile
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DriverDto>> UpdateDriver(Guid id, [FromBody] UpdateDriverDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Using Repository: GetEntity by ID
        var driver = await _repository.GetEntity<Driver>(d => d.Id == id);

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

        // Using Repository: UpdateEntity
        await _repository.UpdateEntity(driver);

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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
    /// Get available jobs from marketplace (unassigned jobs)
    /// </summary>
    [HttpGet("marketplace/jobs")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(PaginatedResult<JobDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<JobDto>>> GetMarketplaceJobs(
        [FromQuery] string? location,
        [FromQuery] string? vehicleType,
        [FromQuery] string? jobType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        // Query for pending (unassigned) jobs
        var query = _context.Jobs.Where(j => j.Status == "pending" && j.DriverId == null);

        // Filter by vehicle type if specified
        if (!string.IsNullOrEmpty(vehicleType))
            query = query.Where(j => j.VehicleTypeRequired == vehicleType || j.VehicleTypeRequired == null);

        // Filter by job type
        if (!string.IsNullOrEmpty(jobType))
            query = query.Where(j => j.JobType == jobType);

        // Filter by location (search in pickup location JSON)
        if (!string.IsNullOrEmpty(location))
            query = query.Where(j => j.PickupLocation.Contains(location) || j.DeliveryLocation.Contains(location));

        // Filter by date range
        if (startDate.HasValue)
            query = query.Where(j => j.ScheduledDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(j => j.ScheduledDate <= endDate.Value);

        var total = await query.CountAsync();
        var jobs = await query
            .OrderBy(j => j.ScheduledDate)
            .ThenBy(j => j.Priority)
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
    /// Claim/accept a job from the marketplace
    /// </summary>
    [HttpPost("marketplace/jobs/{jobId}/claim")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JobDto>> ClaimMarketplaceJob(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        // Check if driver is available (case-insensitive)
        var driverStatus = driver.Status?.ToLower() ?? "";
        if (driverStatus != "active" && driverStatus != "available")
            return BadRequest($"Driver must be active/available to claim jobs. Current status: {driver.Status}");

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            return NotFound("Job not found");

        // Check if job is still available
        if (job.Status != "pending" || job.DriverId != null)
            return BadRequest("Job is no longer available in the marketplace");

        // Assign job to driver
        job.DriverId = driver.Id;
        job.DriverName = $"{driver.FirstName} {driver.LastName}";
        job.Status = "assigned";
        job.UpdatedAt = DateTime.UtcNow;

        // Update driver stats
        driver.TotalJobs += 1;
        driver.ActiveJobs += 1;
        driver.LastActiveDate = DateTime.UtcNow;

        // Add to status history
        AddStatusHistory(job, "assigned", userId!, $"Job claimed by driver {driver.FirstName} {driver.LastName} from marketplace");

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "job.claimed",
            entityType: "Job",
            entityId: jobId.ToString(),
            entityName: job.JobNumber,
            description: $"Driver {driver.FirstName} {driver.LastName} claimed job {job.JobNumber} from marketplace",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                { "DriverId", driver.Id },
                { "DriverName", $"{driver.FirstName} {driver.LastName}" },
                { "CustomerId", job.CustomerId },
                { "CustomerName", job.CustomerName },
                { "ScheduledDate", job.ScheduledDate }
            }
        );

        return Ok(MapJobToDto(job));
    }

    /// <summary>
    /// Accept a job assignment
    /// </summary>
    [HttpPost("me/jobs/{jobId}/accept")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
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

    #region Driver Earnings

    /// <summary>
    /// Get current driver's earnings summary
    /// </summary>
    [HttpGet("me/earnings/summary")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(EarningSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EarningSummaryDto>> GetEarningsSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var earnings = await _context.Earnings
            .Where(e => e.DriverId == driver.Id)
            .ToListAsync();

        var summary = new EarningSummaryDto
        {
            TotalEarnings = earnings.Sum(e => e.NetAmount),
            PendingEarnings = earnings.Where(e => e.PaymentStatus == "pending").Sum(e => e.NetAmount),
            PaidEarnings = earnings.Where(e => e.PaymentStatus == "paid").Sum(e => e.NetAmount),
            TotalJobs = earnings.Count,
            PendingPayments = earnings.Count(e => e.PaymentStatus == "pending"),
            CompletedPayments = earnings.Count(e => e.PaymentStatus == "paid"),
            AverageEarningPerJob = earnings.Any() ? earnings.Average(e => e.NetAmount) : 0,
            TotalBonuses = earnings.Sum(e => e.BonusAmount ?? 0),
            TotalTips = earnings.Sum(e => e.TipAmount ?? 0),
            TotalDeductions = earnings.Sum(e => e.DeductionAmount ?? 0),
            LastPaymentDate = earnings
                .Where(e => e.PaymentDate != null)
                .OrderByDescending(e => e.PaymentDate)
                .FirstOrDefault()?.PaymentDate
        };

        return Ok(summary);
    }

    /// <summary>
    /// Get current driver's earnings list with pagination and filtering
    /// </summary>
    [HttpGet("me/earnings")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(List<EarningDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EarningDto>>> GetMyEarnings(
        [FromQuery] string? paymentStatus = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var query = _context.Earnings
            .Where(e => e.DriverId == driver.Id);

        if (!string.IsNullOrEmpty(paymentStatus))
            query = query.Where(e => e.PaymentStatus == paymentStatus);

        if (startDate.HasValue)
            query = query.Where(e => e.JobCompletedDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.JobCompletedDate <= endDate.Value);

        var earnings = await query
            .OrderByDescending(e => e.JobCompletedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var earningDtos = earnings.Select(e => MapEarningToDto(e)).ToList();

        return Ok(earningDtos);
    }

    /// <summary>
    /// Get earnings for a specific period with daily breakdown
    /// </summary>
    [HttpGet("me/earnings/period")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(PeriodEarningsSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PeriodEarningsSummaryDto>> GetPeriodEarnings(
        [FromQuery] string period = "weekly", // daily, weekly, monthly
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        // Set default date range based on period
        var now = DateTime.UtcNow;
        startDate ??= period switch
        {
            "daily" => now.Date,
            "weekly" => now.AddDays(-7),
            "monthly" => now.AddDays(-30),
            _ => now.AddDays(-7)
        };
        endDate ??= now;

        var earnings = await _context.Earnings
            .Where(e => e.DriverId == driver.Id &&
                       e.JobCompletedDate >= startDate.Value &&
                       e.JobCompletedDate <= endDate.Value)
            .ToListAsync();

        var dailyBreakdown = earnings
            .GroupBy(e => e.JobCompletedDate.Date)
            .Select(g => new DailyEarningDto
            {
                Date = g.Key,
                TotalEarnings = g.Sum(e => e.NetAmount),
                JobsCompleted = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        var summary = new PeriodEarningsSummaryDto
        {
            Period = period,
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            TotalEarnings = earnings.Sum(e => e.NetAmount),
            JobsCompleted = earnings.Count,
            AverageEarningPerJob = earnings.Any() ? earnings.Average(e => e.NetAmount) : 0,
            DailyBreakdown = dailyBreakdown
        };

        return Ok(summary);
    }

    /// <summary>
    /// Get payment status summary
    /// </summary>
    [HttpGet("me/earnings/payment-status")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(PaymentStatusSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentStatusSummaryDto>> GetPaymentStatusSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var earnings = await _context.Earnings
            .Where(e => e.DriverId == driver.Id)
            .ToListAsync();

        var summary = new PaymentStatusSummaryDto
        {
            PendingCount = earnings.Count(e => e.PaymentStatus == "pending"),
            PendingAmount = earnings.Where(e => e.PaymentStatus == "pending").Sum(e => e.NetAmount),
            ProcessingCount = earnings.Count(e => e.PaymentStatus == "processing"),
            ProcessingAmount = earnings.Where(e => e.PaymentStatus == "processing").Sum(e => e.NetAmount),
            PaidCount = earnings.Count(e => e.PaymentStatus == "paid"),
            PaidAmount = earnings.Where(e => e.PaymentStatus == "paid").Sum(e => e.NetAmount),
            FailedCount = earnings.Count(e => e.PaymentStatus == "failed"),
            FailedAmount = earnings.Where(e => e.PaymentStatus == "failed").Sum(e => e.NetAmount)
        };

        return Ok(summary);
    }

    /// <summary>
    /// Get specific earning details
    /// </summary>
    [HttpGet("me/earnings/{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(EarningDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EarningDto>> GetEarningById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var earning = await _context.Earnings
            .FirstOrDefaultAsync(e => e.Id == id && e.DriverId == driver.Id);

        if (earning == null)
            return NotFound();

        return Ok(MapEarningToDto(earning));
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

    private static EarningDto MapEarningToDto(Earning earning)
    {
        return new EarningDto
        {
            Id = earning.Id,
            DriverId = earning.DriverId,
            JobId = earning.JobId,
            JobNumber = earning.JobNumber,
            BaseAmount = earning.BaseAmount,
            BonusAmount = earning.BonusAmount,
            TipAmount = earning.TipAmount,
            DeductionAmount = earning.DeductionAmount,
            NetAmount = earning.NetAmount,
            PaymentStatus = earning.PaymentStatus,
            PaymentMethod = earning.PaymentMethod,
            PaymentReference = earning.PaymentReference,
            PaymentDate = earning.PaymentDate,
            PaymentProcessedDate = earning.PaymentProcessedDate,
            JobCompletedDate = earning.JobCompletedDate,
            JobDistance = earning.JobDistance,
            JobDuration = earning.JobDuration,
            Notes = earning.Notes,
            PaymentDetails = earning.PaymentDetails,
            CreatedAt = earning.CreatedAt,
            UpdatedAt = earning.UpdatedAt
        };
    }

    #endregion

    #region Vehicle Management

    // GET: api/Drivers/me/vehicles
    [HttpGet("me/vehicles")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(List<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VehicleDto>>> GetMyVehicles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var vehicles = await _context.Vehicles
            .Where(v => v.DriverId == driver.Id)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        return Ok(vehicles.Select(MapVehicleToDto).ToList());
    }

    // GET: api/Drivers/me/vehicles/{id}
    [HttpGet("me/vehicles/{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleDto>> GetMyVehicle(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.DriverId == driver.Id);

        if (vehicle == null)
            return NotFound("Vehicle not found");

        return Ok(MapVehicleToDto(vehicle));
    }

    // POST: api/Drivers/me/vehicles
    [HttpPost("me/vehicles")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<VehicleDto>> CreateVehicle([FromBody] CreateVehicleDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        // Check if registration number already exists
        var existingVehicle = await _context.Vehicles
            .AnyAsync(v => v.RegistrationNumber == dto.RegistrationNumber);

        if (existingVehicle)
            return BadRequest("A vehicle with this registration number already exists");

        var vehicle = new Vehicle
        {
            DriverId = driver.Id,
            Type = dto.Type,
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            RegistrationNumber = dto.RegistrationNumber,
            VinNumber = dto.VinNumber,
            CargoCapacity = dto.CargoCapacity ?? 0,
            MaxPayloadWeight = dto.MaxPayloadWeight ?? 0,
            MaxGrossWeight = dto.MaxGrossWeight ?? 0,
            CargoLength = dto.CargoLength,
            CargoWidth = dto.CargoWidth,
            CargoHeight = dto.CargoHeight,
            Features = dto.Features,
            HasInsurance = dto.HasInsurance,
            InsuranceExpiry = dto.InsuranceExpiry,
            Status = dto.Status,
            LastInspectionDate = dto.LastInspectionDate,
            NextInspectionDue = dto.NextInspectionDue,
            Mileage = dto.Mileage,
            Photos = dto.Photos,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyVehicle), new { id = vehicle.Id }, MapVehicleToDto(vehicle));
    }

    // PUT: api/Drivers/me/vehicles/{id}
    [HttpPut("me/vehicles/{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleDto>> UpdateVehicle(Guid id, [FromBody] UpdateVehicleDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.DriverId == driver.Id);

        if (vehicle == null)
            return NotFound("Vehicle not found");

        // Check if registration number is being changed and if it already exists
        if (dto.RegistrationNumber != null && dto.RegistrationNumber != vehicle.RegistrationNumber)
        {
            var existingVehicle = await _context.Vehicles
                .AnyAsync(v => v.RegistrationNumber == dto.RegistrationNumber && v.Id != id);

            if (existingVehicle)
                return BadRequest("A vehicle with this registration number already exists");
        }

        // Update fields that are provided
        if (dto.Type != null) vehicle.Type = dto.Type;
        if (dto.Make != null) vehicle.Make = dto.Make;
        if (dto.Model != null) vehicle.Model = dto.Model;
        if (dto.Year.HasValue) vehicle.Year = dto.Year.Value;
        if (dto.RegistrationNumber != null) vehicle.RegistrationNumber = dto.RegistrationNumber;
        if (dto.VinNumber != null) vehicle.VinNumber = dto.VinNumber;
        if (dto.CargoCapacity.HasValue) vehicle.CargoCapacity = dto.CargoCapacity.Value;
        if (dto.MaxPayloadWeight.HasValue) vehicle.MaxPayloadWeight = dto.MaxPayloadWeight.Value;
        if (dto.MaxGrossWeight.HasValue) vehicle.MaxGrossWeight = dto.MaxGrossWeight.Value;
        if (dto.CargoLength.HasValue) vehicle.CargoLength = dto.CargoLength;
        if (dto.CargoWidth.HasValue) vehicle.CargoWidth = dto.CargoWidth;
        if (dto.CargoHeight.HasValue) vehicle.CargoHeight = dto.CargoHeight;
        if (dto.Features != null) vehicle.Features = dto.Features;
        if (dto.HasInsurance.HasValue) vehicle.HasInsurance = dto.HasInsurance.Value;
        if (dto.InsuranceExpiry.HasValue) vehicle.InsuranceExpiry = dto.InsuranceExpiry;
        if (dto.Status != null) vehicle.Status = dto.Status;
        if (dto.LastInspectionDate.HasValue) vehicle.LastInspectionDate = dto.LastInspectionDate;
        if (dto.NextInspectionDue.HasValue) vehicle.NextInspectionDue = dto.NextInspectionDue;
        if (dto.Mileage.HasValue) vehicle.Mileage = dto.Mileage;
        if (dto.Photos != null) vehicle.Photos = dto.Photos;
        if (dto.IsActive.HasValue) vehicle.IsActive = dto.IsActive.Value;

        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(MapVehicleToDto(vehicle));
    }

    // DELETE: api/Drivers/me/vehicles/{id}
    [HttpDelete("me/vehicles/{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Driver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteVehicle(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver == null)
            return NotFound("Driver profile not found");

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.DriverId == driver.Id);

        if (vehicle == null)
            return NotFound("Vehicle not found");

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static VehicleDto MapVehicleToDto(Vehicle vehicle)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            DriverId = vehicle.DriverId,
            Type = vehicle.Type,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            RegistrationNumber = vehicle.RegistrationNumber,
            VinNumber = vehicle.VinNumber,
            CargoCapacity = vehicle.CargoCapacity,
            MaxPayloadWeight = vehicle.MaxPayloadWeight,
            MaxGrossWeight = vehicle.MaxGrossWeight,
            CargoLength = vehicle.CargoLength,
            CargoWidth = vehicle.CargoWidth,
            CargoHeight = vehicle.CargoHeight,
            Features = vehicle.Features,
            HasInsurance = vehicle.HasInsurance,
            InsuranceExpiry = vehicle.InsuranceExpiry,
            Status = vehicle.Status,
            LastInspectionDate = vehicle.LastInspectionDate,
            NextInspectionDue = vehicle.NextInspectionDue,
            Mileage = vehicle.Mileage,
            Photos = vehicle.Photos,
            IsActive = vehicle.IsActive,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt
        };
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

public class DriverDtoPaginatedResult
{
    public required List<DriverDto> Items { get; set; }
    public int Total { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class CreateDriverDto
{
    public required string UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string LicenseNumber { get; set; }
    public DateTime LicenseExpiry { get; set; }
    public string? VehicleType { get; set; }
    public string? VehicleRegistration { get; set; }
    public string? Status { get; set; }
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
    public string? VehicleTypeRequired { get; set; }
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