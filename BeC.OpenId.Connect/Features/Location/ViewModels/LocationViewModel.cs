namespace BeC.OpenId.Connect.Features.Location.ViewModels;

/// <summary>
/// View model for driver location
/// </summary>
public class LocationViewModel
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? CurrentJobId { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// View model for driver location with driver info
/// </summary>
public class DriverLocationViewModel
{
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public string? Address { get; set; }
    public DateTime Timestamp { get; set; }
    public int AgeInSeconds { get; set; }
}

/// <summary>
/// View model for ETA calculation result
/// </summary>
public class EtaViewModel
{
    public Guid JobId { get; set; }
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public string DestinationAddress { get; set; } = string.Empty;
    public double DistanceInMiles { get; set; }
    public int DurationInMinutes { get; set; }
    public DateTime EstimatedArrivalTime { get; set; }
    public string DurationText { get; set; } = string.Empty;
    public string DistanceText { get; set; } = string.Empty;
}
