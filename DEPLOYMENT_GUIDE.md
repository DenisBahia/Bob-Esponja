# Environment Setup & Deployment Guide

## Local Development Environment

### Prerequisites Installation

#### macOS with Homebrew

```bash
# PostgreSQL
brew install postgresql@18
brew services start postgresql@18

# .NET 10 SDK
# Download from https://dotnet.microsoft.com/download
# Or: brew install dotnet

# Node.js (already installed, verify)
node --version  # Should be 20+
npm --version   # Should be 11+

# Angular CLI
npm install -g @angular/cli
```

#### Windows
- PostgreSQL: Download installer from postgresql.org
- .NET SDK: Download from dotnet.microsoft.com
- Node.js: Download from nodejs.org
- Git: Download from git-scm.com

#### Linux (Ubuntu/Debian)
```bash
# PostgreSQL
sudo apt-get install postgresql postgresql-contrib

# .NET SDK
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh

# Node.js
sudo apt-get install nodejs npm

# Angular CLI
sudo npm install -g @angular/cli
```

---

## Configuration Files

### Backend Configuration

#### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=etf_tracker;Username=denisbahia;Password=postgres;"
  },
  "ExternalApis": {
    "EodhApi": {
      "ApiKey": "YOUR_EODHD_API_KEY",
      "BaseUrl": "https://eodhd.com/api"
    },
    "YahooFinance": {
      "BaseUrl": "https://query1.finance.yahoo.com"
    }
  },
  "AllowedHosts": "*"
}
```

#### appsettings.Development.json (Local overrides)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=etf_tracker;Username=denisbahia;Password=postgres;"
  }
}
```

### Frontend Environment Configuration

#### environment.ts (Development)
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

#### environment.prod.ts (Production)
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.yourdomain.com/api'
};
```

---

## Running the Application

### Development Mode

#### Terminal 1: Backend API
```bash
cd ETFTracker.Api
dotnet run
# Output:
# Now listening on: https://localhost:5000
# Application started. Press Ctrl+C to shut down.
```

#### Terminal 2: Frontend Dev Server
```bash
cd ETFTracker.Web
npm start
# Output:
# ✔ Building...
# ✔ Compiled successfully!
# Application bundle generation complete. [... seconds]
# 
# Watch mode enabled. Application will automatically reload on change.
# ⠋ Serving on http://localhost:4200/
```

#### Terminal 3: Optional - Database Monitoring
```bash
# Monitor PostgreSQL queries
psql -U denisbahia -d etf_tracker

# Useful queries:
# SELECT * FROM holdings WHERE user_id = 1;
# SELECT * FROM price_snapshots ORDER BY created_at DESC LIMIT 10;
# SELECT * FROM transactions WHERE holding_id = 1;
```

---

## Production Deployment

### Backend Deployment (IIS)

1. **Publish**:
```bash
cd ETFTracker.Api
dotnet publish -c Release -o ./publish
```

2. **Create IIS Application**:
- Create new Application Pool (.NET 10 Integrated Pipeline)
- Create new Website pointing to publish folder
- Configure application settings in web.config

3. **Update Configuration**:
- Edit `appsettings.Production.json`
- Set production database connection string
- Set production API keys
- Configure HTTPS certificate

### Backend Deployment (Docker)

Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ETFTracker.Api/ETFTracker.Api.csproj", "ETFTracker.Api/"]
RUN dotnet restore "ETFTracker.Api/ETFTracker.Api.csproj"
COPY . .
WORKDIR "/src/ETFTracker.Api"
RUN dotnet build "ETFTracker.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ETFTracker.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ETFTracker.Api.dll"]
```

Build and run:
```bash
docker build -t etf-tracker-api .
docker run -p 5000:5000 \
  -e ConnectionStrings__DefaultConnection="Host=db;Database=etf_tracker;Username=postgres;Password=password;" \
  -e ExternalApis__EodhApi__ApiKey="YOUR_API_KEY" \
  etf-tracker-api
```

### Frontend Deployment

1. **Build for Production**:
```bash
cd ETFTracker.Web
npm run build
# Output generated in dist/etftracker.web/
```

2. **Deploy to Web Server**:
```bash
# Copy dist contents to your web server
# Nginx example:
scp -r dist/etftracker.web/* user@server:/var/www/etf-tracker/

# Apache example:
scp -r dist/etftracker.web/* user@server:/var/www/html/etf-tracker/
```

3. **Web Server Configuration**:

**Nginx** (`/etc/nginx/sites-available/etf-tracker`):
```nginx
server {
    listen 80;
    server_name yourdomain.com;

    root /var/www/etf-tracker;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api {
        proxy_pass https://api.yourdomain.com;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    # SSL (with Let's Encrypt)
    listen 443 ssl http2;
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
}
```

