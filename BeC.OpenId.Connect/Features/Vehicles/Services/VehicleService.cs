using System.Text.Json;
using AutoMapper;
using BeC.Common.Data.Repositories.Interfaces;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.ActivityLogs.Services.Interfaces;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Vehicles.Models;
using BeC.OpenId.Connect.Features.Vehicles.Services.Interfaces;
using BeC.OpenId.Connect.Features.Vehicles.ViewModels;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BeC.OpenId.Connect.Features.Vehicles.Services;

/// <summary>
/// Implementation of vehicle service
/// </summary>
public class VehicleService : IVehicleService
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository _repository;
    private readonly IActivityLogService _activityLogService;
    private readonly IMapper _mapper;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(
        ApplicationDbContext context,
        IRepository repository,
        IActivityLogService activityLogService,
        IMapper mapper,
        ILogger<VehicleService> logger)
    {
        _context = context;
        _repository = repository;
        _activityLogService = activityLogService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VehicleListViewModel> GetAllVehiclesAsync(string? status, Guid? driverId, int page, int pageSize)
    {
        // Build predicate based on filters
        System.Linq.Expressions.Expression<Func<Vehicle, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(status) && driverId.HasValue)
        {
            predicate = v => v.Status == status && v.DriverId == driverId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(status))
        {
            predicate = v => v.Status == status;
        }
        else if (driverId.HasValue)
        {
            predicate = v => v.DriverId == driverId.Value;
        }

        // Build query with filters (using DbContext for Include support)
        var query = _context.Vehicles.Include(v => v.Driver).AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();
        var vehicles = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new VehicleListViewModel
        {
            Vehicles = _mapper.Map<List<VehicleViewModel>>(vehicles),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<VehicleViewModel?> GetVehicleByIdAsync(Guid id)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == id);

        return vehicle != null ? _mapper.Map<VehicleViewModel>(vehicle) : null;
    }

    public async Task<List<VehicleViewModel>> GetVehiclesByDriverUserIdAsync(string userId)
    {
        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
        {
            return new List<VehicleViewModel>();
        }

        var vehicles = await _repository.GetEntities<Vehicle, DateTime>(
            v => v.DriverId == driver.Id,
            v => v.CreatedAt,
            isDescending: true
        );

        return _mapper.Map<List<VehicleViewModel>>(vehicles);
    }

    public async Task<VehicleViewModel> CreateVehicleAsync(CreateVehicleModel model, string userId, IEnumerable<string> userRoles)
    {
        // Determine the driver ID
        Guid targetDriverId;

        if (model.DriverId.HasValue)
        {
            // Admin creating vehicle for a specific driver
            if (!userRoles.Contains(Roles.Admin) && !userRoles.Contains(Roles.SuperAdmin))
            {
                throw new UnauthorizedAccessException("Only admins can create vehicles for other drivers");
            }

            targetDriverId = model.DriverId.Value;
        }
        else
        {
            // Driver creating their own vehicle
            var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
            if (driver == null)
            {
                throw new InvalidOperationException("Driver profile not found");
            }

            targetDriverId = driver.Id;
        }

        // Verify driver exists
        var targetDriver = await _repository.GetEntity<Driver>(d => d.Id == targetDriverId);
        if (targetDriver == null)
        {
            throw new InvalidOperationException("Driver not found");
        }

        // Check for duplicate registration number
        var exists = await _repository.Exists<Vehicle>(v => v.RegistrationNumber == model.RegistrationNumber);
        if (exists)
        {
            throw new InvalidOperationException("A vehicle with this registration number already exists");
        }

        var vehicle = new Vehicle
        {
            DriverId = targetDriverId,
            Type = model.Type,
            Make = model.Make,
            Model = model.Model,
            Year = model.Year,
            RegistrationNumber = model.RegistrationNumber,
            VinNumber = model.VinNumber,
            CargoCapacity = model.CargoCapacity,
            MaxPayloadWeight = model.MaxPayloadWeight,
            MaxGrossWeight = model.MaxGrossWeight,
            CargoLength = model.CargoLength,
            CargoWidth = model.CargoWidth,
            CargoHeight = model.CargoHeight,
            Features = model.Features != null ? JsonSerializer.Serialize(model.Features) : null,
            HasInsurance = model.HasInsurance,
            InsuranceExpiry = model.InsuranceExpiry,
            Status = "active",
            LastInspectionDate = model.LastInspectionDate,
            NextInspectionDue = model.NextInspectionDue,
            Mileage = model.Mileage,
            Photos = model.Photos != null ? JsonSerializer.Serialize(model.Photos) : null,
            IsActive = true
        };

        await _repository.InsertEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_created",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNumber})",
            $"Created vehicle for driver {targetDriver.FirstName} {targetDriver.LastName}"
        );

        // Load driver info for response
        vehicle.Driver = targetDriver;

        return _mapper.Map<VehicleViewModel>(vehicle);
    }

    public async Task<(bool success, VehicleViewModel? vehicle, string? errorMessage)> UpdateVehicleAsync(
        Guid id, UpdateVehicleModel model, string userId, IEnumerable<string> userRoles)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle == null)
        {
            return (false, null, "Vehicle not found");
        }

        // Check permissions
        var isAdmin = userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.SuperAdmin);
        var isOwner = vehicle.Driver.UserId == userId;

        if (!isAdmin && !isOwner)
        {
            return (false, null, "You can only update your own vehicles");
        }

        // Update fields
        vehicle.Type = model.Type ?? vehicle.Type;
        vehicle.Make = model.Make ?? vehicle.Make;
        vehicle.Model = model.Model ?? vehicle.Model;
        vehicle.Year = model.Year ?? vehicle.Year;
        vehicle.RegistrationNumber = model.RegistrationNumber ?? vehicle.RegistrationNumber;
        vehicle.VinNumber = model.VinNumber ?? vehicle.VinNumber;
        vehicle.CargoCapacity = model.CargoCapacity ?? vehicle.CargoCapacity;
        vehicle.MaxPayloadWeight = model.MaxPayloadWeight ?? vehicle.MaxPayloadWeight;
        vehicle.MaxGrossWeight = model.MaxGrossWeight ?? vehicle.MaxGrossWeight;
        vehicle.CargoLength = model.CargoLength ?? vehicle.CargoLength;
        vehicle.CargoWidth = model.CargoWidth ?? vehicle.CargoWidth;
        vehicle.CargoHeight = model.CargoHeight ?? vehicle.CargoHeight;
        vehicle.HasInsurance = model.HasInsurance ?? vehicle.HasInsurance;
        vehicle.InsuranceExpiry = model.InsuranceExpiry ?? vehicle.InsuranceExpiry;
        vehicle.LastInspectionDate = model.LastInspectionDate ?? vehicle.LastInspectionDate;
        vehicle.NextInspectionDue = model.NextInspectionDue ?? vehicle.NextInspectionDue;
        vehicle.Mileage = model.Mileage ?? vehicle.Mileage;

        if (model.Features != null)
        {
            vehicle.Features = JsonSerializer.Serialize(model.Features);
        }

        if (model.Photos != null)
        {
            vehicle.Photos = JsonSerializer.Serialize(model.Photos);
        }

        vehicle.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_updated",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model} ({vehicle.RegistrationNumber})",
            "Vehicle updated"
        );

        return (true, _mapper.Map<VehicleViewModel>(vehicle), null);
    }

    public async Task<(bool success, VehicleViewModel? vehicle, string? errorMessage)> UpdateVehicleStatusAsync(
        Guid id, UpdateVehicleStatusModel model, string userId, IEnumerable<string> userRoles)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle == null)
        {
            return (false, null, "Vehicle not found");
        }

        // Check permissions
        var isAdmin = userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.SuperAdmin);
        var isOwner = vehicle.Driver.UserId == userId;

        if (!isAdmin && !isOwner)
        {
            return (false, null, "You can only update your own vehicles");
        }

        var oldStatus = vehicle.Status;
        vehicle.Status = model.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_status_changed",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model}",
            $"Status changed from {oldStatus} to {model.Status}"
        );

        return (true, _mapper.Map<VehicleViewModel>(vehicle), null);
    }

    public async Task<(bool success, VehicleViewModel? vehicle, string? errorMessage)> LogMaintenanceAsync(
        Guid id, LogMaintenanceModel model, string userId, IEnumerable<string> userRoles)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle == null)
        {
            return (false, null, "Vehicle not found");
        }

        // Check permissions
        var isAdmin = userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.SuperAdmin);
        var isOwner = vehicle.Driver.UserId == userId;

        if (!isAdmin && !isOwner)
        {
            return (false, null, "You can only log maintenance for your own vehicles");
        }

        vehicle.LastInspectionDate = model.MaintenanceDate ?? DateTime.UtcNow;
        vehicle.NextInspectionDue = model.NextInspectionDue;
        vehicle.Mileage = model.Mileage ?? vehicle.Mileage;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_maintenance",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model}",
            $"Maintenance logged: {model.Description ?? "Routine maintenance"}"
        );

        return (true, _mapper.Map<VehicleViewModel>(vehicle), null);
    }

    public async Task<List<MaintenanceHistoryViewModel>> GetMaintenanceHistoryAsync(Guid id)
    {
        var maintenanceLogs = await _context.ActivityLogs
            .Where(a => a.EntityType == "Vehicle" &&
                       a.EntityId == id.ToString() &&
                       a.Action == "vehicle_maintenance")
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        return _mapper.Map<List<MaintenanceHistoryViewModel>>(maintenanceLogs);
    }

    public async Task<(bool success, string? errorMessage)> DeleteVehicleAsync(
        Guid id, string userId, IEnumerable<string> userRoles)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle == null)
        {
            return (false, "Vehicle not found");
        }

        // Check permissions
        var isAdmin = userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.SuperAdmin);
        var isOwner = vehicle.Driver.UserId == userId;

        if (!isAdmin && !isOwner)
        {
            return (false, "You can only delete your own vehicles");
        }

        // Soft delete
        vehicle.IsActive = false;
        vehicle.Status = "retired";
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateEntity(vehicle);

        await _activityLogService.LogActivityAsync(
            userId,
            "vehicle_deleted",
            "Vehicle",
            vehicle.Id.ToString(),
            $"{vehicle.Make} {vehicle.Model}",
            "Vehicle deleted (soft delete)"
        );

        return (true, null);
    }

    public async Task<bool> CanUserAccessVehicleAsync(Guid vehicleId, string userId, IEnumerable<string> userRoles)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == vehicleId);

        if (vehicle == null)
        {
            return false;
        }

        var isAdmin = userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.SuperAdmin);
        var isOwner = vehicle.Driver.UserId == userId;

        return isAdmin || isOwner;
    }
}
