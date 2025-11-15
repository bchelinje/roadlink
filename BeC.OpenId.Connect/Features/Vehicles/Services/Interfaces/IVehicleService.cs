using BeC.OpenId.Connect.Features.Vehicles.Models;
using BeC.OpenId.Connect.Features.Vehicles.ViewModels;

namespace BeC.OpenId.Connect.Features.Vehicles.Services.Interfaces;

/// <summary>
/// Service for managing vehicles
/// </summary>
public interface IVehicleService
{
    /// <summary>
    /// Get all vehicles with pagination and filtering (Admin only)
    /// </summary>
    Task<VehicleListViewModel> GetAllVehiclesAsync(string? status, Guid? driverId, int page, int pageSize);

    /// <summary>
    /// Get vehicle by ID
    /// </summary>
    Task<VehicleViewModel?> GetVehicleByIdAsync(Guid id);

    /// <summary>
    /// Get vehicles for a specific driver
    /// </summary>
    Task<List<VehicleViewModel>> GetVehiclesByDriverUserIdAsync(string userId);

    /// <summary>
    /// Create a new vehicle
    /// </summary>
    Task<VehicleViewModel> CreateVehicleAsync(CreateVehicleModel model, string userId, IEnumerable<string> userRoles);

    /// <summary>
    /// Update an existing vehicle
    /// </summary>
    Task<(bool success, VehicleViewModel? vehicle, string? errorMessage)> UpdateVehicleAsync(Guid id, UpdateVehicleModel model, string userId, IEnumerable<string> userRoles);

    /// <summary>
    /// Update vehicle status
    /// </summary>
    Task<(bool success, VehicleViewModel? vehicle, string? errorMessage)> UpdateVehicleStatusAsync(Guid id, UpdateVehicleStatusModel model, string userId, IEnumerable<string> userRoles);

    /// <summary>
    /// Log vehicle maintenance
    /// </summary>
    Task<(bool success, VehicleViewModel? vehicle, string? errorMessage)> LogMaintenanceAsync(Guid id, LogMaintenanceModel model, string userId, IEnumerable<string> userRoles);

    /// <summary>
    /// Get vehicle maintenance history
    /// </summary>
    Task<List<MaintenanceHistoryViewModel>> GetMaintenanceHistoryAsync(Guid id);

    /// <summary>
    /// Delete (soft delete) a vehicle
    /// </summary>
    Task<(bool success, string? errorMessage)> DeleteVehicleAsync(Guid id, string userId, IEnumerable<string> userRoles);

    /// <summary>
    /// Check if user has access to a vehicle
    /// </summary>
    Task<bool> CanUserAccessVehicleAsync(Guid vehicleId, string userId, IEnumerable<string> userRoles);
}
