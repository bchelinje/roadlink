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
/// Job Templates management for repeat customers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class JobTemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<JobTemplatesController> _logger;

    public JobTemplatesController(
        ApplicationDbContext context,
        IActivityLogService activityLogService,
        ILogger<JobTemplatesController> logger)
    {
        _context = context;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get my job templates
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(typeof(List<JobTemplate>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<JobTemplate>>> GetMyTemplates()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var templates = await _context.JobTemplates
            .Where(t => t.CustomerId == userId && t.Status == "active")
            .OrderByDescending(t => t.IsDefault)
            .ThenByDescending(t => t.LastUsedDate)
            .ThenBy(t => t.TemplateName)
            .ToListAsync();

        return Ok(templates);
    }

    /// <summary>
    /// Get template by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(JobTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobTemplate>> GetTemplate(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var template = await _context.JobTemplates.FindAsync(id);
        if (template == null)
            return NotFound();

        // Check permissions
        if (template.CustomerId != userId)
            return Forbid("You can only view your own templates");

        return Ok(template);
    }

    /// <summary>
    /// Create a new job template
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(typeof(JobTemplate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobTemplate>> CreateTemplate([FromBody] CreateJobTemplateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var template = new JobTemplate
        {
            CustomerId = userId,
            TemplateName = dto.TemplateName,
            Description = dto.Description,
            JobType = dto.JobType,
            VehicleTypeRequired = dto.VehicleTypeRequired,
            Priority = dto.Priority,
            PickupLocation = dto.PickupLocation,
            PickupLatitude = dto.PickupLatitude,
            PickupLongitude = dto.PickupLongitude,
            DeliveryLocation = dto.DeliveryLocation,
            DeliveryLatitude = dto.DeliveryLatitude,
            DeliveryLongitude = dto.DeliveryLongitude,
            EstimatedDistance = dto.EstimatedDistance,
            EstimatedDuration = dto.EstimatedDuration,
            Items = dto.Items != null ? JsonSerializer.Serialize(dto.Items) : null,
            SpecialInstructions = dto.SpecialInstructions,
            CustomerNotes = dto.CustomerNotes,
            StopsConfiguration = dto.Stops != null ? JsonSerializer.Serialize(dto.Stops) : null,
            BasePrice = dto.BasePrice,
            Tags = dto.Tags != null ? string.Join(",", dto.Tags) : null,
            IsDefault = dto.IsDefault,
            Status = "active"
        };

        // If this is set as default, unset other defaults
        if (dto.IsDefault)
        {
            var otherDefaults = await _context.JobTemplates
                .Where(t => t.CustomerId == userId && t.IsDefault)
                .ToListAsync();
            foreach (var t in otherDefaults)
            {
                t.IsDefault = false;
            }
        }

        _context.JobTemplates.Add(template);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "job_template.created",
            "JobTemplate",
            template.Id.ToString(),
            template.TemplateName,
            $"Created job template: {template.TemplateName}"
        );

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    /// <summary>
    /// Update a job template
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(typeof(JobTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobTemplate>> UpdateTemplate(Guid id, [FromBody] UpdateJobTemplateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var template = await _context.JobTemplates.FindAsync(id);
        if (template == null)
            return NotFound();

        if (template.CustomerId != userId)
            return Forbid("You can only update your own templates");

        // Update fields
        if (!string.IsNullOrEmpty(dto.TemplateName)) template.TemplateName = dto.TemplateName;
        if (dto.Description != null) template.Description = dto.Description;
        if (!string.IsNullOrEmpty(dto.JobType)) template.JobType = dto.JobType;
        if (dto.VehicleTypeRequired != null) template.VehicleTypeRequired = dto.VehicleTypeRequired;
        if (dto.Priority != null) template.Priority = dto.Priority;
        if (!string.IsNullOrEmpty(dto.PickupLocation)) template.PickupLocation = dto.PickupLocation;
        if (dto.PickupLatitude.HasValue) template.PickupLatitude = dto.PickupLatitude;
        if (dto.PickupLongitude.HasValue) template.PickupLongitude = dto.PickupLongitude;
        if (!string.IsNullOrEmpty(dto.DeliveryLocation)) template.DeliveryLocation = dto.DeliveryLocation;
        if (dto.DeliveryLatitude.HasValue) template.DeliveryLatitude = dto.DeliveryLatitude;
        if (dto.DeliveryLongitude.HasValue) template.DeliveryLongitude = dto.DeliveryLongitude;
        if (dto.EstimatedDistance.HasValue) template.EstimatedDistance = dto.EstimatedDistance;
        if (dto.EstimatedDuration.HasValue) template.EstimatedDuration = dto.EstimatedDuration;
        if (dto.Items != null) template.Items = JsonSerializer.Serialize(dto.Items);
        if (dto.SpecialInstructions != null) template.SpecialInstructions = dto.SpecialInstructions;
        if (dto.CustomerNotes != null) template.CustomerNotes = dto.CustomerNotes;
        if (dto.Stops != null) template.StopsConfiguration = JsonSerializer.Serialize(dto.Stops);
        if (dto.BasePrice.HasValue) template.BasePrice = dto.BasePrice;
        if (dto.Tags != null) template.Tags = string.Join(",", dto.Tags);

        if (dto.IsDefault.HasValue && dto.IsDefault.Value)
        {
            var otherDefaults = await _context.JobTemplates
                .Where(t => t.CustomerId == userId && t.Id != id && t.IsDefault)
                .ToListAsync();
            foreach (var t in otherDefaults)
            {
                t.IsDefault = false;
            }
            template.IsDefault = true;
        }

        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "job_template.updated",
            "JobTemplate",
            template.Id.ToString(),
            template.TemplateName,
            $"Updated job template: {template.TemplateName}"
        );

        return Ok(template);
    }

    /// <summary>
    /// Delete (archive) a job template
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var template = await _context.JobTemplates.FindAsync(id);
        if (template == null)
            return NotFound();

        if (template.CustomerId != userId)
            return Forbid("You can only delete your own templates");

        // Soft delete - archive instead of removing
        template.Status = "archived";
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "job_template.deleted",
            "JobTemplate",
            template.Id.ToString(),
            template.TemplateName,
            $"Archived job template: {template.TemplateName}"
        );

        return NoContent();
    }

    /// <summary>
    /// Create a job from a template
    /// </summary>
    [HttpPost("{id}/create-job")]
    [Authorize(Roles = Infrastructure.Authorization.Roles.Customer)]
    [ProducesResponseType(typeof(Job), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Job>> CreateJobFromTemplate(Guid id, [FromBody] CreateJobFromTemplateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var template = await _context.JobTemplates.FindAsync(id);
        if (template == null)
            return NotFound("Template not found");

        if (template.CustomerId != userId)
            return Forbid("You can only use your own templates");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Generate job number
        var jobNumber = $"JOB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var job = new Job
        {
            JobNumber = jobNumber,
            CustomerId = userId,
            CustomerName = user.DisplayName ?? user.UserName ?? "Unknown",
            CustomerEmail = user.Email ?? "",
            CustomerPhone = user.PhoneNumber ?? "",
            JobType = template.JobType,
            VehicleTypeRequired = template.VehicleTypeRequired,
            Priority = template.Priority ?? "normal",
            ScheduledDate = dto.ScheduledDate,
            ScheduledTime = dto.ScheduledTime,
            EstimatedDuration = template.EstimatedDuration ?? 0,
            PickupLocation = template.PickupLocation,
            DeliveryLocation = template.DeliveryLocation,
            Distance = (decimal?)template.EstimatedDistance,
            Items = template.Items,
            SpecialInstructions = template.SpecialInstructions,
            CustomerNotes = template.CustomerNotes,
            Status = "pending"
        };

        _context.Jobs.Add(job);

        // Update template usage statistics
        template.TimesUsed++;
        template.LastUsedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            userId,
            "job.created_from_template",
            "Job",
            job.Id.ToString(),
            job.JobNumber,
            $"Created job {job.JobNumber} from template {template.TemplateName}"
        );

        return CreatedAtAction("GetJob", "Jobs", new { id = job.Id }, job);
    }
}

#region DTOs

public class CreateJobTemplateDto
{
    public required string TemplateName { get; set; }
    public string? Description { get; set; }
    public required string JobType { get; set; }
    public string? VehicleTypeRequired { get; set; }
    public string? Priority { get; set; }
    public required string PickupLocation { get; set; }
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public required string DeliveryLocation { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public double? EstimatedDistance { get; set; }
    public int? EstimatedDuration { get; set; }
    public List<object>? Items { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? CustomerNotes { get; set; }
    public List<object>? Stops { get; set; }
    public decimal? BasePrice { get; set; }
    public List<string>? Tags { get; set; }
    public bool IsDefault { get; set; } = false;
}

public class UpdateJobTemplateDto
{
    public string? TemplateName { get; set; }
    public string? Description { get; set; }
    public string? JobType { get; set; }
    public string? VehicleTypeRequired { get; set; }
    public string? Priority { get; set; }
    public string? PickupLocation { get; set; }
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public string? DeliveryLocation { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public double? EstimatedDistance { get; set; }
    public int? EstimatedDuration { get; set; }
    public List<object>? Items { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? CustomerNotes { get; set; }
    public List<object>? Stops { get; set; }
    public decimal? BasePrice { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsDefault { get; set; }
}

public class CreateJobFromTemplateDto
{
    public required DateTime ScheduledDate { get; set; }
    public required string ScheduledTime { get; set; }
}

#endregion
