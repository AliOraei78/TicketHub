using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows.WorkflowEditor;

public partial class WorkflowToolbar : ComponentBase
{
    [Parameter] public string Name { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> NameChanged { get; set; }

    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> DescriptionChanged { get; set; }

    [Parameter] public EventCallback OnSave { get; set; }

    [Parameter] public bool IsActive { get; set; } = true;
    [Parameter] public EventCallback<bool> IsActiveChanged { get; set; }

    [Parameter] public int NodeCount { get; set; }
    [Parameter] public int ConnectionCount { get; set; }

    protected async Task OnIsActiveChanged(ChangeEventArgs e)
    {
        IsActive = (bool)(e.Value ?? false);
        await IsActiveChanged.InvokeAsync(IsActive);
    }

    protected async Task OnNameInput(ChangeEventArgs e)
    {
        Name = e.Value?.ToString() ?? string.Empty;
        await NameChanged.InvokeAsync(Name);
    }

    protected async Task OnDescriptionInput(ChangeEventArgs e)
    {
        Description = e.Value?.ToString() ?? string.Empty;
        await DescriptionChanged.InvokeAsync(Description);
    }
}
