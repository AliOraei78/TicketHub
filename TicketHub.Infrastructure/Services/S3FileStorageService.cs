using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketHub.Application.Common.Models;
using TicketHub.Application.Interfaces;

namespace TicketHub.Infrastructure.Services;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3FileStorageService> _logger;
    private readonly StorageSettings _settings;
    private static bool _bucketInitialized = false;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public S3FileStorageService(
        IAmazonS3 s3Client,
        IOptions<StorageSettings> options,
        ILogger<S3FileStorageService> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _settings = options.Value;
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketInitialized) return;

            var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _settings.BucketName);
            if (!bucketExists)
            {
                _logger.LogInformation("باکت S3 با نام '{BucketName}' وجود ندارد. در حال ایجاد باکت جدید...", _settings.BucketName);
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = _settings.BucketName,
                    UseClientRegion = true
                };
                await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken);
                _logger.LogInformation("باکت S3 '{BucketName}' با موفقیت ایجاد شد.", _settings.BucketName);
            }

            _bucketInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "خطا یا هشدار در بررسی یا ایجاد باکت '{BucketName}'. عملیات ادامه می‌یابد.", _settings.BucketName);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string> SaveFileAsync(
        Stream content,
        string fileName,
        string folderName = "uploads/attachments",
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var cleanFolder = string.IsNullOrWhiteSpace(folderName) ? "uploads/attachments" : folderName.Trim('/');
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var objectKey = $"{cleanFolder}/{uniqueFileName}";

        var putRequest = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
            InputStream = content,
            AutoCloseStream = false,
            DisablePayloadSigning = true,
            ContentType = GetContentType(fileName)
        };

        try
        {
            if (content.CanSeek)
            {
                putRequest.Headers.ContentLength = content.Length;
            }
        }
        catch
        {
            // Ignored if stream length cannot be determined
        }

        _logger.LogInformation("در حال آپلود فایل '{FileName}' به فضای ذخیره‌سازی S3/MinIO با کلید '{ObjectKey}'...", fileName, objectKey);
        await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        _logger.LogInformation("فایل '{FileName}' با موفقیت در باکت '{BucketName}' ذخیره شد.", fileName, _settings.BucketName);

        return objectKey;
    }

    public async Task<Stream?> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var key = NormalizeKey(filePath);

        try
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            _logger.LogWarning("فایل با کلید '{Key}' در باکت S3 '{BucketName}' یافت نشد.", key, _settings.BucketName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت استریم فایل '{Key}' از S3.", key);
            throw;
        }
    }

    public Task<string> GetFileUrlAsync(string filePath, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(string.Empty);

        if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(filePath);
        }

        var key = NormalizeKey(filePath);

        if (!string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
        {
            var baseUrl = _settings.PublicBaseUrl.TrimEnd('/');
            var publicUrl = $"{baseUrl}/{_settings.BucketName}/{key}";
            return Task.FromResult(publicUrl);
        }

        var expirationMinutes = _settings.PresignedUrlExpirationMinutes > 0 ? _settings.PresignedUrlExpirationMinutes : 60;
        var expiryDuration = expiry ?? TimeSpan.FromMinutes(expirationMinutes);

        var preSignedUrlRequest = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiryDuration),
            Verb = HttpVerb.GET
        };

        var preSignedUrl = _s3Client.GetPreSignedURL(preSignedUrlRequest);
        return Task.FromResult(preSignedUrl);
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        var key = NormalizeKey(filePath);

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            _logger.LogInformation("فایل با کلید '{Key}' از باکت S3 '{BucketName}' با موفقیت حذف شد.", key, _settings.BucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف فایل '{Key}' از باکت S3 '{BucketName}'.", key, _settings.BucketName);
        }
    }

    public void DeleteFile(string filePath)
    {
        DeleteFileAsync(filePath).GetAwaiter().GetResult();
    }

    private static string NormalizeKey(string path)
    {
        return path.TrimStart('/').Replace('\\', '/');
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".7z" => "application/x-7z-compressed",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".csv" => "text/csv",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
