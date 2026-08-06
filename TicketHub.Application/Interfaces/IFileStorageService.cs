namespace TicketHub.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream content, string fileName, string folderName = "uploads/attachments");
    void DeleteFile(string filePath);
}