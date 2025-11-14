using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Common.Enums;
using GoogleApi.Entities.Maps.Common;
using GoogleApi.Entities.Maps.Directions.Request;
using GoogleApi.Entities.Maps.DistanceMatrix.Request;
using GoogleApi.Entities.Maps.Geocoding;
using GoogleApi.Entities.Maps.Geocoding.Address.Request;
using GoogleApi.Entities.Maps.Geocoding.Location.Request;
using GoogleApi.Entities.Places.AutoComplete.Request;

namespace BeC.OpenId.Connect.Infrastructure.Maps;

/// <summary>
/// Implementation of Google Maps service
/// </summary>
public class GoogleMapsService : IGoogleMapsService
{
    private readonly string _apiKey;
    private readonly ILogger<GoogleMapsService> _logger;

    public GoogleMapsService(IConfiguration configuration, ILogger<GoogleMapsService> logger)
    {
        _apiKey = configuration["GoogleMaps:ApiKey"]
            ?? throw new InvalidOperationException("GoogleMaps:ApiKey not configured");
        _logger = logger;
    }

    public async Task<GeocodeResult?> GeocodeAddressAsync(string address)
    {
        try
        {
            var request = new AddressGeocodeRequest
            {
                Key = _apiKey,
                Address = address
            };

            var response = await GoogleMaps.AddressGeocode.QueryAsync(request);

            if (response.Status != Status.Ok || !response.Results.Any())
            {
                _logger.LogWarning("Geocoding failed for address: {Address}. Status: {Status}",
                    address, response.Status);
                return null;
            }

            var result = response.Results.First();
            return new GeocodeResult
            {
                Latitude = result.Geometry.Location.Latitude,
                Longitude = result.Geometry.Location.Longitude,
                FormattedAddress = result.FormattedAddress,
                PlaceId = result.PlaceId
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
            var request = new LocationGeocodeRequest
            {
                Key = _apiKey,
                Location = new Coordinate(latitude, longitude)
            };

            var response = await GoogleMaps.LocationGeocode.QueryAsync(request);

            if (response.Status != Status.Ok || !response.Results.Any())
            {
                _logger.LogWarning("Reverse geocoding failed for coordinates: {Lat}, {Lng}. Status: {Status}",
                    latitude, longitude, response.Status);
                return null;
            }

            return response.Results.First().FormattedAddress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding coordinates: {Lat}, {Lng}", latitude, longitude);
            return null;
        }
    }

    public async Task<DistanceResult?> CalculateDistanceAsync(string origin, string destination)
    {
        try
        {
            var request = new DistanceMatrixRequest
            {
                Key = _apiKey,
                Origins = new[] { new LocationEx(origin) },
                Destinations = new[] { new LocationEx(destination) },
                TravelMode = GoogleApi.Entities.Maps.Common.Enums.TravelMode.Driving
            };

            var response = await GoogleMaps.DistanceMatrix.QueryAsync(request);

            if (response.Status != Status.Ok || !response.Rows.Any())
            {
                _logger.LogWarning("Distance calculation failed. Origin: {Origin}, Destination: {Destination}. Status: {Status}",
                    origin, destination, response.Status);
                return null;
            }

            var element = response.Rows.First().Elements.First();
            if (element.Status != Status.Ok)
            {
                _logger.LogWarning("Distance element status not OK: {Status}", element.Status);
                return null;
            }

            var distanceMeters = element.Distance.Value;
            var durationSeconds = element.Duration.Value;

            return new DistanceResult
            {
                DistanceInMeters = distanceMeters,
                DistanceInMiles = distanceMeters * 0.000621371,
                DistanceInKilometers = distanceMeters / 1000.0,
                DurationInSeconds = durationSeconds,
                DurationInMinutes = durationSeconds / 60,
                DistanceText = element.Distance.Text,
                DurationText = element.Duration.Text
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance between {Origin} and {Destination}", origin, destination);
            return null;
        }
    }

    public async Task<DistanceResult?> CalculateDistanceAsync(double originLat, double originLng, double destLat, double destLng)
    {
        try
        {
            var request = new DistanceMatrixRequest
            {
                Key = _apiKey,
                Origins = new[] { new LocationEx($"{originLat},{originLng}") },
                Destinations = new[] { new LocationEx($"{destLat},{destLng}") },
                TravelMode = GoogleApi.Entities.Maps.Common.Enums.TravelMode.Driving
            };

            var response = await GoogleMaps.DistanceMatrix.QueryAsync(request);

            if (response.Status != Status.Ok || !response.Rows.Any())
            {
                _logger.LogWarning("Distance calculation failed for coordinates. Status: {Status}", response.Status);
                return null;
            }

            var element = response.Rows.First().Elements.First();
            if (element.Status != Status.Ok)
            {
                return null;
            }

            var distanceMeters = element.Distance.Value;
            var durationSeconds = element.Duration.Value;

            return new DistanceResult
            {
                DistanceInMeters = distanceMeters,
                DistanceInMiles = distanceMeters * 0.000621371,
                DistanceInKilometers = distanceMeters / 1000.0,
                DurationInSeconds = durationSeconds,
                DurationInMinutes = durationSeconds / 60,
                DistanceText = element.Distance.Text,
                DurationText = element.Duration.Text
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance between coordinates");
            return null;
        }
    }

    public async Task<RouteResult?> GetOptimizedRouteAsync(string origin, string destination, List<string>? waypoints = null)
    {
        try
        {
            var request = new DirectionsRequest
            {
                Key = _apiKey,
                Origin = new LocationEx(origin),
                Destination = new LocationEx(destination),
                TravelMode = GoogleApi.Entities.Maps.Common.Enums.TravelMode.Driving,
                OptimizeWaypoints = true
            };

            if (waypoints?.Any() == true)
            {
                request.WayPoints = waypoints.Select(w => new WayPoint(new LocationEx(w))).ToArray();
            }

            var response = await GoogleMaps.Directions.QueryAsync(request);

            if (response.Status != Status.Ok || !response.Routes.Any())
            {
                _logger.LogWarning("Route calculation failed. Status: {Status}", response.Status);
                return null;
            }

            var route = response.Routes.First();
            var leg = route.Legs.First();

            var totalDistance = route.Legs.Sum(l => l.Distance.Value);
            var totalDuration = route.Legs.Sum(l => l.Duration.Value);

            var steps = leg.Steps.Select(step => new RouteStep
            {
                Instruction = step.HtmlInstructions,
                DistanceInMeters = step.Distance.Value,
                DurationInSeconds = step.Duration.Value,
                StartLocation = new GeocodeResult
                {
                    Latitude = step.StartLocation.Latitude,
                    Longitude = step.StartLocation.Longitude,
                    FormattedAddress = ""
                },
                EndLocation = new GeocodeResult
                {
                    Latitude = step.EndLocation.Latitude,
                    Longitude = step.EndLocation.Longitude,
                    FormattedAddress = ""
                }
            }).ToList();

            return new RouteResult
            {
                TotalDistanceInMeters = totalDistance,
                TotalDistanceInMiles = totalDistance * 0.000621371,
                TotalDurationInSeconds = totalDuration,
                TotalDurationInMinutes = totalDuration / 60,
                Steps = steps,
                OverviewPolyline = route.OverviewPath?.Points ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating optimized route");
            return null;
        }
    }

    public async Task<List<string>> AutocompleteAddressAsync(string input, string? sessionToken = null)
    {
        try
        {
            var request = new PlacesAutoCompleteRequest
            {
                Key = _apiKey,
                Input = input,
                SessionToken = sessionToken
            };

            var response = await GooglePlaces.AutoComplete.QueryAsync(request);

            if (response.Status != Status.Ok)
            {
                _logger.LogWarning("Autocomplete failed for input: {Input}. Status: {Status}",
                    input, response.Status);
                return new List<string>();
            }

            return response.Predictions.Select(p => p.Description).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error autocompleting address: {Input}", input);
            return new List<string>();
        }
    }
}
