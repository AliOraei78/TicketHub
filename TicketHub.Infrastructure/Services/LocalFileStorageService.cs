using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketHub.Application.Common.Models;
using TicketHub.Application.Interfaces;

namespace TicketHub.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly StorageSettings _settings;

    public LocalFileStorageService(
        IWebHostEnvironment env,
        ILogger<LocalFileStorageService> logger,
        IOptions<StorageSettings>? options = null)
    {
        _env = env;
        _logger = logger;
        _settings = options?.Value ?? new StorageSettings();
    }

    public async Task<string> SaveFileAsync(
        Stream content,
        string fileName,
        string folderName = "uploads/attachments",
        CancellationToken cancellationToken = default)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var targetFolder = string.IsNullOrWhiteSpace(folderName) ? _settings.LocalPath : folderName;
        var uploadsFolder = Path.Combine(webRoot, targetFolder);

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var relativePath = Path.Combine(targetFolder, uniqueFileName).Replace("\\", "/");
        _logger.LogInformation("فایل با موفقیت در فضای دیسک محلی ذخیره شد: {FilePath}", relativePath);
        return relativePath;
    }

    public Task<Stream?> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult<Stream?>(null);

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var absolutePath = Path.Combine(webRoot, filePath.TrimStart('/'));

        if (!File.Exists(absolutePath))
        {
            _logger.LogWarning("فایل در مسیر مشخص شده بر روی دیسک یافت نشد: {AbsolutePath}", absolutePath);
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
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

        var normalizedPath = "/" + filePath.TrimStart('/');
        return Task.FromResult(normalizedPath);
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        DeleteFile(filePath);
        return Task.CompletedTask;
    }

    public void DeleteFile(string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath)) return;

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var absolutePath = Path.Combine(webRoot, relativeFilePath.TrimStart('/'));
        if (File.Exists(absolutePath))
        {
            try
            {
                File.Delete(absolutePath);
                _logger.LogInformation("فایل از فضای محلی حذف شد: {AbsolutePath}", absolutePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در حذف فایل محلی: {AbsolutePath}", absolutePath);
            }
        }
    }
}
