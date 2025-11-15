using BeC.OpenId.Connect.Features.Location.Models;
using BeC.OpenId.Connect.Features.Location.ViewModels;

namespace BeC.OpenId.Connect.Features.Location.Services.Interfaces;

/// <summary>
/// Service for managing driver locations and ETA calculations
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Update driver's current location
    /// </summary>
    Task<LocationViewModel> UpdateDriverLocationAsync(UpdateLocationModel model, string userId);

    /// <summary>
    /// Get driver's current location for a specific job
    /// </summary>
    Task<(bool success, DriverLocationViewModel? location, string? errorMessage)> GetDriverLocationForJobAsync(Guid jobId, string userId, IEnumerable<string> userRoles);

    /// <summary>
    /// Calculate ETA for driver to reach job location
    /// </summary>
    Task<(bool success, EtaViewModel? eta, string? errorMessage)> CalculateEtaAsync(Guid jobId, string userId, IEnumerable<string> userRoles);

    /// <summary>
    /// Get driver's location history (Admin)
    /// </summary>
    Task<List<LocationViewModel>> GetDriverLocationHistoryAsync(Guid driverId, DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// Get all active drivers with their last known locations (Admin)
    /// </summary>
    Task<List<DriverLocationViewModel>> GetActiveDriverLocationsAsync();
}
