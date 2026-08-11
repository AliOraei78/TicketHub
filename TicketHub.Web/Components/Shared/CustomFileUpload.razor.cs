using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace TicketHub.Web.Components.Shared;

public partial class CustomFileUpload : ComponentBase
{
    [Parameter] public string Label { get; set; } = "پیوست‌ها";
    [Parameter] public List<IBrowserFile> SelectedFiles { get; set; } = new();
    [Parameter] public EventCallback<List<IBrowserFile>> SelectedFilesChanged { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;

    protected string ErrorMessage { get; set; } = string.Empty;
    private const long MaxTotalSizeBytes = 10 * 1024 * 1024; // 10 MB
    private readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".xlsx", ".xls", ".doc", ".docx", ".ppt", ".pptx", ".txt", ".json" };

    protected async Task HandleSelection(InputFileChangeEventArgs e)
    {
        ErrorMessage = string.Empty;
        var files = e.GetMultipleFiles(maximumFileCount: 20);
        long currentTotalSize = SelectedFiles.Sum(f => f.Size);

        foreach (var file in files)
        {
            var extension = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                ErrorMessage = $"فرمت فایل {file.Name} مجاز نیست.";
                continue;
            }

            if (currentTotalSize + file.Size > MaxTotalSizeBytes)
            {
                ErrorMessage = "حجم کل فایل‌های انتخابی نمی‌تواند بیشتر از ۱۰ مگابایت باشد.";
                break;
            }

            if (!SelectedFiles.Any(f => f.Name == file.Name && f.Size == file.Size))
            {
                SelectedFiles.Add(file);
                currentTotalSize += file.Size;
            }
        }

        await SelectedFilesChanged.InvokeAsync(SelectedFiles);
    }

    protected async Task RemoveFile(IBrowserFile file)
    {
        SelectedFiles.Remove(file);
        await SelectedFilesChanged.InvokeAsync(SelectedFiles);
    }
}
