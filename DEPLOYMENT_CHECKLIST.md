# ✅ Deployment Checklist

Use this checklist to ensure you've completed all deployment steps correctly.

## 🎯 Pre-Deployment

- [ ] Code is pushed to GitHub
- [ ] Railway account created
- [ ] Vercel account created
- [ ] Google Maps API key obtained
- [ ] Stripe keys obtained (if using payments)
- [ ] Strong JWT secret generated (32+ characters)

## 🛤️ Railway Setup (Backend)

- [ ] New Railway project created
- [ ] GitHub repository connected
- [ ] PostgreSQL database added to project
- [ ] All environment variables configured:
  - [ ] `ConnectionStrings__DefaultConnection`
  - [ ] `ASPNETCORE_ENVIRONMENT=Production`
  - [ ] `ASPNETCORE_URLS`
  - [ ] `Jwt__Secret`
  - [ ] `Jwt__Issuer`
  - [ ] `Jwt__Audience`
  - [ ] `GoogleMaps__ApiKey`
  - [ ] `AllowedOrigins__0` (will update after Vercel)
- [ ] Domain generated for Railway app
- [ ] Railway URL saved: `_______________________________`
- [ ] Deployment successful (check logs)
- [ ] Database migrations completed

## 🌐 Vercel Setup (Frontend)

- [ ] New Vercel project created
- [ ] GitHub repository imported
- [ ] Root directory set to `frontend`
- [ ] Build command set to `npm run build:vercel`
- [ ] Output directory set to `dist/bec-admin-dashboard/browser`
- [ ] Environment variables configured:
  - [ ] `RAILWAY_API_URL` (your Railway URL)
  - [ ] `API_BASE_URL` (your Railway URL)
- [ ] First deployment successful
- [ ] Vercel URL saved: `_______________________________`

## 🔄 Post-Deployment Configuration

- [ ] Updated Railway `AllowedOrigins__0` with Vercel URL
- [ ] Railway backend redeployed after CORS update
- [ ] Frontend successfully loads
- [ ] Can access Swagger UI: `https://your-railway-app.up.railway.app/swagger`
- [ ] API health check working: `https://your-railway-app.up.railway.app/health`

## 🧪 Testing

- [ ] Can access frontend homepage
- [ ] Can register new user
- [ ] Can login with credentials
- [ ] No CORS errors in browser console
- [ ] API calls successful (check Network tab)
- [ ] Can create/view jobs (if applicable)
- [ ] Authentication tokens working
- [ ] Google Maps integration working
- [ ] Payment processing working (if using Stripe)

## 🔐 Security

- [ ] JWT secret is strong and unique
- [ ] HTTPS enabled (automatic on both platforms)
- [ ] CORS configured correctly (only Vercel domain)
- [ ] No secrets committed to Git
- [ ] `.env` files in `.gitignore`
- [ ] Google Maps API key restricted
- [ ] Stripe keys are production keys (not test)

## 📊 Monitoring

- [ ] Can view Railway logs
- [ ] Can view Vercel logs
- [ ] No critical errors in logs
- [ ] Database connection stable

## 📝 Documentation

- [ ] Railway URL documented
- [ ] Vercel URL documented
- [ ] Admin credentials saved securely
- [ ] API documentation accessible

## 🎉 Go Live

- [ ] Shared demo URL with team/stakeholders
- [ ] Monitoring set up for errors
- [ ] Ready for production traffic

---

## 📋 Important URLs

```
Backend (Railway):  https://________________________________
Frontend (Vercel):  https://________________________________
API Swagger:        https://________________________________/swagger
Database Host:      ________________________________
```

---

## 🚨 If Something Goes Wrong

1. **Check Railway logs** for backend errors
2. **Check Vercel logs** for frontend build errors
3. **Check browser console** for frontend runtime errors
4. **Verify environment variables** are correct
5. **Check CORS configuration** matches exactly
6. **Redeploy** both services if needed

---

## 🔄 Redeployment Commands

### Backend (Railway)
- Auto-deploys on push to `main`
- Manual: Go to Railway → Select service → Click "Deploy"

### Frontend (Vercel)
- Auto-deploys on push to `main`
- Manual: `vercel --prod` (requires Vercel CLI)

---

## 📞 Support Resources

- Railway Docs: https://docs.railway.app
- Vercel Docs: https://vercel.com/docs
- Railway Discord: https://discord.gg/railway

---

**Last Updated**: _____________
**Deployed By**: _____________
