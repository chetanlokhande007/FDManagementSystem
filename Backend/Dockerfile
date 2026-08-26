# ═══════════════════════════════════════════════════════════════
#  Stage 1: Restore & Build
# ═══════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY FinTrustFDManager.Model/FinTrustFDManager.Model.csproj ./FinTrustFDManager.Model/
COPY FinTrustFDManager.DAL/FinTrustFDManager.DAL.csproj ./FinTrustFDManager.DAL/
COPY FinTrustFDManager.BAL/FinTrustFDManager.BAL.csproj ./FinTrustFDManager.BAL/
COPY FinTrustFDManager.API/FinTrustFDManager.API.csproj ./FinTrustFDManager.API/
COPY FinTrustFDManager.slnx ./

RUN dotnet restore FinTrustFDManager.API/FinTrustFDManager.API.csproj

# Copy source and build
COPY . .
RUN dotnet publish FinTrustFDManager.API/FinTrustFDManager.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ═══════════════════════════════════════════════════════════════
#  Stage 2: Runtime (minimal image)
# ═══════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Install curl for health check
RUN apk add --no-cache curl

# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

# Expose ports
# 8080 = HTTP (ASPNETCORE_URLS default in .NET 8)
# 8081 = HTTPS (optional, for reverse proxy scenarios)
EXPOSE 8080
EXPOSE 8081

# Health check — hits the /health endpoint
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Override ASPNETCORE_URLS for container (HTTP only — HTTPS is handled by reverse proxy)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "FinTrustFDManager.API.dll"]
