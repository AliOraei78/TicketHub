# =========================================================================
# Multi-stage Dockerfile for TicketHub.Web (.NET 10)
# =========================================================================

# -------------------------------------------------------------------------
# Stage 1: Build & Publish
# -------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for optimal layer caching during restore
COPY ["TicketHub.Core/TicketHub.Core.csproj", "TicketHub.Core/"]
COPY ["TicketHub.Application/TicketHub.Application.csproj", "TicketHub.Application/"]
COPY ["TicketHub.Infrastructure/TicketHub.Infrastructure.csproj", "TicketHub.Infrastructure/"]
COPY ["TicketHub.Web/TicketHub.Web.csproj", "TicketHub.Web/"]

# Restore NuGet packages
RUN dotnet restore "TicketHub.Web/TicketHub.Web.csproj"

# Copy all source files
COPY . .

# Build and publish release output
WORKDIR "/src/TicketHub.Web"
RUN dotnet publish "TicketHub.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -------------------------------------------------------------------------
# Stage 2: Final Runtime Image
# -------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install fontconfig and fonts required by SkiaSharp and DNTCaptcha on Linux
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    fonts-liberation \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Set production environment defaults
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Create directory for logs and upload attachments with write permissions
RUN mkdir -p /app/logs /app/wwwroot/uploads

# Copy published application
COPY --from=build /app/publish .

# Expose standard ASP.NET Core port
EXPOSE 8080

# Run TicketHub Web application
ENTRYPOINT ["dotnet", "TicketHub.Web.dll"]
