# Multi-stage build for Investments Tracker

# Stage 1: Build .NET Backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /app

COPY ["ETFTracker.Api/ETFTracker.Api.csproj", "ETFTracker.Api/"]
COPY ["global.json", "./"]
RUN dotnet restore "ETFTracker.Api/ETFTracker.Api.csproj"

COPY ["ETFTracker.Api/", "ETFTracker.Api/"]
RUN dotnet publish "ETFTracker.Api/ETFTracker.Api.csproj" \
    -c Release \
    -o /app/publish \
    --self-contained false \
    --no-restore

# Stage 2: Build Angular Frontend
FROM node:20-alpine AS frontend-build
WORKDIR /app

COPY ["ETFTracker.Web/", "."]
RUN npm ci
RUN npm run build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd -m -u 1001 appuser

COPY --from=backend-build /app/publish .
# Angular outputs browser files to dist/etftracker.web/browser/
COPY --from=frontend-build /app/dist/etftracker.web/browser ./wwwroot

RUN chown -R appuser:appuser /app

EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnosticTools=false

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD sh -c 'curl -f "http://localhost:${PORT:-8080}/healthz" || exit 1'

USER appuser
# Railway injects PORT; fallback 8080 for local container runs.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} ./ETFTracker.Api"]

