using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class ModalDialog : ComponentBase
{
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    [Parameter, EditorRequired]
    public string Title { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string MaxWidthClass { get; set; } = "max-w-md";

    [Parameter]
    public string Class { get; set; } = string.Empty;
}
