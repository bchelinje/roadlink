using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeC.OpenId.Connect.Features.Location.Dtos;

/// <summary>
/// Real-time driver location tracking
/// </summary>
[Table("DriverLocations")]
public class DriverLocation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DriverId { get; set; }

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public double? Accuracy { get; set; } // in meters

    public double? Speed { get; set; } // in meters per second

    public double? Heading { get; set; } // in degrees (0-360)

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Current job (if driver is on a job)
    public Guid? CurrentJobId { get; set; }

    // Address (reverse geocoded from coordinates)
    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request to update driver location
/// </summary>
public class UpdateLocationRequest
{
    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public double? Accuracy { get; set; }

    public double? Speed { get; set; }

    public double? Heading { get; set; }

    public Guid? CurrentJobId { get; set; }
}

/// <summary>
/// Driver location response with additional info
/// </summary>
public class DriverLocationResponse
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
/// ETA calculation result
/// </summary>
public class EtaCalculationResult
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
