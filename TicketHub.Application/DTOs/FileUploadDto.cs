namespace TicketHub.Application.DTOs;

public class FileUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public Stream Content { get; set; } = default!;
}