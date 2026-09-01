using System;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TicketHub.Application.Common.Models;
using TicketHub.Application.Interfaces;

namespace TicketHub.Infrastructure.Services;

public static class StorageServiceExtensions
{
    public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        var storageSection = configuration.GetSection(StorageSettings.SectionName);
        services.Configure<StorageSettings>(storageSection);
        var settings = storageSection.Get<StorageSettings>() ?? new StorageSettings();

        if (string.Equals(settings.Provider, "S3", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(settings.Provider, "MinIO", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var s3Config = new AmazonS3Config
                {
                    ForcePathStyle = settings.ForcePathStyle,
                    UseHttp = !settings.UseSsl
                };

                if (!string.IsNullOrWhiteSpace(settings.Region))
                {
                    s3Config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region);
                }

                if (!string.IsNullOrWhiteSpace(settings.Endpoint))
                {
                    s3Config.ServiceURL = settings.Endpoint;
                }

                if (!string.IsNullOrWhiteSpace(settings.AccessKey) && !string.IsNullOrWhiteSpace(settings.SecretKey))
                {
                    var credentials = new Amazon.Runtime.BasicAWSCredentials(settings.AccessKey, settings.SecretKey);
                    return new AmazonS3Client(credentials, s3Config);
                }

                return new AmazonS3Client(s3Config);
            });

            services.AddScoped<IFileStorageService, S3FileStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }

        return services;
    }
}
