# Multi-stage build for ETF Investment Tracker
# Stage 1: Build .NET Backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /app
COPY ["ETFTracker.Api/ETFTracker.Api.csproj", "ETFTracker.Api/"]
RUN dotnet restore "ETFTracker.Api/ETFTracker.Api.csproj"
COPY . .
RUN dotnet publish "ETFTracker.Api/ETFTracker.Api.csproj" -c Release -o /app/publish

# Stage 2: Build Angular Frontend
FROM node:20-alpine AS frontend-build
WORKDIR /app
COPY ["ETFTracker.Web/package.json", "ETFTracker.Web/package-lock.json", "./"]
RUN npm ci
COPY ETFTracker.Web/ .
RUN npm run build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published .NET app
COPY --from=backend-build /app/publish .

# Copy Angular build output to wwwroot
# Angular builds to dist/etftracker.web/ - copy the contents directly to wwwroot
COPY --from=frontend-build /app/dist/etftracker.web ./wwwroot

# Expose port (Render uses dynamic port assignment, but API runs on this)
EXPOSE 10000

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:10000/api/holdings || exit 1

# Start the application
CMD ["./ETFTracker.Api"]

