# ⚡ Quick Start - Deploy in 15 Minutes

Get your app live in minutes with this streamlined guide.

## 🎯 What You'll Deploy

- **Backend**: .NET API + PostgreSQL on Railway
- **Frontend**: Angular app on Vercel
- **Cost**: FREE (Railway $5 monthly credit + Vercel free tier)

---

## 📦 Step 1: Push to GitHub (If not already)

```bash
git add .
git commit -m "Ready for deployment"
git push origin main
```

---

## 🚂 Step 2: Railway Backend (5 minutes)

### A. Create Project
1. Go to [railway.app](https://railway.app) → Sign in with GitHub
2. **New Project** → **Deploy from GitHub repo**
3. Select your repository

### B. Add Database
1. In project: **+ New** → **Database** → **PostgreSQL**
2. Wait for database to start

### C. Configure Backend
1. Click your **backend service** (not database)
2. **Variables** tab → Add these:

```bash
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${{PORT}}
Jwt__Secret=CHANGE_THIS_TO_A_SECURE_RANDOM_STRING_AT_LEAST_32_CHARS
Jwt__Issuer=https://YOUR_APP_NAME.up.railway.app
Jwt__Audience=https://YOUR_APP_NAME.up.railway.app
Jwt__ExpiryMinutes=60
GoogleMaps__ApiKey=YOUR_GOOGLE_MAPS_API_KEY
AllowedOrigins__0=*
```

### D. Get Backend URL
1. **Settings** → **Domains** → **Generate Domain**
2. **Copy URL**: `https://your-app.up.railway.app`
3. ✅ Save this URL!

---

## ▲ Step 3: Vercel Frontend (5 minutes)

### A. Create Project
1. Go to [vercel.com](https://vercel.com) → Sign in with GitHub
2. **Add New Project** → Import your GitHub repo

### B. Configure Build
- **Root Directory**: `frontend`
- **Build Command**: `npm run build:vercel`
- **Output Directory**: `dist/bec-admin-dashboard/browser`

### C. Add Environment Variables
```bash
RAILWAY_API_URL=https://your-railway-app.up.railway.app
API_BASE_URL=https://your-railway-app.up.railway.app
```

### D. Deploy
1. Click **Deploy**
2. Wait 2-3 minutes
3. **Copy URL**: `https://your-app.vercel.app`
4. ✅ Save this URL!

---

## 🔄 Step 4: Update CORS (2 minutes)

1. Go back to **Railway**
2. Click backend service → **Variables**
3. Find `AllowedOrigins__0`
4. Change from `*` to: `https://your-app.vercel.app`
5. Railway will auto-redeploy

---

## ✅ Step 5: Test It!

1. Open your Vercel URL
2. Try to register/login
3. Check browser console (should be no CORS errors)
4. Test creating a job or booking

**Swagger Docs**: `https://your-railway-app.up.railway.app/swagger`

---

## 🎉 You're Live!

**Frontend**: `https://your-app.vercel.app`
**Backend**: `https://your-app.up.railway.app`

---

## 🔧 Common Issues

### "CORS Error" in browser
- Make sure `AllowedOrigins__0` in Railway matches your Vercel URL exactly
- No trailing slashes!

### "Cannot connect to database"
- Check Railway logs: Service → Deployments → View Logs
- Verify PostgreSQL is running

### "404 on frontend routes"
- Should work automatically via `vercel.json`
- Check build output directory is correct

### "Unauthorized" errors
- Check `Jwt__Secret` is set in Railway
- Verify `Jwt__Issuer` matches Railway URL

---

## 📋 Quick Checklist

- [ ] Railway backend deployed
- [ ] PostgreSQL database running
- [ ] Railway URL saved
- [ ] Vercel frontend deployed
- [ ] Vercel URL saved
- [ ] CORS updated in Railway
- [ ] Can login successfully
- [ ] No console errors

---

## 🚀 Next Steps

1. **Custom Domain**: Add your own domain in Railway/Vercel settings
2. **Monitoring**: Set up error tracking (Sentry, LogRocket, etc.)
3. **Staging**: Create separate Railway/Vercel projects for staging
4. **Security**: Rotate JWT secret regularly, add rate limiting

---

## 💡 Pro Tips

- Railway auto-deploys on every push to `main`
- Vercel auto-deploys on every push to `main`
- Use Railway's **"View Logs"** to debug backend issues
- Use Chrome DevTools → Network tab to debug API calls
- Test with `curl` to isolate frontend vs backend issues:
  ```bash
  curl https://your-railway-app.up.railway.app/health
  ```

---

## 📞 Need Help?

See `DEPLOYMENT_GUIDE.md` for detailed instructions
See `DEPLOYMENT_CHECKLIST.md` for complete testing checklist

**Railway Support**: https://discord.gg/railway
**Vercel Support**: https://vercel.com/docs
