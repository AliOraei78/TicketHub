using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TicketHub.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to storage and returns the stored file path / key.
    /// </summary>
    Task<string> SaveFileAsync(Stream content, string fileName, string folderName = "uploads/attachments", CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a readable stream of the file content.
    /// </summary>
    Task<Stream?> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a downloadable / preview URL for the file (either relative local URL or pre-signed S3 URL).
    /// </summary>
    Task<string> GetFileUrlAsync(string filePath, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes a file from storage.
    /// </summary>
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously deletes a file from storage for backward compatibility.
    /// </summary>
    void DeleteFile(string filePath);
}