namespace TicketHub.Application.Common.Models;

public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Storage provider type: "Local", "S3", or "MinIO"
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Local directory path for file storage when Provider is "Local"
    /// </summary>
    public string LocalPath { get; set; } = "uploads";

    /// <summary>
    /// S3 / MinIO custom endpoint URL (e.g. "http://minio:9000" or "http://127.0.0.1:9000")
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Target bucket name in S3 / MinIO
    /// </summary>
    public string BucketName { get; set; } = "tickethub-attachments";

    /// <summary>
    /// Access Key for S3 / MinIO
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// Secret Key for S3 / MinIO
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// AWS / S3 Region (default: us-east-1)
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Whether to force path-style URLs (http://endpoint/bucket/key), required for MinIO
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Whether to use SSL for S3 connection
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Optional public base URL / CDN URL for serving file links directly
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Pre-signed URL expiration duration in minutes (default: 60)
    /// </summary>
    public int PresignedUrlExpirationMinutes { get; set; } = 60;
}
