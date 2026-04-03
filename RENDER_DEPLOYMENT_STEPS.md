# 🚀 Docker Deployment Guide for Render.com

## ✅ Files Created

I've created the following files in your repository:

1. **`Dockerfile`** - Multi-stage Docker build configuration
2. **`.dockerignore`** - Optimizes Docker build by excluding unnecessary files
3. **`ETFTracker.Api/appsettings.Production.json`** - Production settings template
4. **Updated `ETFTracker.Api/Program.cs`** - Serves Angular app + runs migrations

---

## 📋 NEXT STEPS

### **STEP 1: Push Files to GitHub** (5 minutes)

```bash
cd "/Users/denisbahia/RiderProjects/Bob Esponja"
git add Dockerfile .dockerignore ETFTracker.Api/appsettings.Production.json ETFTracker.Api/Program.cs
git commit -m "Add Docker configuration for Render.com deployment"
git push origin main
```

**Verify on GitHub:**
- Go to your repo
- You should see `Dockerfile` in the root directory

---

### **STEP 2: Set Up PostgreSQL Database on Render.com** (10 minutes)

1. **Log in to Render.com** → [https://dashboard.render.com](https://dashboard.render.com)
2. **Click "New +"** button (top right) → Select **PostgreSQL**
3. **Configure your database:**
   - **Name**: `etf-tracker-db`
   - **Database**: `etf_tracker`
   - **User**: `postgres`
   - **Region**: Choose closest to you (EU/Ireland recommended)
   - **PostgreSQL Version**: 15
   - **Plan**: Starter (Free tier or $7/month paid)

4. **Click "Create Database"**
5. **SAVE these details** (you'll need them shortly):
   - **Host**: `[something].render.com`
   - **Database**: `etf_tracker`
   - **User**: `postgres`
   - **Password**: (Render will show this)
   - **Port**: `5432`
   - **Full Connection String**: Render provides this (copy it!)

---

### **STEP 3: Create Web Service on Render.com** (15 minutes)

1. **In Render Dashboard**, click **"New +"** → Select **Web Service**

2. **Connect Your Repository:**
   - Click "Connect a repository"
   - Select your GitHub repo (`bob-esponja` or whatever it's called)
   - Click "Connect"

3. **Configure the Service:**

   | Setting | Value |
   |---------|-------|
   | **Name** | `etf-tracker-app` |
   | **Environment** | `Docker` |
   | **Region** | (Choose your region) |
   | **Branch** | `main` |
   | **Plan** | Starter (Free) or Starter Plus ($7/month) |

   > **Note:** Free tier services go to sleep after 15 minutes of inactivity. Use Starter Plus ($7/month) for a production app.

4. **Click "Create Web Service"**

5. **Wait for deployment to start** (you'll see logs appearing)

---

### **STEP 4: Set Environment Variables in Render** (5 minutes)

While the web service is building, add your environment variables:

1. **In Render Dashboard**, click your newly created service (`etf-tracker-app`)
2. **Go to "Environment"** tab on the left
3. **Click "Add Environment Variable"** for each:

```
Variable Name: ASPNETCORE_ENVIRONMENT
Value: Production

Variable Name: ASPNETCORE_URLS  
Value: http://+:10000

Variable Name: ConnectionStrings__DefaultConnection
Value: postgresql://postgres:YOUR_PASSWORD@YOUR_HOST:5432/etf_tracker
(Replace YOUR_PASSWORD and YOUR_HOST with values from Step 2)

Variable Name: ExternalApis__EodhApi__ApiKey
Value: 69452ab0cc2501.73169166
(Or use your own Eodhd API key if you have one)
```

> **⚠️ IMPORTANT:** Replace placeholders with your actual database credentials from Step 2!

4. **Click "Save"** after each variable
5. Service will **automatically redeploy** with new variables

---

### **STEP 5: Monitor Deployment** (5-10 minutes)

1. **In your Render service page**, go to **"Logs"** tab
2. **Watch the deployment process:**
   - You should see: `Building Dockerfile...`
   - Then: `docker build` commands running
   - Then: `Successfully built...`
   - Then: Container starting up

3. **Look for these success indicators:**
   - ✅ `dotnet ETFTracker.Api` process starts
   - ✅ `Executing DbCommand...` (database migration running)
   - ✅ `Application startup complete`
   - ✅ `Listening on: http://+:10000`

4. **If you see errors:**
   - Check the **Logs** for specific error messages
   - Most common: Database connection string is wrong (STEP 4)
   - See **Troubleshooting** section below

---

### **STEP 6: Verify Deployment Works** (5 minutes)

Once you see "Application startup complete" in logs:

1. **Get your service URL:**
   - In Render Dashboard, your service has a URL like: `https://etf-tracker-app.onrender.com`

2. **Test the API is running:**
   ```bash
   curl https://etf-tracker-app.onrender.com/api/holdings
   ```
   
   Should return JSON (even if empty: `[]`)

3. **Test the Angular app loads:**
   - Open browser: `https://etf-tracker-app.onrender.com`
   - You should see your Angular app loading
   - Check browser console (F12) for any API errors

4. **Verify database is working:**
   - The app should load without database errors
   - If you see "Unable to connect to database" - your connection string is wrong

---

## ⚡ Quick Reference: Your Deployment URL

Once deployed, your app is live at:
```
https://etf-tracker-app.onrender.com
```

- **Frontend**: `https://etf-tracker-app.onrender.com`
- **API**: `https://etf-tracker-app.onrender.com/api`
- **Holdings**: `https://etf-tracker-app.onrender.com/api/holdings`

---

## 🐛 Troubleshooting

### **Build Fails: "Docker build failed"**
- Check the Logs in Render
- Common causes:
  - Invalid Dockerfile syntax (unlikely, we generated it)
  - Repository structure wrong (check paths match your project)
  - Missing dependencies in package.json

**Fix:** Copy the error message and we can debug.

---

### **Application Crashes: "Database connection failed"**

**Error:** `failed to create connection to ...`

**Cause:** Wrong database credentials

**Fix:**
1. Copy the full connection string from Render PostgreSQL page
2. Go to your Web Service → Environment
3. Update `ConnectionStrings__DefaultConnection` with correct string
4. Service will automatically redeploy

**Correct format:**
```
postgresql://postgres:yourpassword@host.render.com:5432/etf_tracker
```

---

### **Database Migrations Fail**

**Error:** `Executed DbCommand... failed`

**Cause:** Migration files not included or database schema issue

**Fix:**
1. In Render Shell (if available), run:
   ```bash
   dotnet ef database update
   ```
2. Or restart the service - it will retry migrations on startup

---

### **Angular App Shows Blank Page**

**Cause:** Incorrect API URL in Angular config

**Check:**
1. Open browser DevTools (F12)
2. Go to Network tab
3. Look for failed requests to `/api/`
4. The URL should be `https://etf-tracker-app.onrender.com/api`

**Fix:** If API URLs are wrong, the issue is in our Angular environment config (we can fix this).

---

### **Service Goes to Sleep (Free Tier)**

**Free tier services pause after 15 minutes of inactivity.**

**Solutions:**
1. Upgrade to **Starter Plus** ($7/month) - always running
2. Use free tier for development/testing only
3. Upgrade when going to production

---

## ✨ What's Happening Behind the Scenes

1. **Dockerfile builds in 3 stages:**
   - Stage 1: Builds your .NET API (Release mode)
   - Stage 2: Builds your Angular app (Production mode)
   - Stage 3: Combines both into runtime container

2. **On startup:**
   - .NET app starts on port 10000
   - Serves Angular files from `wwwroot/` directory
   - Runs database migrations automatically
   - Seeds default user if needed

3. **Requests are handled:**
   - API calls (`/api/*`) → routed to .NET controllers
   - Static files (`/`) → served from Angular build
   - Page refreshes → fallback to `index.html` (SPA routing)

---

## 📊 Expected Timings

| Step | Time |
|------|------|
| Push to GitHub | 1 min |
| Create Database | 3-5 min |
| Create Web Service | 1 min |
| Set Environment Variables | 2 min |
| Build Docker Image | 3-5 min |
| Start Application | 1-2 min |
| Run Migrations | 1 min |
| **Total** | **12-20 minutes** |

---

## 🎯 Success Checklist

- [ ] Files pushed to GitHub
- [ ] PostgreSQL database created on Render
- [ ] Web Service created on Render
- [ ] Environment variables set
- [ ] Deployment completed (no errors in Logs)
- [ ] API responds at `/api/holdings`
- [ ] Angular app loads in browser
- [ ] No database connection errors

---

## 🆘 Need Help?

If something goes wrong:

1. **Check Render Logs** first - they show exactly what went wrong
2. **Verify environment variables** - connection string is #1 issue
3. **Test locally** - make sure app works on your machine first

Common issues are usually:
- Wrong database credentials (95% of cases)
- Missing environment variables (4%)
- Project structure/paths (1%)

---

## 📝 Next: After Successful Deployment

Once everything is working:

1. **Test all features:**
   - Add holdings
   - View dashboard
   - Check price updates
   - Run projections

2. **Monitor in production:**
   - Render Dashboard shows CPU/Memory usage
   - Check Logs regularly for errors
   - Monitor database usage

3. **Set up monitoring (optional):**
   - Configure Render alerts
   - Set up uptime monitoring
   - Log aggregation

---

**You're all set! 🚀 Follow the steps above and you'll have your app live on Render.com in about 20 minutes.**

Any issues? Let me know!

