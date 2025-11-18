using System.Security.Claims;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Settings.Dtos;
using BeC.OpenId.Connect.Features.Users.Dtos;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Settings.Controllers;

/// <summary>
/// Settings management endpoints for users, drivers, customers, and platform
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLogService,
        ILogger<SettingsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    #region User Settings

    /// <summary>
    /// Get current user's settings
    /// </summary>
    [HttpGet("user")]
    [ProducesResponseType(typeof(UserSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSettings>> GetUserSettings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create default settings if they don't exist
            settings = new UserSettings
            {
                UserId = userId
            };
            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(settings);
    }

    /// <summary>
    /// Update current user's settings
    /// </summary>
    [HttpPut("user")]
    [ProducesResponseType(typeof(UserSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserSettings>> UpdateUserSettings([FromBody] UpdateUserSettingsDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create new settings
            settings = new UserSettings
            {
                UserId = userId
            };
            _context.UserSettings.Add(settings);
        }

        // Update only provided fields
        if (dto.PreferredLanguage != null) settings.PreferredLanguage = dto.PreferredLanguage;
        if (dto.TimeZone != null) settings.TimeZone = dto.TimeZone;
        if (dto.Currency != null) settings.Currency = dto.Currency;
        if (dto.DateFormat != null) settings.DateFormat = dto.DateFormat;
        if (dto.TimeFormat != null) settings.TimeFormat = dto.TimeFormat;

        if (dto.ShowProfileToPublic.HasValue) settings.ShowProfileToPublic = dto.ShowProfileToPublic.Value;
        if (dto.AllowDataSharing.HasValue) settings.AllowDataSharing = dto.AllowDataSharing.Value;
        if (dto.ShareLocationWithDriver.HasValue) settings.ShareLocationWithDriver = dto.ShareLocationWithDriver.Value;
        if (dto.ShowOnlineStatus.HasValue) settings.ShowOnlineStatus = dto.ShowOnlineStatus.Value;
        if (dto.AllowMarketingEmails.HasValue) settings.AllowMarketingEmails = dto.AllowMarketingEmails.Value;

        if (dto.TwoFactorEnabled.HasValue) settings.TwoFactorEnabled = dto.TwoFactorEnabled.Value;
        if (dto.EmailVerificationRequired.HasValue) settings.EmailVerificationRequired = dto.EmailVerificationRequired.Value;
        if (dto.PhoneVerificationRequired.HasValue) settings.PhoneVerificationRequired = dto.PhoneVerificationRequired.Value;
        if (dto.SessionTimeoutMinutes.HasValue) settings.SessionTimeoutMinutes = dto.SessionTimeoutMinutes.Value;
        if (dto.RequirePasswordChangeEvery90Days.HasValue) settings.RequirePasswordChangeEvery90Days = dto.RequirePasswordChangeEvery90Days.Value;

        if (dto.PreferredContactMethod != null) settings.PreferredContactMethod = dto.PreferredContactMethod;

        if (dto.Theme != null) settings.Theme = dto.Theme;
        if (dto.HighContrastMode.HasValue) settings.HighContrastMode = dto.HighContrastMode.Value;
        if (dto.ReducedMotion.HasValue) settings.ReducedMotion = dto.ReducedMotion.Value;

        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "SETTINGS_UPDATED",
            entityType: "USER_SETTINGS",
            entityId: settings.Id.ToString(),
            entityName: "User Settings",
            description: $"User settings updated for user: {userId}",
            severity: "INFO"
        );

        return Ok(settings);
    }

    #endregion

    #region Driver Settings

    /// <summary>
    /// Get driver settings (Driver role required)
    /// </summary>
    [HttpGet("driver")]
    [Authorize(Roles = AuthRoles.Driver + "," + AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(DriverSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DriverSettings>> GetDriverSettings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var settings = await _context.DriverSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create default settings if they don't exist
            settings = new DriverSettings
            {
                UserId = userId
            };
            _context.DriverSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(settings);
    }

    /// <summary>
    /// Update driver settings (Driver role required)
    /// </summary>
    [HttpPut("driver")]
    [Authorize(Roles = AuthRoles.Driver + "," + AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(DriverSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DriverSettings>> UpdateDriverSettings([FromBody] UpdateDriverSettingsDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var settings = await _context.DriverSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create new settings
            settings = new DriverSettings
            {
                UserId = userId
            };
            _context.DriverSettings.Add(settings);
        }

        // Update only provided fields
        if (dto.AcceptingJobs.HasValue) settings.AcceptingJobs = dto.AcceptingJobs.Value;
        if (dto.MaxServiceRadiusMiles.HasValue) settings.MaxServiceRadiusMiles = dto.MaxServiceRadiusMiles.Value;
        if (dto.WorkingHours != null) settings.WorkingHours = dto.WorkingHours;
        if (dto.DaysOff != null) settings.DaysOff = dto.DaysOff;

        if (dto.MinimumJobValue.HasValue) settings.MinimumJobValue = dto.MinimumJobValue.Value;
        if (dto.MaximumJobDistanceMiles.HasValue) settings.MaximumJobDistanceMiles = dto.MaximumJobDistanceMiles.Value;
        if (dto.PreferredJobTypes != null) settings.PreferredJobTypes = dto.PreferredJobTypes;
        if (dto.PreferredVehicleTypes != null) settings.PreferredVehicleTypes = dto.PreferredVehicleTypes;
        if (dto.AutoAcceptJobs.HasValue) settings.AutoAcceptJobs = dto.AutoAcceptJobs.Value;
        if (dto.AutoAcceptRadiusMiles.HasValue) settings.AutoAcceptRadiusMiles = dto.AutoAcceptRadiusMiles.Value;

        if (dto.PayoutFrequency != null) settings.PayoutFrequency = dto.PayoutFrequency;
        if (dto.BankAccountLast4 != null) settings.BankAccountLast4 = dto.BankAccountLast4;
        if (dto.StripeAccountId != null) settings.StripeAccountId = dto.StripeAccountId;
        if (dto.InstantPayoutEnabled.HasValue) settings.InstantPayoutEnabled = dto.InstantPayoutEnabled.Value;
        if (dto.MinimumPayoutAmount.HasValue) settings.MinimumPayoutAmount = dto.MinimumPayoutAmount.Value;

        if (dto.NotifyOnNewJobsNearby.HasValue) settings.NotifyOnNewJobsNearby = dto.NotifyOnNewJobsNearby.Value;
        if (dto.NotifyOnJobRequests.HasValue) settings.NotifyOnJobRequests = dto.NotifyOnJobRequests.Value;
        if (dto.NotifyOnPayoutProcessed.HasValue) settings.NotifyOnPayoutProcessed = dto.NotifyOnPayoutProcessed.Value;
        if (dto.NotifyOnLowRating.HasValue) settings.NotifyOnLowRating = dto.NotifyOnLowRating.Value;

        if (dto.DefaultVehicleId.HasValue) settings.DefaultVehicleId = dto.DefaultVehicleId.Value;

        if (dto.SharePerformanceMetrics.HasValue) settings.SharePerformanceMetrics = dto.SharePerformanceMetrics.Value;
        if (dto.ParticipateInLeaderboard.HasValue) settings.ParticipateInLeaderboard = dto.ParticipateInLeaderboard.Value;

        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "SETTINGS_UPDATED",
            entityType: "DRIVER_SETTINGS",
            entityId: settings.Id.ToString(),
            entityName: "Driver Settings",
            description: $"Driver settings updated for user: {userId}",
            severity: "INFO"
        );

        return Ok(settings);
    }

    #endregion

    #region Customer Settings

    /// <summary>
    /// Get customer settings (Customer role required)
    /// </summary>
    [HttpGet("customer")]
    [Authorize(Roles = AuthRoles.Customer + "," + AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(CustomerSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CustomerSettings>> GetCustomerSettings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var settings = await _context.CustomerSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create default settings if they don't exist
            settings = new CustomerSettings
            {
                UserId = userId
            };
            _context.CustomerSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(settings);
    }

    /// <summary>
    /// Update customer settings (Customer role required)
    /// </summary>
    [HttpPut("customer")]
    [Authorize(Roles = AuthRoles.Customer + "," + AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(CustomerSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CustomerSettings>> UpdateCustomerSettings([FromBody] UpdateCustomerSettingsDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var settings = await _context.CustomerSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create new settings
            settings = new CustomerSettings
            {
                UserId = userId
            };
            _context.CustomerSettings.Add(settings);
        }

        // Update only provided fields
        if (dto.DefaultVehicleType != null) settings.DefaultVehicleType = dto.DefaultVehicleType;
        if (dto.AutoBookFavoriteDriver.HasValue) settings.AutoBookFavoriteDriver = dto.AutoBookFavoriteDriver.Value;
        if (dto.AllowAlternativeDrivers.HasValue) settings.AllowAlternativeDrivers = dto.AllowAlternativeDrivers.Value;
        if (dto.PreferredMaxDistance.HasValue) settings.PreferredMaxDistance = dto.PreferredMaxDistance.Value;
        if (dto.DefaultPickupAddress != null) settings.DefaultPickupAddress = dto.DefaultPickupAddress;
        if (dto.DefaultDeliveryAddress != null) settings.DefaultDeliveryAddress = dto.DefaultDeliveryAddress;

        if (dto.DefaultPaymentMethodId != null) settings.DefaultPaymentMethodId = dto.DefaultPaymentMethodId;
        if (dto.SavePaymentMethods.HasValue) settings.SavePaymentMethods = dto.SavePaymentMethods.Value;
        if (dto.AutoTipEnabled.HasValue) settings.AutoTipEnabled = dto.AutoTipEnabled.Value;
        if (dto.DefaultTipPercentage.HasValue) settings.DefaultTipPercentage = dto.DefaultTipPercentage.Value;
        if (dto.RequestReceiptByEmail.HasValue) settings.RequestReceiptByEmail = dto.RequestReceiptByEmail.Value;

        if (dto.NotifyOnDriverAssigned.HasValue) settings.NotifyOnDriverAssigned = dto.NotifyOnDriverAssigned.Value;
        if (dto.NotifyOnDriverArriving.HasValue) settings.NotifyOnDriverArriving = dto.NotifyOnDriverArriving.Value;
        if (dto.NotifyOnJobStarted.HasValue) settings.NotifyOnJobStarted = dto.NotifyOnJobStarted.Value;
        if (dto.NotifyOnJobCompleted.HasValue) settings.NotifyOnJobCompleted = dto.NotifyOnJobCompleted.Value;
        if (dto.NotifyOnSpecialOffers.HasValue) settings.NotifyOnSpecialOffers = dto.NotifyOnSpecialOffers.Value;

        if (dto.ShowDriverRating.HasValue) settings.ShowDriverRating = dto.ShowDriverRating.Value;
        if (dto.ShowPriceEstimate.HasValue) settings.ShowPriceEstimate = dto.ShowPriceEstimate.Value;
        if (dto.ShowDriverLocation.HasValue) settings.ShowDriverLocation = dto.ShowDriverLocation.Value;
        if (dto.EnableJobTracking.HasValue) settings.EnableJobTracking = dto.EnableJobTracking.Value;

        if (dto.RequireAccessibleVehicle.HasValue) settings.RequireAccessibleVehicle = dto.RequireAccessibleVehicle.Value;
        if (dto.RequireDriverAssistance.HasValue) settings.RequireDriverAssistance = dto.RequireDriverAssistance.Value;
        if (dto.SpecialRequirements != null) settings.SpecialRequirements = dto.SpecialRequirements;

        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "SETTINGS_UPDATED",
            entityType: "CUSTOMER_SETTINGS",
            entityId: settings.Id.ToString(),
            entityName: "Customer Settings",
            description: $"Customer settings updated for user: {userId}",
            severity: "INFO"
        );

        return Ok(settings);
    }

    #endregion

    #region Platform Settings (Admin Only)

    /// <summary>
    /// Get all platform settings (Admin only)
    /// </summary>
    [HttpGet("platform")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(List<PlatformSettings>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<PlatformSettings>>> GetPlatformSettings(
        [FromQuery] string? category = null,
        [FromQuery] bool? isPublic = null)
    {
        var query = _context.PlatformSettings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Category == category);
        }

        if (isPublic.HasValue)
        {
            query = query.Where(s => s.IsPublic == isPublic.Value);
        }

        var settings = await query.OrderBy(s => s.Category).ThenBy(s => s.SettingName).ToListAsync();

        return Ok(settings);
    }

    /// <summary>
    /// Get public platform settings (accessible by all authenticated users)
    /// </summary>
    [HttpGet("platform/public")]
    [ProducesResponseType(typeof(List<PlatformSettings>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlatformSettings>>> GetPublicPlatformSettings()
    {
        var settings = await _context.PlatformSettings
            .Where(s => s.IsPublic)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SettingName)
            .ToListAsync();

        return Ok(settings);
    }

    /// <summary>
    /// Get platform setting by key
    /// </summary>
    [HttpGet("platform/{key}")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(PlatformSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlatformSettings>> GetPlatformSettingByKey(string key)
    {
        var setting = await _context.PlatformSettings
            .FirstOrDefaultAsync(s => s.SettingKey == key);

        if (setting == null)
            return NotFound(new { message = $"Platform setting with key '{key}' not found" });

        return Ok(setting);
    }

    /// <summary>
    /// Create platform setting (SuperAdmin only)
    /// </summary>
    [HttpPost("platform")]
    [Authorize(Roles = AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(PlatformSettings), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlatformSettings>> CreatePlatformSetting([FromBody] CreatePlatformSettingDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Check if setting key already exists
        var exists = await _context.PlatformSettings.AnyAsync(s => s.SettingKey == dto.SettingKey);
        if (exists)
            return BadRequest(new { message = $"Platform setting with key '{dto.SettingKey}' already exists" });

        var setting = new PlatformSettings
        {
            SettingKey = dto.SettingKey,
            SettingName = dto.SettingName,
            SettingValue = dto.SettingValue,
            ValueType = dto.ValueType ?? "string",
            Description = dto.Description,
            Category = dto.Category,
            IsPublic = dto.IsPublic,
            IsEditable = dto.IsEditable,
            UpdatedBy = userId
        };

        _context.PlatformSettings.Add(setting);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "PLATFORM_SETTING_CREATED",
            entityType: "PLATFORM_SETTINGS",
            entityId: setting.Id.ToString(),
            entityName: setting.SettingName,
            description: $"Platform setting created: {setting.SettingKey}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["SettingKey"] = setting.SettingKey,
                ["Category"] = setting.Category ?? "N/A"
            }
        );

        return CreatedAtAction(nameof(GetPlatformSettingByKey), new { key = setting.SettingKey }, setting);
    }

    /// <summary>
    /// Update platform setting (Admin only)
    /// </summary>
    [HttpPut("platform/{key}")]
    [Authorize(Roles = AuthRoles.Admin + "," + AuthRoles.SuperAdmin)]
    [ProducesResponseType(typeof(PlatformSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlatformSettings>> UpdatePlatformSetting(
        string key,
        [FromBody] UpdatePlatformSettingDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var setting = await _context.PlatformSettings
            .FirstOrDefaultAsync(s => s.SettingKey == key);

        if (setting == null)
            return NotFound(new { message = $"Platform setting with key '{key}' not found" });

        if (!setting.IsEditable)
            return BadRequest(new { message = "This setting is not editable" });

        setting.SettingValue = dto.SettingValue;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "PLATFORM_SETTING_UPDATED",
            entityType: "PLATFORM_SETTINGS",
            entityId: setting.Id.ToString(),
            entityName: setting.SettingName,
            description: $"Platform setting updated: {setting.SettingKey}",
            severity: "INFO",
            metadata: new Dictionary<string, object>
            {
                ["SettingKey"] = setting.SettingKey,
                ["OldValue"] = "redacted",
                ["NewValue"] = "redacted"
            }
        );

        return Ok(setting);
    }

    /// <summary>
    /// Delete platform setting (SuperAdmin only)
    /// </summary>
    [HttpDelete("platform/{key}")]
    [Authorize(Roles = AuthRoles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePlatformSetting(string key)
    {
        var setting = await _context.PlatformSettings
            .FirstOrDefaultAsync(s => s.SettingKey == key);

        if (setting == null)
            return NotFound(new { message = $"Platform setting with key '{key}' not found" });

        _context.PlatformSettings.Remove(setting);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            action: "PLATFORM_SETTING_DELETED",
            entityType: "PLATFORM_SETTINGS",
            entityId: setting.Id.ToString(),
            entityName: setting.SettingName,
            description: $"Platform setting deleted: {setting.SettingKey}",
            severity: "WARNING",
            metadata: new Dictionary<string, object>
            {
                ["SettingKey"] = setting.SettingKey
            }
        );

        return NoContent();
    }

    #endregion
}
