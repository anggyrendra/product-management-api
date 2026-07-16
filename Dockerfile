# =============================================================================
# Multi-stage Dockerfile for the Product Management API (ASP.NET Core 8 / C#)
# Stage 1: build the application using the SDK image
# Stage 2: run the published application using the smaller runtime image
# =============================================================================

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies first to leverage
# Docker's layer caching (restores only run when the csproj changes).
COPY ProductApi.csproj ./
RUN dotnet restore ProductApi.csproj

# Copy the rest of the source code and publish a release build.
COPY . ./
RUN dotnet publish ProductApi.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published output from the build stage.
COPY --from=build /app/publish ./

# Expose the HTTP port the application listens on.
EXPOSE 8080

# Configure ASP.NET Core to listen on all interfaces, port 8080.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Optional: disable first-time telemetry for a cleaner startup log.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "ProductApi.dll"]
