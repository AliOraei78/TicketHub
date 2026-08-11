using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;

namespace TicketHub.Web.Components.Shared;

public partial class DynamicFields : ComponentBase
{
    [Parameter] public string? Title { get; set; }

    [Parameter, EditorRequired]
    public List<DynamicFieldModel> Fields { get; set; } = new();

    [Parameter]
    public Dictionary<string, string> ValidationErrors { get; set; } = new();

    [Parameter]
    public string Class { get; set; } = string.Empty;

    private Dictionary<int, List<IBrowserFile>> _files = new();

    protected string GetGridClass(DynamicFieldModel field)
    {
        var type = (FieldTypeEnum)field.FieldTypeId;
        if (type == FieldTypeEnum.TextArea || type == FieldTypeEnum.File)
        {
            return "col-span-1 sm:col-span-2 flex flex-col";
        }
        return "col-span-1 flex flex-col";
    }

    protected List<IBrowserFile> GetInternalBrowserFiles(DynamicFieldModel field)
    {
        if (_files.TryGetValue(field.OriginalFieldId, out var files))
            return files;
        return new List<IBrowserFile>();
    }

    protected async Task HandleFilesChanged(DynamicFieldModel field, List<IBrowserFile> files)
    {
        _files[field.OriginalFieldId] = files;
        var pendingUploads = new List<FileUploadDto>();

        foreach (var f in files)
        {
            var ms = new System.IO.MemoryStream();
            await f.OpenReadStream(10 * 1024 * 1024).CopyToAsync(ms);
            ms.Position = 0;

            pendingUploads.Add(new FileUploadDto
            {
                FileName = f.Name,
                ContentType = f.ContentType,
                Size = f.Size,
                Content = ms
            });
        }

        field.PendingUploads = pendingUploads;
        field.Value = files.Any() ? $"{files.Count} فایل انتخاب شد" : string.Empty;
    }

    protected bool GetCheckboxValue(DynamicFieldModel field) => bool.TryParse(field.Value, out var val) && val;
    protected void SetCheckboxValue(DynamicFieldModel field, bool val) => field.Value = val.ToString().ToLower();

    protected List<string> GetOptions(DynamicFieldModel field)
    {
        return string.IsNullOrWhiteSpace(field.Options)
            ? new List<string>()
            : field.Options.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(o => o.Trim())
                           .ToList();
    }

    protected List<string> GetMultipleValues(DynamicFieldModel field)
    {
        return string.IsNullOrWhiteSpace(field.Value)
            ? new List<string>()
            : field.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    protected void SetMultipleValues(DynamicFieldModel field, List<string> vals)
    {
        field.Value = string.Join(",", vals);
    }
}
