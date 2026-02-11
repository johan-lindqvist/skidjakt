# Build stage - use SDK for compilation
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy only project files first (for better layer caching)
COPY src/Skidjakt.Api/Skidjakt.Api.csproj src/Skidjakt.Api/
COPY src/Skidjakt.Core/Skidjakt.Core.csproj src/Skidjakt.Core/
COPY src/Skidjakt.Infrastructure/Skidjakt.Infrastructure.csproj src/Skidjakt.Infrastructure/
COPY src/Skidjakt.Scraper/Skidjakt.Scraper.csproj src/Skidjakt.Scraper/

# Restore only the API project and its dependencies (excludes tests)
RUN dotnet restore src/Skidjakt.Api/Skidjakt.Api.csproj

# Copy source code (only src/, not tests/)
COPY src/ src/

# Publish the API project
# --no-restore: we already restored above
# /p:UseAppHost=false: don't generate native executable, saves space
RUN dotnet publish src/Skidjakt.Api/Skidjakt.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage - use smaller alpine image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Install ICU for globalization (needed for Swedish locale, dates, etc.)
RUN apk add --no-cache icu-libs

# Set environment variables for globalization
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app

# Copy only the published output from build stage
COPY --from=build /app/publish .

# Create data directory for SQLite database
RUN mkdir -p /app/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "Skidjakt.Api.dll"]
