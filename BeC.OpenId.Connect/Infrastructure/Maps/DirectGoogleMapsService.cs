using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeC.OpenId.Connect.Infrastructure.Maps;

/// <summary>
/// Direct HTTP implementation of Google Maps API (bypasses GoogleApi library issues)
/// </summary>
public class DirectGoogleMapsService : IGoogleMapsService
{
    private readonly string _apiKey;
    private readonly ILogger<DirectGoogleMapsService> _logger;
    private readonly HttpClient _httpClient;

    public DirectGoogleMapsService(
        IConfiguration configuration,
        ILogger<DirectGoogleMapsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _apiKey = configuration["GoogleMaps:ApiKey"]
            ?? throw new InvalidOperationException("GoogleMaps:ApiKey not configured");
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<DistanceResult?> CalculateDistanceAsync(string origin, string destination)
    {
        try
        {
            _logger.LogInformation("Calculating distance from '{Origin}' to '{Destination}'", origin, destination);

            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={Uri.EscapeDataString(origin)}&destinations={Uri.EscapeDataString(destination)}&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Google Maps API response: {Content}", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("API request failed with status {Status}: {Content}", response.StatusCode, content);
                return null;
            }

            var result = JsonSerializer.Deserialize<DistanceMatrixResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Status != "OK" || result.Rows == null || !result.Rows.Any())
            {
                _logger.LogWarning("Distance calculation failed. Status: {Status}", result?.Status);
                return null;
            }

            var element = result.Rows[0].Elements?[0];
            if (element?.Status != "OK" || element.Distance == null || element.Duration == null)
            {
                _logger.LogWarning("Distance element status not OK: {Status}", element?.Status);
                return null;
            }

            var distanceMeters = element.Distance.Value;
            var durationSeconds = element.Duration.Value;

            _logger.LogInformation("Successfully calculated distance: {Distance} meters", distanceMeters);

            return new DistanceResult
            {
                DistanceInMeters = distanceMeters,
                DistanceInMiles = distanceMeters * 0.000621371,
                DistanceInKilometers = distanceMeters / 1000.0,
                DurationInSeconds = durationSeconds,
                DurationInMinutes = durationSeconds / 60,
                DistanceText = element.Distance.Text ?? "",
                DurationText = element.Duration.Text ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance between {Origin} and {Destination}", origin, destination);
            return null;
        }
    }

    public Task<DistanceResult?> CalculateDistanceAsync(double originLat, double originLng, double destLat, double destLng)
    {
        return CalculateDistanceAsync($"{originLat},{originLng}", $"{destLat},{destLng}");
    }

    public async Task<GeocodeResult?> GeocodeAddressAsync(string address)
    {
        try
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Geocoding request failed: {Content}", content);
                return null;
            }

            var result = JsonSerializer.Deserialize<GeocodeResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Status != "OK" || result.Results == null || !result.Results.Any())
            {
                return null;
            }

            var firstResult = result.Results[0];
            return new GeocodeResult
            {
                Latitude = firstResult.Geometry?.Location?.Lat ?? 0,
                Longitude = firstResult.Geometry?.Location?.Lng ?? 0,
                FormattedAddress = firstResult.FormattedAddress ?? "",
                PlaceId = firstResult.PlaceId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            return null;
        }
    }

    public async Task<string?> ReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<GeocodeResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Results?.FirstOrDefault()?.FormattedAddress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding: {Lat}, {Lng}", latitude, longitude);
            return null;
        }
    }

    public async Task<List<string>> AutocompleteAddressAsync(string input, string? sessionToken = null)
    {
        try
        {
            var url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={Uri.EscapeDataString(input)}&key={_apiKey}";

            if (!string.IsNullOrEmpty(sessionToken))
            {
                url += $"&sessiontoken={sessionToken}";
            }

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Autocomplete request failed: {Content}", content);
                return new List<string>();
            }

            var result = JsonSerializer.Deserialize<AutocompleteResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Predictions?.Select(p => p.Description ?? "").Where(d => !string.IsNullOrEmpty(d)).ToList()
                ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error autocompleting address: {Input}", input);
            return new List<string>();
        }
    }

    public Task<RouteResult?> GetOptimizedRouteAsync(string origin, string destination, List<string>? waypoints = null)
    {
        // This is more complex, return null for now
        _logger.LogWarning("GetOptimizedRouteAsync not implemented in DirectGoogleMapsService");
        return Task.FromResult<RouteResult?>(null);
    }
}

#region Response DTOs

public class DistanceMatrixResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("rows")]
    public List<DistanceMatrixRow>? Rows { get; set; }
}

public class DistanceMatrixRow
{
    [JsonPropertyName("elements")]
    public List<DistanceMatrixElement>? Elements { get; set; }
}

public class DistanceMatrixElement
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("distance")]
    public DistanceInfo? Distance { get; set; }

    [JsonPropertyName("duration")]
    public DurationInfo? Duration { get; set; }
}

public class DistanceInfo
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

public class DurationInfo
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public class GeocodeResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("results")]
    public List<GeocodeResponseResult>? Results { get; set; }
}

public class GeocodeResponseResult
{
    [JsonPropertyName("formatted_address")]
    public string? FormattedAddress { get; set; }

    [JsonPropertyName("geometry")]
    public GeocodeGeometry? Geometry { get; set; }

    [JsonPropertyName("place_id")]
    public string? PlaceId { get; set; }
}

public class GeocodeGeometry
{
    [JsonPropertyName("location")]
    public GeocodeLocation? Location { get; set; }
}

public class GeocodeLocation
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}

public class AutocompleteResponse
{
    [JsonPropertyName("predictions")]
    public List<AutocompletePrediction>? Predictions { get; set; }
}

public class AutocompletePrediction
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

#endregion