**Apache** (`.htaccess` in root):
```apache
<IfModule mod_rewrite.c>
  RewriteEngine On
  RewriteBase /
  RewriteRule ^index\.html$ - [L]
  RewriteCond %{REQUEST_FILENAME} !-f
  RewriteCond %{REQUEST_FILENAME} !-d
  RewriteRule . /index.html [L]
</IfModule>

# Proxy API calls to backend
ProxyPreserveHost On
ProxyPassMatch "^/api/(.*)$" "https://api.yourdomain.com/api/$1"
```

### Database Migration to Production

```bash
# Create backup of production database
pg_dump -U denisbahia etf_tracker > backup_$(date +%Y%m%d_%H%M%S).sql

# Create new database
createdb -U denisbahia etf_tracker_prod

# Restore from backup
psql -U denisbahia etf_tracker_prod < backup.sql

# Or using migrations from .NET
dotnet ef database update --environment Production
```

---

## Monitoring & Maintenance

### Backend Monitoring

```bash
# Check API is running
curl -X GET https://localhost:5000/api/holdings/dashboard

# View logs
tail -f ~/.netcore/logs/etf-tracker.log
```

### Database Maintenance

```bash
# Backup database
pg_dump -U denisbahia etf_tracker | gzip > etf_tracker_$(date +%Y%m%d).sql.gz

# Restore from backup
gunzip < etf_tracker_20260331.sql.gz | psql -U denisbahia etf_tracker

# Analyze for performance
psql -U denisbahia etf_tracker
ANALYZE;
```

### Frontend Performance

- Monitor bundle size: `ng build --stats-json`
- Analyze with webpack-bundle-analyzer
- Use Chrome DevTools Lighthouse

---

## Troubleshooting Production Issues

### API Returns 500 Error
1. Check application logs
2. Verify database connection
3. Check API key configuration
4. Review Event Viewer (Windows) or syslog (Linux)

### Prices Not Updating
1. Verify API keys are correct
2. Check daily request limits
3. Test API calls manually
4. Review application logs

### Frontend Not Loading
1. Check CORS configuration in backend
2. Verify API URL in frontend config
3. Check network tab in browser console
4. Verify SSL certificate (if using HTTPS)

### Database Connection Issues
1. Verify PostgreSQL is running
2. Check connection string
3. Verify database user permissions
    4. Test connection: `psql -U denisbahia -d etf_tracker`

---

## Security Checklist

- [ ] Update CORS to specific domains (not *)
- [ ] Enable HTTPS/SSL certificates
- [ ] Configure firewall rules
- [ ] Set strong PostgreSQL passwords
- [ ] Rotate API keys regularly
- [ ] Implement rate limiting
- [ ] Enable database backups
- [ ] Add application monitoring
- [ ] Implement request logging
- [ ] Use environment variables for sensitive data

---

## Performance Optimization

### Backend
- Enable response compression in Program.cs
- Add database indexing (already done in schema)
- Implement caching for price data
- Use async/await throughout
- Monitor query performance with Entity Framework profiling

### Frontend
- Lazy load modules
- Implement change detection strategy
- Minimize bundle size
- Use production builds
- Enable gzip compression on web server

### Database
- Regular VACUUM and ANALYZE
- Archive old price snapshots
- Implement data retention policies
- Use connection pooling

---

## Scaling Considerations

### Horizontal Scaling
- Run multiple API instances behind load balancer
- Use session-less API design (✓ already implemented)
- Implement distributed caching (Redis)

### Vertical Scaling
- Increase server resources
- Optimize database queries
- Implement query result caching

### Database Scaling
- Read replicas for reporting
- Archive historical data
- Implement partitioning for large tables

---

## Backup & Recovery

### Automated Backups
```bash
# Daily backup script (cron)
#!/bin/bash
BACKUP_DIR="/backups/etf-tracker"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
pg_dump -U postgres etf_tracker | gzip > $BACKUP_DIR/backup_$TIMESTAMP.sql.gz

# Keep only last 30 days
find $BACKUP_DIR -name "backup_*.sql.gz" -mtime +30 -delete
```

### Recovery Procedure
1. Stop the application
2. Restore database from backup
3. Verify data integrity
4. Restart application
5. Test critical functions

---

## Useful Commands Reference

### .NET CLI
```bash
dotnet new webapi -n ProjectName          # Create new project
dotnet build                              # Build project
dotnet run                                # Run project
dotnet publish -c Release                 # Publish for deployment
dotnet ef migrations add MigrationName    # Create migration
dotnet ef database update                 # Apply migrations
dotnet test                               # Run tests
```

### Angular CLI
```bash
ng new ProjectName                        # Create new project
ng serve                                  # Start dev server
ng build                                  # Build for production
ng test                                   # Run tests
ng lint                                   # Check code style
ng e2e                                    # Run E2E tests
ng generate component ComponentName       # Generate component
```

### PostgreSQL
```bash
psql -U username -d database              # Connect to database
\dt                                       # List tables
\di                                       # List indexes
SELECT * FROM pg_stat_statements;         # View slow queries
VACUUM;                                   # Clean up database
ANALYZE;                                  # Update table statistics
```

---

**Version**: 1.0
**Last Updated**: April 12, 2026

