# Troubleshooting Distance Calculator

## Error: "Could not calculate distance. Please check your addresses."

This error means the Google Maps API call is failing. Here's how to fix it:

### Step 1: Verify Backend is Running

1. Open a terminal and run:
```bash
cd BeC.OpenId.Connect
dotnet run
```

2. You should see:
```
Now listening on: https://localhost:7172
```

3. Keep this terminal open and running

### Step 2: Check Backend Logs

When you try to calculate distance, look at the backend terminal. You should see logs like:
```
info: BeC.OpenId.Connect.Features.Maps.Controllers.MapsController[0]
      Calculating distance from 'London, UK' to 'Manchester, UK'
```

**If you see errors**, they will tell you exactly what's wrong.

### Step 3: Test API Directly

Open your browser and try this URL:
```
https://localhost:7172/api/Maps/distance?origin=London,UK&destination=Manchester,UK
```

You should see JSON response with distance data. If not, check the error message.

### Step 4: Verify Google Maps API Key

Your API key needs these APIs enabled:

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project
3. Go to **APIs & Services** → **Library**
4. Enable these APIs:
   - ✅ **Distance Matrix API**
   - ✅ **Places API** (for autocomplete)
   - ✅ **Geocoding API** (for coordinates)

5. Go to **APIs & Services** → **Credentials**
6. Click on your API key
7. Under **API restrictions**, ensure the above APIs are allowed

### Step 5: Check API Key Restrictions

If your API key has restrictions:

1. **Application restrictions**: Set to "None" for testing
2. **API restrictions**: Ensure Distance Matrix API, Places API, and Geocoding API are allowed
3. **Website restrictions**: If set, add `localhost` to allowed domains

### Step 6: Test Google Maps API Directly

Test your API key directly with curl:

```bash
# Replace YOUR_API_KEY with your actual key from appsettings.json
curl "https://maps.googleapis.com/maps/api/distancematrix/json?origins=London,UK&destinations=Manchester,UK&key=YOUR_API_KEY"
```

**Expected response:**
```json
{
   "destination_addresses" : [ "Manchester, UK" ],
   "origin_addresses" : [ "London, UK" ],
   "rows" : [ ... ],
   "status" : "OK"
}
```

**If you see an error:**
- `REQUEST_DENIED` = API key invalid or API not enabled
- `OVER_QUERY_LIMIT` = You've exceeded your quota
- `INVALID_REQUEST` = Check request format

### Step 7: Common Issues & Solutions

#### Issue: "API key not valid"
**Solution:**
1. Copy the API key from Google Cloud Console
2. Paste it exactly in `appsettings.json` under `GoogleMaps.ApiKey`
3. Restart the backend

#### Issue: "REQUEST_DENIED"
**Solution:**
1. Enable Distance Matrix API in Google Cloud Console
2. Check API key restrictions
3. Wait 1-2 minutes for changes to propagate

#### Issue: "CORS error" in browser console
**Solution:** Backend CORS is already configured for `http://localhost:4200`

#### Issue: "Connection refused"
**Solution:** Backend is not running. Start it with `dotnet run`

### Step 8: Check Frontend Console

Open browser DevTools (F12) and go to **Console** tab. You should see:
```
Distance calculated: Object { distanceInMiles: 200.5, ... }
```

If you see errors, they will point to the problem.

### Step 9: Enable Detailed Logging

To see more details in backend logs, update `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "BeC.OpenId.Connect": "Debug",
      "BeC.OpenId.Connect.Infrastructure.Maps": "Debug"
    }
  }
}
```

Restart the backend and try again.

### Quick Diagnostic Script

Run this in your terminal to check everything:

```bash
# Check if backend is running
curl -k https://localhost:7172/api/Maps/distance?origin=London,UK&destination=Manchester,UK

# If you get HTML instead of JSON, backend might be redirecting
# Try the Swagger UI instead:
open https://localhost:7172/swagger
```

### Still Not Working?

1. **Check the exact error message** in the backend console
2. **Check browser console** (F12) for network errors
3. **Verify API key** works with curl command above
4. **Try with simple addresses** like "London, UK" first
5. **Check API quotas** in Google Cloud Console

### Working Example

If everything is set up correctly, this should work:

1. **Pickup:** London, UK
2. **Delivery:** Manchester, UK
3. **Expected result:**
   - Distance: ~200 miles
   - Duration: ~3.5 hours
   - Price: ~$500

### API Key Setup Checklist

- [ ] API key copied from Google Cloud Console
- [ ] API key pasted in `appsettings.json`
- [ ] Distance Matrix API enabled
- [ ] Places API enabled
- [ ] Geocoding API enabled
- [ ] API restrictions allow these APIs
- [ ] Backend restarted after changes
- [ ] Backend running on port 7172
- [ ] Frontend running on port 4200

### Need a New API Key?

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project (or select existing)
3. Go to **APIs & Services** → **Credentials**
4. Click **+ CREATE CREDENTIALS** → **API key**
5. Copy the key
6. Click on the key to edit it
7. Enable the required APIs (Distance Matrix, Places, Geocoding)
8. Click **Save**
9. Paste in `appsettings.json`
10. Restart backend

---

## Quick Test Command

Test the API key directly:
```bash
# Replace YOUR_API_KEY with your actual key
curl "https://maps.googleapis.com/maps/api/distancematrix/json?origins=New+York&destinations=Los+Angeles&key=AIzaSyASSq8GZ60dxKwM2bhkCEShx1qWSfL3gIE"
```

If this returns `"status": "OK"`, your API key works and the issue is elsewhere.
