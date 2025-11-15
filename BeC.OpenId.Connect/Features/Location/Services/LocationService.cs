using AutoMapper;
using BeC.Common.Data.Repositories.Interfaces;
using BeC.OpenId.Connect.Dto;
using BeC.OpenId.Connect.Features.Drivers.Dtos;
using BeC.OpenId.Connect.Features.Location.Dtos;
using BeC.OpenId.Connect.Features.Location.Models;
using BeC.OpenId.Connect.Features.Location.Services.Interfaces;
using BeC.OpenId.Connect.Features.Location.ViewModels;
using BeC.OpenId.Connect.Infrastructure.Authorization;
using BeC.OpenId.Connect.Infrastructure.Maps;
using Microsoft.EntityFrameworkCore;
using AuthRoles = BeC.OpenId.Connect.Infrastructure.Authorization.Roles;

namespace BeC.OpenId.Connect.Features.Location.Services;

/// <summary>
/// Implementation of location service
/// </summary>
public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository _repository;
    private readonly IGoogleMapsService _mapsService;
    private readonly IMapper _mapper;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        ApplicationDbContext context,
        IRepository repository,
        IGoogleMapsService mapsService,
        IMapper mapper,
        ILogger<LocationService> logger)
    {
        _context = context;
        _repository = repository;
        _mapsService = mapsService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LocationViewModel> UpdateDriverLocationAsync(UpdateLocationModel model, string userId)
    {
        var driver = await _repository.GetEntity<Driver>(d => d.UserId == userId);
        if (driver == null)
        {
            throw new InvalidOperationException("Driver profile not found");
        }

        // Validate coordinates
        if (model.Latitude < -90 || model.Latitude > 90 ||
            model.Longitude < -180 || model.Longitude > 180)
        {
            throw new ArgumentException("Invalid coordinates");
        }

        // Reverse geocode to get address
        string? address = null;
        try
        {
            address = await _mapsService.ReverseGeocodeAsync(model.Latitude, model.Longitude);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reverse geocode location");
        }

        // Create location record
        var location = new DriverLocation
        {
            DriverId = driver.Id,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            Accuracy = model.Accuracy,
            Speed = model.Speed,
            Heading = model.Heading,
            CurrentJobId = model.CurrentJobId,
            Address = address,
            Timestamp = DateTime.UtcNow
        };

        await _repository.InsertEntity(location);

        _logger.LogInformation("Updated location for driver {DriverId} at {Lat},{Lng}",
            driver.Id, model.Latitude, model.Longitude);

        return _mapper.Map<LocationViewModel>(location);
    }

    public async Task<(bool success, DriverLocationViewModel? location, string? errorMessage)> GetDriverLocationForJobAsync(
        Guid jobId, string userId, IEnumerable<string> userRoles)
    {
        // Get job and verify authorization
        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            return (false, null, "Job not found");
        }

        // Check if user is customer or driver or admin
        var isCustomer = job.CustomerId == userId;
        var isDriver = job.Driver?.UserId == userId;
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        if (!isCustomer && !isDriver && !isAdmin)
        {
            return (false, null, "Forbidden");
        }

        if (job.DriverId == null)
        {
            return (false, null, "No driver assigned to this job");
        }

        // Get most recent location
        var location = await _context.DriverLocations
            .Where(l => l.DriverId == job.DriverId.Value)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefaultAsync();

        if (location == null)
        {
            return (false, null, "Driver location not available");
        }

        var ageInSeconds = (int)(DateTime.UtcNow - location.Timestamp).TotalSeconds;

        var response = new DriverLocationViewModel
        {
            DriverId = job.Driver!.Id,
            DriverName = $"{job.Driver.FirstName} {job.Driver.LastName}",
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Speed = location.Speed,
            Heading = location.Heading,
            Address = location.Address,
            Timestamp = location.Timestamp,
            AgeInSeconds = ageInSeconds
        };

        return (true, response, null);
    }

    public async Task<(bool success, EtaViewModel? eta, string? errorMessage)> CalculateEtaAsync(
        Guid jobId, string userId, IEnumerable<string> userRoles)
    {
        // Get job and verify authorization
        var job = await _context.Jobs
            .Include(j => j.Driver)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            return (false, null, "Job not found");
        }

        // Check authorization
        var isCustomer = job.CustomerId == userId;
        var isDriver = job.Driver?.UserId == userId;
        var isAdmin = userRoles.Contains(AuthRoles.Admin) || userRoles.Contains(AuthRoles.SuperAdmin);

        if (!isCustomer && !isDriver && !isAdmin)
        {
            return (false, null, "Forbidden");
        }

        if (job.DriverId == null)
        {
            return (false, null, "No driver assigned to this job");
        }

        // Get driver's current location
        var driverLocation = await _context.DriverLocations
            .Where(l => l.DriverId == job.DriverId.Value)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefaultAsync();

        if (driverLocation == null)
        {
            return (false, null, "Driver location not available");
        }

        // Check if location is too old (more than 5 minutes)
        if ((DateTime.UtcNow - driverLocation.Timestamp).TotalMinutes > 5)
        {
            var minutesAgo = (int)(DateTime.UtcNow - driverLocation.Timestamp).TotalMinutes;
            return (false, null, $"Driver location is outdated. Last updated {minutesAgo} minutes ago.");
        }

        // Get destination coordinates (pickup address)
        var destinationGeocode = await _mapsService.GeocodeAddressAsync(job.PickupLocation);

        if (destinationGeocode == null)
        {
            return (false, null, "Unable to geocode destination address");
        }

        // Calculate route distance and duration
        var route = await _mapsService.CalculateDistanceAsync(
            driverLocation.Latitude,
            driverLocation.Longitude,
            destinationGeocode.Latitude,
            destinationGeocode.Longitude);

        if (route == null)
        {
            return (false, null, "Unable to calculate route");
        }

        var eta = DateTime.UtcNow.AddMinutes(route.DurationInMinutes);

        var result = new EtaViewModel
        {
            JobId = job.Id,
            DriverId = job.Driver!.Id,
            DriverName = $"{job.Driver.FirstName} {job.Driver.LastName}",
            CurrentLatitude = driverLocation.Latitude,
            CurrentLongitude = driverLocation.Longitude,
            DestinationLatitude = destinationGeocode.Latitude,
            DestinationLongitude = destinationGeocode.Longitude,
            DestinationAddress = job.PickupLocation,
            DistanceInMiles = route.DistanceInMiles,
            DurationInMinutes = route.DurationInMinutes,
            EstimatedArrivalTime = eta,
            DurationText = route.DurationText,
            DistanceText = route.DistanceText
        };

        _logger.LogInformation("Calculated ETA for job {JobId}: {Duration} minutes",
            jobId, route.DurationInMinutes);

        return (true, result, null);
    }

    public async Task<List<LocationViewModel>> GetDriverLocationHistoryAsync(
        Guid driverId, DateTime? startDate, DateTime? endDate)
    {
        var driver = await _repository.GetEntity<Driver>(d => d.Id == driverId);
        if (driver == null)
        {
            throw new InvalidOperationException("Driver not found");
        }

        // Build query with date filters
        var query = _context.DriverLocations.Where(l => l.DriverId == driverId);

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= endDate.Value);
        }

        var history = await query
            .OrderByDescending(l => l.Timestamp)
            .Take(1000) // Limit to prevent excessive data
            .ToListAsync();

        return _mapper.Map<List<LocationViewModel>>(history);
    }

    public async Task<List<DriverLocationViewModel>> GetActiveDriverLocationsAsync()
    {
        // Get all drivers with recent locations (within last 30 minutes)
        var recentCutoff = DateTime.UtcNow.AddMinutes(-30);

        var activeDrivers = await _context.DriverLocations
            .Where(l => l.Timestamp >= recentCutoff)
            .GroupBy(l => l.DriverId)
            .Select(g => g.OrderByDescending(l => l.Timestamp).FirstOrDefault())
            .ToListAsync();

        var driverIds = activeDrivers.Select(l => l!.DriverId).ToList();

        var drivers = await _context.Drivers
            .Where(d => driverIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id);

        var response = activeDrivers
            .Where(l => l != null && drivers.ContainsKey(l.DriverId))
            .Select(l =>
            {
                var driver = drivers[l!.DriverId];
                return new DriverLocationViewModel
                {
                    DriverId = driver.Id,
                    DriverName = $"{driver.FirstName} {driver.LastName}",
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Speed = l.Speed,
                    Heading = l.Heading,
                    Address = l.Address,
                    Timestamp = l.Timestamp,
                    AgeInSeconds = (int)(DateTime.UtcNow - l.Timestamp).TotalSeconds
                };
            })
            .ToList();

        return response;
    }
}
