# Google Maps API Monitoring Guide

## Quick Links

- **Dashboard**: https://console.cloud.google.com/apis/dashboard
- **Billing**: https://console.cloud.google.com/billing
- **Quotas**: https://console.cloud.google.com/apis/api/distance-matrix-backend.googleapis.com/quotas

## Check Current Usage

### Option 1: Google Cloud Console (Web)

1. Visit: https://console.cloud.google.com/apis/dashboard
2. Click on each API:
   - Distance Matrix API
   - Places API
   - Geocoding API
3. View usage graphs and metrics

### Option 2: Enable Detailed Monitoring

1. Go to **APIs & Services** → **Credentials**
2. Click on your API key
3. Scroll down to **API restrictions**
4. Click **Metrics** tab to see detailed usage

## Cost Breakdown

### Your Current Setup

**APIs Enabled:**
- ✅ Distance Matrix API
- ✅ Places API (Autocomplete)
- ✅ Geocoding API

**Monthly Free Credit:** $200
**Estimated Usage (Development):**
- ~100 distance calculations/day = ~3,000/month
- ~200 autocomplete requests/day = ~6,000/month
- ~50 geocoding requests/day = ~1,500/month

**Estimated Cost:** ~$25-30/month (well within free tier)

### Pricing Reference

| API | Price | Your Usage | Estimated Cost |
|-----|-------|-----------|----------------|
| Distance Matrix | $5 per 1,000 | 3,000/month | $15/month |
| Places Autocomplete | $2.83 per 1,000 (session) | 6,000/month | $17/month |
| Geocoding | $5 per 1,000 | 1,500/month | $7.50/month |
| **Total** | | | **$39.50/month** |

**With $200 free credit:** You pay $0 for 5+ months!

## Set Up Alerts

### Budget Alert Setup

1. Go to: https://console.cloud.google.com/billing/budgets
2. Click **CREATE BUDGET**
3. Configure:
   - **Name**: "Google Maps API Monthly Budget"
   - **Budget Amount**: $50
   - **Alert Thresholds**: 50%, 75%, 90%, 100%
   - **Email**: Your email address
4. Click **FINISH**

### API Quota Alerts

1. Go to: https://console.cloud.google.com/apis/dashboard
2. Click on **Distance Matrix API**
3. Click **Quotas**
4. Set up notifications for quota usage

## Optimize Usage

### Backend Caching (Recommended)

Add response caching to reduce API calls:

```csharp
// Cache distance calculations for 24 hours
// Same route = same distance (unless traffic patterns considered)
```

### Session Tokens for Autocomplete

Your implementation already uses session tokens! This saves ~60% on autocomplete costs.

```typescript
// Good: Uses session tokens
mapsService.autocompleteAddress(input, sessionToken);
```

### Debouncing

Your autocomplete is already debounced (300ms), which reduces unnecessary API calls.

## Monthly Usage Dashboard

Create a simple SQL query to track your usage:

```sql
-- Distance calculations this month
SELECT
    COUNT(*) as DistanceCalculations,
    MONTH(CreatedAt) as Month
FROM Jobs
WHERE CreatedAt >= DATEADD(month, -1, GETDATE())
GROUP BY MONTH(CreatedAt);
```

## Cost Projection

### Development (Current)
- Users: 5-10/day
- Jobs: 10-20/day
- API Calls: ~200/day
- **Monthly Cost**: $0 (within free tier)

### Production (Estimated)
- Users: 100-200/day
- Jobs: 50-100/day
- API Calls: ~1,000/day (~30,000/month)
- **Monthly Cost**: ~$150-200

### Optimization Tips

1. **Cache Popular Routes**: Store frequently used routes in DB
2. **Batch Requests**: Group multiple distance calculations
3. **Use Waypoints**: Calculate multi-stop routes in one request
4. **Implement Rate Limiting**: Prevent abuse
5. **Session Tokens**: Always use for autocomplete (already implemented!)

## Monitoring Script

Run this weekly to check your usage:

```bash
# View this month's usage in Cloud Console
open "https://console.cloud.google.com/apis/dashboard"

# Or use gcloud CLI (if installed)
gcloud services list --enabled --project=YOUR_PROJECT_ID
```

## Troubleshooting High Usage

If you see unexpected high usage:

1. **Check Logs**: Look for repeated API calls
2. **Review Code**: Ensure no infinite loops
3. **Check Frontend**: Browser refreshes shouldn't trigger API calls
4. **Enable Request Logging**: Track each API call
5. **Set Daily Quotas**: Limit max requests per day

## Support

- **Google Maps Support**: https://developers.google.com/maps/support
- **Billing Issues**: https://console.cloud.google.com/support
- **Community**: https://stackoverflow.com/questions/tagged/google-maps-api

---

**Last Updated**: December 9, 2025
**Current Free Credit**: $200/month
**Estimated Monthly Usage**: $25-40 (within free tier)
