# =========================================================================
# Dockerfile for TicketHub.Web (.NET 10)
# =========================================================================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install fontconfig and fonts required by SkiaSharp and DNTCaptcha on Linux
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    fonts-liberation \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Set production environment defaults
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Create directory for logs, upload attachments, and dataprotection keys
RUN mkdir -p /app/logs /app/wwwroot/uploads /app/dataprotection-keys

# Copy published application
COPY bin/Publish/ .

# Expose standard ASP.NET Core port
EXPOSE 8080

# Run TicketHub Web application
ENTRYPOINT ["dotnet", "TicketHub.Web.dll"]
