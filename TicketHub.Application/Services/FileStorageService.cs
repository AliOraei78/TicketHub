using Microsoft.AspNetCore.Hosting;
using TicketHub.Application.Interfaces;

namespace TicketHub.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveFileAsync(Stream content, string fileName, string folderName = "uploads/attachments")
    {
        var uploadsFolder = Path.Combine(_env.WebRootPath, folderName);
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(fileStream);

        return Path.Combine(folderName, uniqueFileName).Replace("\\", "/");
    }

    public void DeleteFile(string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath)) return;

        var absolutePath = Path.Combine(_env.WebRootPath, relativeFilePath.TrimStart('/'));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }
}