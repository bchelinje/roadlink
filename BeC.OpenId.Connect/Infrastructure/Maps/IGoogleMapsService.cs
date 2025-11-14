namespace BeC.OpenId.Connect.Infrastructure.Maps;

/// <summary>
/// Service for Google Maps API operations
/// </summary>
public interface IGoogleMapsService
{
    /// <summary>
    /// Geocode an address to get latitude and longitude
    /// </summary>
    Task<GeocodeResult?> GeocodeAddressAsync(string address);

    /// <summary>
    /// Reverse geocode coordinates to get an address
    /// </summary>
    Task<string?> ReverseGeocodeAsync(double latitude, double longitude);

    /// <summary>
    /// Calculate distance and duration between two addresses
    /// </summary>
    Task<DistanceResult?> CalculateDistanceAsync(string origin, string destination);

    /// <summary>
    /// Calculate distance and duration between coordinates
    /// </summary>
    Task<DistanceResult?> CalculateDistanceAsync(double originLat, double originLng, double destLat, double destLng);

    /// <summary>
    /// Get optimized route for multiple waypoints
    /// </summary>
    Task<RouteResult?> GetOptimizedRouteAsync(string origin, string destination, List<string>? waypoints = null);

    /// <summary>
    /// Autocomplete address suggestions
    /// </summary>
    Task<List<string>> AutocompleteAddressAsync(string input, string? sessionToken = null);
}

/// <summary>
/// Result of geocoding operation
/// </summary>
public class GeocodeResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
    public string? PlaceId { get; set; }
}

/// <summary>
/// Distance calculation result
/// </summary>
public class DistanceResult
{
    public double DistanceInMeters { get; set; }
    public double DistanceInMiles { get; set; }
    public double DistanceInKilometers { get; set; }
    public int DurationInSeconds { get; set; }
    public int DurationInMinutes { get; set; }
    public string DistanceText { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
}

/// <summary>
/// Route calculation result
/// </summary>
public class RouteResult
{
    public double TotalDistanceInMeters { get; set; }
    public double TotalDistanceInMiles { get; set; }
    public int TotalDurationInSeconds { get; set; }
    public int TotalDurationInMinutes { get; set; }
    public List<RouteStep> Steps { get; set; } = new();
    public string OverviewPolyline { get; set; } = string.Empty;
}

/// <summary>
/// Individual step in a route
/// </summary>
public class RouteStep
{
    public string Instruction { get; set; } = string.Empty;
    public double DistanceInMeters { get; set; }
    public int DurationInSeconds { get; set; }
    public GeocodeResult StartLocation { get; set; } = new();
    public GeocodeResult EndLocation { get; set; } = new();
}
