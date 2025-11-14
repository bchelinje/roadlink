# Google Maps & Dynamic Pricing Features

This document explains the newly added Google Maps integration and Dynamic Pricing system for the BeC Moving Services platform.

## Table of Contents
1. [Overview](#overview)
2. [Google Maps Integration](#google-maps-integration)
3. [Dynamic Pricing System](#dynamic-pricing-system)
4. [Location Tracking](#location-tracking)
5. [Configuration](#configuration)
6. [Database Setup](#database-setup)
7. [API Endpoints](#api-endpoints)
8. [Usage Examples](#usage-examples)

---

## Overview

The new features include:

### Google Maps Integration
- **Geocoding**: Convert addresses to coordinates and vice versa
- **Distance Calculation**: Calculate distance and duration between two locations
- **Route Optimization**: Get optimized routes with turn-by-turn directions
- **Address Autocomplete**: Provide address suggestions as users type

### Dynamic Pricing System
- **Configurable Pricing Rules**: Define multiple pricing rules with priorities
- **Distance-Based Pricing**: Per-mile rates and distance bands
- **Time-Based Pricing**: Per-minute rates
- **Vehicle Type Pricing**: Different rates for different vehicle types
- **Surge Pricing**: Time-based multipliers (peak hours, weekends)
- **Service Add-ons**: Helpers, packing, assembly, storage charges
- **Pricing History**: Audit trail of all price calculations

### Location Tracking
- **Real-Time Location Updates**: Drivers can update their location in real-time
- **Customer Visibility**: Customers can see driver location for their jobs
- **ETA Calculations**: Automatic calculation of estimated arrival time
- **Location History**: Track driver movement history (admin)

---

## Google Maps Integration

### Features Implemented

#### 1. Geocoding
Convert addresses to geographic coordinates (latitude/longitude).

```csharp
var result = await _mapsService.GeocodeAddressAsync("123 Main St, San Francisco, CA");
// Returns: GeocodeResult with Latitude, Longitude, FormattedAddress
```

#### 2. Reverse Geocoding
Convert coordinates to human-readable addresses.

```csharp
var address = await _mapsService.ReverseGeocodeAsync(37.7749, -122.4194);
// Returns: "123 Main St, San Francisco, CA 94102, USA"
```

#### 3. Distance Calculation
Calculate distance and duration between two locations.

```csharp
var distance = await _mapsService.CalculateDistanceAsync(
    "123 Main St, San Francisco, CA",
    "456 Market St, San Francisco, CA");

// Returns: DistanceResult
// {
//   DistanceInMiles: 2.3,
//   DurationInMinutes: 8,
//   DistanceText: "2.3 miles",
//   DurationText: "8 mins"
// }
```

#### 4. Route Optimization
Get optimized routes with multiple waypoints.

```csharp
var route = await _mapsService.GetOptimizedRouteAsync(
    origin: "123 Main St, SF",
    destination: "789 Pine St, SF",
    waypoints: new List<string> { "456 Market St, SF", "234 Oak St, SF" });

// Returns: RouteResult with turn-by-turn directions
```

#### 5. Address Autocomplete
Provide address suggestions for user input.

```csharp
var suggestions = await _mapsService.AutocompleteAddressAsync("123 Main");
// Returns: List of address suggestions
```

### Google Maps Service Interface

```csharp
public interface IGoogleMapsService
{
    Task<GeocodeResult?> GeocodeAddressAsync(string address);
    Task<string?> ReverseGeocodeAsync(double latitude, double longitude);
    Task<DistanceResult?> CalculateDistanceAsync(string origin, string destination);
    Task<DistanceResult?> CalculateDistanceAsync(double originLat, double originLng, double destLat, double destLng);
    Task<RouteResult?> GetOptimizedRouteAsync(string origin, string destination, List<string>? waypoints = null);
    Task<List<string>> AutocompleteAddressAsync(string input, string? sessionToken = null);
}
```

---

## Dynamic Pricing System

### Pricing Rule Types

The system supports 7 types of pricing rules:

#### 1. Base Fare
Fixed starting fee for all jobs.

```json
{
  "name": "Standard Base Fare",
  "type": "base_fare",
  "fixedAmount": 15.00,
  "priority": 10
}
```

#### 2. Per-Mile Rate
Charge per mile traveled.

```json
{
  "name": "Standard Per-Mile Rate",
  "type": "per_mile",
  "perMileRate": 2.50,
  "vehicleType": "van",
  "priority": 20
}
```

#### 3. Per-Minute Rate
Charge per minute of estimated duration.

```json
{
  "name": "Time Charge",
  "type": "per_minute",
  "perMinuteRate": 0.50,
  "priority": 30
}
```

#### 4. Distance Bands
Fixed charges for distance ranges.

```json
{
  "name": "Long Distance Fee",
  "type": "distance_band",
  "minDistance": 20.0,
  "maxDistance": 50.0,
  "fixedAmount": 25.00,
  "priority": 25
}
```

#### 5. Vehicle Type Charges
Additional fees based on vehicle type.

```json
{
  "name": "Large Truck Fee",
  "type": "vehicle_type",
  "vehicleType": "large_truck",
  "fixedAmount": 50.00,
  "priority": 40
}
```

#### 6. Time Multipliers (Surge Pricing)
Percentage increase during peak times.

```json
{
  "name": "Weekend Surge",
  "type": "time_multiplier",
  "multiplierPercentage": 1.5,
  "weekendOnly": true,
  "priority": 80
}
```

Peak hours example:
```json
{
  "name": "Rush Hour Surge",
  "type": "time_multiplier",
  "multiplierPercentage": 1.3,
  "startTime": "07:00:00",
  "endTime": "09:00:00",
  "weekdayOnly": true,
  "priority": 85
}
```

#### 7. Service Add-ons
Charges for additional services.

```json
{
  "name": "Helper Fee",
  "type": "service_addon",
  "serviceAddonType": "helpers",
  "fixedAmount": 25.00,
  "priority": 60
}
```

### Pricing Calculation Algorithm

The system calculates prices in the following order:

1. **Get Distance & Duration** - Uses Google Maps API if not provided
2. **Load Active Rules** - Fetches all active pricing rules ordered by priority
3. **Base Fare** - Applies base fare rule
4. **Distance Charges** - Applies per-mile rates and distance band fees
5. **Time Charges** - Applies per-minute rates
6. **Vehicle Charges** - Applies vehicle-specific fees
7. **Service Add-ons** - Applies helper, packing, assembly fees
8. **Calculate Subtotal** - Sums all charges
9. **Apply Surge** - Applies time-based multipliers (takes maximum if multiple apply)
10. **Platform Fee** - Adds 10% platform fee
11. **Calculate Total** - Final price with detailed breakdown

### Pricing Result Breakdown

```json
{
  "baseFare": 15.00,
  "distanceCharge": 25.00,
  "timeCharge": 15.00,
  "vehicleTypeCharge": 50.00,
  "serviceAddonsCharge": 25.00,
  "surgeMultiplier": 1.3,
  "subTotal": 169.00,
  "platformFee": 16.90,
  "totalPrice": 185.90,
  "breakdown": [
    {
      "description": "Base Fare",
      "amount": 15.00
    },
    {
      "description": "Distance (10.00 miles @ $2.50/mile)",
      "amount": 25.00,
      "details": "van"
    },
    {
      "description": "Time (30 min @ $0.5000/min)",
      "amount": 15.00
    },
    {
      "description": "Vehicle Type - large_truck",
      "amount": 50.00
    },
    {
      "description": "Service Add-on: helpers",
      "amount": 25.00,
      "details": "1 helpers"
    },
    {
      "description": "Peak Time Multiplier (Rush Hour Surge)",
      "amount": 39.00,
      "details": "30% surge"
    },
    {
      "description": "Platform Fee (10%)",
      "amount": 16.90
    }
  ],
  "distanceInMiles": 10.0,
  "estimatedDurationMinutes": 30
}
```

---

## Location Tracking

### Driver Location Updates

Drivers can update their location in real-time:

```http
POST /api/location/drivers/me/location
Authorization: Bearer {driver_jwt_token}
Content-Type: application/json

{
  "latitude": 37.7749,
  "longitude": -122.4194,
  "accuracy": 10.0,
  "speed": 15.5,
  "heading": 90.0,
  "currentJobId": "job-guid-here"
}
```

**Features:**
- Automatic reverse geocoding to get address
- Real-time notification to customer if on a job
- Stores location history for analytics

### Customer View Driver Location

Customers can see their driver's location:

```http
GET /api/location/jobs/{jobId}/driver-location
Authorization: Bearer {customer_jwt_token}
```

**Response:**
```json
{
  "driverId": "driver-guid",
  "driverName": "John Smith",
  "latitude": 37.7749,
  "longitude": -122.4194,
  "speed": 15.5,
  "heading": 90.0,
  "address": "123 Market St, San Francisco, CA",
  "timestamp": "2025-01-15T10:30:00Z",
  "ageInSeconds": 5
}
```

### ETA Calculation

Calculate estimated time of arrival:

```http
GET /api/location/jobs/{jobId}/eta
Authorization: Bearer {customer_or_driver_jwt_token}
```

**Response:**
```json
{
  "jobId": "job-guid",
  "driverId": "driver-guid",
  "driverName": "John Smith",
  "currentLatitude": 37.7749,
  "currentLongitude": -122.4194,
  "destinationLatitude": 37.7849,
  "destinationLongitude": -122.4094,
  "destinationAddress": "456 Mission St, San Francisco, CA",
  "distanceInMiles": 2.3,
  "durationInMinutes": 8,
  "estimatedArrivalTime": "2025-01-15T10:38:00Z",
  "durationText": "8 mins",
  "distanceText": "2.3 mi"
}
```

---

## Configuration

### 1. Google Maps API Key

Get your API key from [Google Cloud Console](https://console.cloud.google.com/):

1. Create a new project
2. Enable these APIs:
   - Geocoding API
   - Distance Matrix API
   - Directions API
   - Places API
3. Create credentials (API Key)
4. Restrict the key (optional but recommended)

### 2. Update appsettings.json

```json
{
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY_HERE"
  }
}
```

### 3. Environment Variables (Production)

For production, use environment variables instead:

```bash
export GoogleMaps__ApiKey="your-production-api-key"
```

Or in Azure App Service, set:
- Key: `GoogleMaps:ApiKey`
- Value: `your-production-api-key`

---

## Database Setup

### Create Migration

Run the following command to create the database migration:

```bash
dotnet ef migrations add AddGoogleMapsAndPricing --project BeC.OpenId.Connect
```

### Apply Migration

Update the database:

```bash
dotnet ef database update --project BeC.OpenId.Connect
```

### New Tables Created

1. **PricingRules** - Stores all pricing rules
2. **PricingHistory** - Audit trail of price calculations
3. **DriverLocations** - Real-time location tracking history

### Seed Default Pricing Rules

After migration, seed some default rules:

```sql
-- Base Fare
INSERT INTO PricingRules (Id, Name, Type, FixedAmount, Priority, IsActive, CreatedAt, UpdatedAt)
VALUES (NEWID(), 'Standard Base Fare', 'base_fare', 15.00, 10, 1, GETUTCDATE(), GETUTCDATE());

-- Per-Mile Rate
INSERT INTO PricingRules (Id, Name, Type, PerMileRate, Priority, IsActive, CreatedAt, UpdatedAt)
VALUES (NEWID(), 'Standard Per-Mile', 'per_mile', 2.50, 20, 1, GETUTCDATE(), GETUTCDATE());

-- Per-Minute Rate
INSERT INTO PricingRules (Id, Name, Type, PerMinuteRate, Priority, IsActive, CreatedAt, UpdatedAt)
VALUES (NEWID(), 'Time Charge', 'per_minute', 0.50, 30, 1, GETUTCDATE(), GETUTCDATE());

-- Weekend Surge
INSERT INTO PricingRules (Id, Name, Type, MultiplierPercentage, WeekendOnly, Priority, IsActive, CreatedAt, UpdatedAt)
VALUES (NEWID(), 'Weekend Surge', 'time_multiplier', 1.3, 1, 80, 1, GETUTCDATE(), GETUTCDATE());
```

---

## API Endpoints

### Pricing Endpoints

#### Calculate Full Price
```http
POST /api/pricing/calculate
Authorization: Bearer {token}
Content-Type: application/json

{
  "pickupAddress": "123 Main St, San Francisco, CA",
  "deliveryAddress": "456 Market St, San Francisco, CA",
  "vehicleType": "van",
  "scheduledDate": "2025-01-20T09:00:00Z",
  "serviceAddons": ["helpers", "packing"],
  "numberOfHelpers": 2
}
```

#### Get Quick Estimate
```http
GET /api/pricing/estimate?distanceInMiles=10&vehicleType=van
Authorization: Bearer {token}
```

#### Get Surge Multiplier
```http
GET /api/pricing/surge?scheduledDate=2025-01-20T08:00:00Z&pickupAddress=San+Francisco
Authorization: Bearer {token}
```

#### Get Pricing Rules (Admin)
```http
GET /api/pricing/rules?isActive=true
Authorization: Bearer {admin_token}
```

#### Create Pricing Rule (Admin)
```http
POST /api/pricing/rules
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "name": "Large Truck Fee",
  "description": "Additional fee for large trucks",
  "type": "vehicle_type",
  "vehicleType": "large_truck",
  "fixedAmount": 50.00,
  "priority": 40,
  "isActive": true
}
```

#### Update Pricing Rule (Admin)
```http
PUT /api/pricing/rules/{id}
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "name": "Updated Rule Name",
  "fixedAmount": 60.00,
  ...
}
```

#### Toggle Rule Active Status (Admin)
```http
PATCH /api/pricing/rules/{id}/toggle
Authorization: Bearer {admin_token}
```

#### Delete Pricing Rule (Admin)
```http
DELETE /api/pricing/rules/{id}
Authorization: Bearer {admin_token}
```

### Location Tracking Endpoints

#### Update Driver Location
```http
POST /api/location/drivers/me/location
Authorization: Bearer {driver_token}
Content-Type: application/json

{
  "latitude": 37.7749,
  "longitude": -122.4194,
  "currentJobId": "job-guid-optional"
}
```

#### Get Driver Location for Job
```http
GET /api/location/jobs/{jobId}/driver-location
Authorization: Bearer {customer_or_driver_token}
```

#### Calculate ETA
```http
GET /api/location/jobs/{jobId}/eta
Authorization: Bearer {customer_or_driver_token}
```

#### Get Driver Location History (Admin)
```http
GET /api/location/drivers/{driverId}/history?startDate=2025-01-01&endDate=2025-01-31
Authorization: Bearer {admin_token}
```

#### Get Active Drivers (Admin)
```http
GET /api/location/drivers/active
Authorization: Bearer {admin_token}
```

---

## Usage Examples

### Example 1: Create Job with Dynamic Pricing

```csharp
[HttpPost("jobs")]
public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
{
    // 1. Calculate price using pricing service
    var pricingRequest = new PricingCalculationRequest
    {
        PickupAddress = request.PickupAddress,
        DeliveryAddress = request.DeliveryAddress,
        VehicleType = request.VehicleType,
        ScheduledDate = request.ScheduledDate,
        ServiceAddons = request.ServiceAddons,
        NumberOfHelpers = request.NumberOfHelpers
    };

    var priceResult = await _pricingService.CalculatePriceAsync(pricingRequest);

    // 2. Create job with calculated price
    var job = new Job
    {
        JobNumber = GenerateJobNumber(),
        PickupAddress = request.PickupAddress,
        DeliveryAddress = request.DeliveryAddress,
        VehicleType = request.VehicleType,
        ScheduledDate = request.ScheduledDate,
        EstimatedPrice = priceResult.TotalPrice,
        Distance = priceResult.DistanceInMiles,
        EstimatedDuration = priceResult.EstimatedDurationMinutes,
        CustomerId = currentCustomer.Id,
        Status = "pending"
    };

    _context.Jobs.Add(job);
    await _context.SaveChangesAsync();

    // 3. Save pricing history
    await _pricingService.SavePricingHistoryAsync(
        pricingRequest,
        priceResult,
        jobId: job.Id,
        customerId: currentCustomer.Id);

    // 4. Send notifications
    await _notificationService.SendToRoleAsync("Driver", "job_created", new
    {
        jobId = job.Id,
        pickupAddress = job.PickupAddress,
        estimatedPrice = priceResult.TotalPrice
    });

    return Ok(new
    {
        job,
        pricing = priceResult
    });
}
```

### Example 2: Real-Time Driver Tracking

```csharp
// Mobile app sends location every 10 seconds
setInterval(async () => {
    const position = await Geolocation.getCurrentPosition();

    await fetch('/api/location/drivers/me/location', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
            speed: position.coords.speed,
            heading: position.coords.heading,
            currentJobId: currentJob?.id
        })
    });
}, 10000);
```

### Example 3: Show ETA to Customer

```typescript
// Angular component
async updateETA() {
    const response = await this.http.get<EtaCalculationResult>(
        `/api/location/jobs/${this.jobId}/eta`
    ).toPromise();

    this.eta = response.estimatedArrivalTime;
    this.distance = response.distanceText;
    this.duration = response.durationText;

    // Update UI
    this.showOnMap(response.currentLatitude, response.currentLongitude);
}
```

### Example 4: Admin Dashboard - Manage Pricing Rules

```typescript
// Create new surge pricing for holidays
createHolidaySurge() {
    const rule: PricingRule = {
        name: "Holiday Surge",
        description: "20% surge during holidays",
        type: "time_multiplier",
        multiplierPercentage: 1.2,
        priority: 90,
        isActive: true
    };

    this.http.post('/api/pricing/rules', rule).subscribe(
        response => console.log('Rule created', response)
    );
}
```

---

## Testing

### Test Pricing Calculation

```bash
curl -X POST https://your-api.com/api/pricing/calculate \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pickupAddress": "1 Market St, San Francisco, CA",
    "deliveryAddress": "100 Van Ness Ave, San Francisco, CA",
    "vehicleType": "van",
    "scheduledDate": "2025-01-20T09:00:00Z",
    "serviceAddons": ["helpers"],
    "numberOfHelpers": 1
  }'
```

### Test Location Update

```bash
curl -X POST https://your-api.com/api/location/drivers/me/location \
  -H "Authorization: Bearer DRIVER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "latitude": 37.7749,
    "longitude": -122.4194
  }'
```

---

## Performance Considerations

### Google Maps API
- **Caching**: Consider caching geocoding results for common addresses
- **Rate Limits**: Google Maps has usage limits - monitor in Cloud Console
- **Optimization**: Use Distance Matrix API for batch calculations

### Location Tracking
- **Update Frequency**: 10-30 seconds recommended for real-time tracking
- **Database Growth**: DriverLocations table will grow quickly - consider:
  - Archiving old data (older than 30 days)
  - Implementing data retention policies
  - Using time-series database for historical data

### Pricing Calculations
- **Rule Caching**: Active rules are loaded from database each time
- **Optimization**: Consider caching active rules in memory with invalidation

---

## Security Considerations

1. **API Key Protection**
   - Never commit API keys to source control
   - Use environment variables in production
   - Implement key rotation policy

2. **Authorization**
   - All pricing admin endpoints require Admin/SuperAdmin role
   - Location updates restricted to driver role
   - ETA/location view restricted to job participants

3. **Rate Limiting**
   - Implement rate limiting on pricing calculation endpoints
   - Limit location update frequency per driver

4. **Data Privacy**
   - Location data is sensitive - implement retention policies
   - Only authorized users can view driver locations
   - Comply with GDPR/CCPA for location data

---

## Troubleshooting

### Google Maps API Errors

**Error: "API key not configured"**
- Ensure `GoogleMaps:ApiKey` is set in appsettings.json
- Check environment variables in production

**Error: "REQUEST_DENIED"**
- Enable required APIs in Google Cloud Console
- Check API key restrictions

**Error: "OVER_QUERY_LIMIT"**
- Exceeded daily quota
- Upgrade Google Maps billing account

### Pricing Calculation Issues

**No base fare applied**
- Check if base_fare rule exists and is active
- Verify Priority is set correctly

**Surge pricing not working**
- Check time and date conditions
- Verify WeekendOnly/WeekdayOnly settings
- Check StartTime/EndTime format

### Location Tracking Issues

**Location not updating**
- Check driver authorization token
- Verify latitude/longitude are valid (-90 to 90, -180 to 180)
- Check network connectivity

**ETA calculation fails**
- Ensure driver has recent location (within 5 minutes)
- Verify pickup address is valid
- Check Google Maps API is working

---

## Next Steps

Recommended enhancements:

1. **Address Autocomplete UI**: Add autocomplete to address input fields
2. **Map Visualization**: Show routes and driver locations on a map
3. **Advanced Surge Pricing**: Dynamic surge based on demand
4. **Price Optimization**: ML-based price optimization
5. **Multiple Stops**: Support for multi-stop routes
6. **Traffic Integration**: Real-time traffic data for better ETAs

---

## Support

For questions or issues:
- Check logs in `/var/logs/` or Application Insights
- Review Google Maps API usage in Cloud Console
- Contact: bchelinje@gmail.com
