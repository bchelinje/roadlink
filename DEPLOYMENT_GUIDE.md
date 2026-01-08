# 🚀 Deployment Guide - BeC OpenID Connect

This guide will help you deploy your full-stack application using **Railway** (Backend + Database) and **Vercel** (Frontend).

## 📋 Prerequisites

- GitHub account
- Railway account (sign up at [railway.app](https://railway.app))
- Vercel account (sign up at [vercel.com](https://vercel.com))
- Your code pushed to a GitHub repository

---

## 🛤️ Part 1: Deploy Backend to Railway

### Step 1: Create Railway Project

1. Go to [Railway Dashboard](https://railway.app/dashboard)
2. Click **"New Project"**
3. Select **"Deploy from GitHub repo"**
4. Choose your `BeC.OpenId.Connect` repository
5. Railway will detect your Dockerfile automatically

### Step 2: Add PostgreSQL Database

1. In your Railway project, click **"+ New"**
2. Select **"Database"** → **"PostgreSQL"**
3. Railway will provision a PostgreSQL database
4. Wait for the database to deploy (usually 1-2 minutes)

### Step 3: Configure Environment Variables

1. Click on your **backend service** (not the database)
2. Go to **"Variables"** tab
3. Click **"+ New Variable"** and add the following:

```bash
# Database Connection
# Railway automatically provides DATABASE_URL, but we need to format it for .NET
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${{PORT}}

# JWT Settings (IMPORTANT: Generate a secure random string)
Jwt__Secret=YOUR_SUPER_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG
Jwt__Issuer=https://your-app-name.up.railway.app
Jwt__Audience=https://your-app-name.up.railway.app
Jwt__ExpiryMinutes=60

# Google Maps API
GoogleMaps__ApiKey=your_google_maps_api_key

# CORS - Add your Vercel frontend URL (you'll update this after deploying frontend)
AllowedOrigins__0=https://your-vercel-app.vercel.app

# Stripe (if using payments)
Stripe__SecretKey=your_stripe_secret_key
Stripe__PublishableKey=your_stripe_publishable_key
```

### Step 4: Get Your Railway URL

1. Go to **"Settings"** tab
2. Scroll to **"Domains"**
3. Click **"Generate Domain"**
4. Copy your Railway URL (e.g., `https://your-app.up.railway.app`)
5. **Save this URL** - you'll need it for frontend configuration

### Step 5: Run Database Migrations

Railway should automatically run your migrations on deployment. If not:

1. Go to **"Deployments"** tab
2. Wait for the build to complete
3. Check logs for any migration errors

---

## 🌐 Part 2: Deploy Frontend to Vercel

### Step 1: Create Vercel Project

1. Go to [Vercel Dashboard](https://vercel.com/dashboard)
2. Click **"Add New Project"**
3. Import your GitHub repository
4. Select the repository
5. Configure as follows:
   - **Framework Preset**: Other
   - **Root Directory**: `frontend`
   - **Build Command**: `npm run build:vercel`
   - **Output Directory**: `dist/bec-admin-dashboard/browser`

### Step 2: Configure Environment Variables

Before deploying, add these environment variables in Vercel:

1. Click **"Environment Variables"** during setup (or later in Settings)
2. Add the following variables:

```bash
# Your Railway Backend URL
RAILWAY_API_URL=https://your-app.up.railway.app
API_BASE_URL=https://your-app.up.railway.app

# Vercel will automatically provide VERCEL_URL
# No need to set it manually
```

### Step 3: Deploy

1. Click **"Deploy"**
2. Vercel will build and deploy your frontend
3. Wait for deployment to complete (2-5 minutes)
4. Copy your Vercel URL (e.g., `https://your-app.vercel.app`)

### Step 4: Update Backend CORS

Now that you have your Vercel URL, go back to Railway:

1. Open your Railway backend service
2. Go to **"Variables"**
3. Update `AllowedOrigins__0` to your Vercel URL:
   ```
   AllowedOrigins__0=https://your-app.vercel.app
   ```
4. Click **"Deploy"** to restart with new settings

---

## 🔧 Part 3: Final Configuration

### Update Frontend Environment URLs

The `replace-env.js` script will automatically replace the URLs during build using Vercel's environment variables. Make sure these are set:

- `RAILWAY_API_URL`: Your Railway backend URL
- `VERCEL_URL`: Automatically provided by Vercel

### Test Your Deployment

1. Open your Vercel URL in a browser
2. Try to register a new account
3. Test login functionality
4. Check browser console for any CORS errors
5. Verify API calls are hitting your Railway backend

---

## 🔐 Security Checklist

- [ ] Change `Jwt__Secret` to a strong random string (at least 32 characters)
- [ ] Enable HTTPS only (both platforms provide this automatically)
- [ ] Set up proper CORS origins (only your Vercel domain)
- [ ] Never commit `.env` files with secrets
- [ ] Use Railway's secret management for sensitive data
- [ ] Enable 2FA on both Railway and Vercel accounts
- [ ] Set up Google Maps API key restrictions (HTTP referrer for frontend, IP for backend)

---

## 📊 Monitoring & Logs

### Railway Logs
1. Go to your Railway project
2. Click on your service
3. View **"Deployments"** → Click on latest deployment → **"View Logs"**

### Vercel Logs
1. Go to your Vercel project
2. Click **"Deployments"** → Select deployment → **"View Function Logs"**

---

## 🔄 Continuous Deployment

Both platforms are now set up for automatic deployments:

- **Push to `main` branch** → Railway automatically rebuilds backend
- **Push to `main` branch** → Vercel automatically rebuilds frontend

You can also set up deployment from specific branches in each platform's settings.

---

## 💰 Cost Estimate

### Railway Free Tier
- $5 credit per month (resets monthly)
- Enough for small demos and testing
- Upgrade to Developer plan ($5/month) for production

### Vercel Free Tier
- Unlimited sites
- 100GB bandwidth per month
- Free for personal projects and demos

**Total Monthly Cost for Demo**: **FREE** (with Railway's monthly credit)

---

## 🐛 Troubleshooting

### Backend not connecting to database
- Check Railway logs for connection errors
- Verify `ConnectionStrings__DefaultConnection` is properly set
- Make sure PostgreSQL service is running

### Frontend shows CORS errors
- Verify `AllowedOrigins__0` in Railway matches your Vercel URL exactly
- Check Railway logs to see if requests are being blocked
- Ensure no trailing slashes in URLs

### 404 on frontend routes
- Vercel should handle SPA routing automatically via `vercel.json`
- Check that `outputDirectory` is correct: `dist/bec-admin-dashboard/browser`

### Authentication not working
- Verify all JWT settings in Railway
- Check `Jwt__Issuer` and `Jwt__Audience` match your Railway URL
- Ensure `Jwt__Secret` is set and matches

### Environment variables not updating
- Railway: Variables update on next deployment - click "Deploy" to restart
- Vercel: Need to redeploy after changing variables

---

## 📝 Environment Variables Quick Reference

### Railway (Backend)
```bash
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${{PORT}}
Jwt__Secret=<generate-secure-random-string-32+chars>
Jwt__Issuer=https://<your-railway-app>.up.railway.app
Jwt__Audience=https://<your-railway-app>.up.railway.app
Jwt__ExpiryMinutes=60
GoogleMaps__ApiKey=<your-key>
AllowedOrigins__0=https://<your-vercel-app>.vercel.app
Stripe__SecretKey=<your-stripe-key>
Stripe__PublishableKey=<your-stripe-key>
```

### Vercel (Frontend)
```bash
RAILWAY_API_URL=https://<your-railway-app>.up.railway.app
API_BASE_URL=https://<your-railway-app>.up.railway.app
```

---

## 🎉 Success!

Your application should now be live!

- **Frontend**: `https://your-app.vercel.app`
- **Backend**: `https://your-app.up.railway.app`
- **Swagger/API Docs**: `https://your-app.up.railway.app/swagger`

---

## 📞 Need Help?

- Railway docs: https://docs.railway.app
- Vercel docs: https://vercel.com/docs
- Railway community: https://discord.gg/railway
- Vercel community: https://github.com/vercel/vercel/discussions

---

## 🚀 Next Steps

1. Set up custom domains (optional)
2. Configure email service for notifications
3. Set up monitoring and alerts
4. Implement backup strategy for database
5. Set up staging environment
