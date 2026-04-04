# Multi-stage build for ETF Investment Tracker
# Stage 1: Build .NET Backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /app

# Copy project files
COPY ["ETFTracker.Api/ETFTracker.Api.csproj", "ETFTracker.Api/"]
COPY ["global.json", "./"]

# Restore dependencies
RUN dotnet restore "ETFTracker.Api/ETFTracker.Api.csproj"

# Copy source code
COPY ["ETFTracker.Api/", "ETFTracker.Api/"]

# Publish in Release mode
RUN dotnet publish "ETFTracker.Api/ETFTracker.Api.csproj" \
    -c Release \
    -o /app/publish \
    --self-contained false \
    --no-restore

# Stage 2: Build Angular Frontend
FROM node:20-alpine AS frontend-build
WORKDIR /app

# Copy package files
COPY ["ETFTracker.Web/package.json", "ETFTracker.Web/package-lock.json", "./"]

# Install dependencies
RUN npm ci --omit=dev

# Copy source code
COPY ["ETFTracker.Web/", "."]

# Build for production
RUN npm run build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Install curl for health checks and create non-root user
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd -m -u 1001 appuser

# Copy published .NET app from backend build
COPY --from=backend-build /app/publish .

# Copy Angular build output to wwwroot
# Angular (application builder) outputs browser files to dist/etftracker.web/browser/
COPY --from=frontend-build /app/dist/etftracker.web/browser ./wwwroot

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 10000

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000
ENV DOTNET_EnableDiagnosticTools=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:10000/api/holdings || exit 1

# Start the application
ENTRYPOINT ["./ETFTracker.Api"]

