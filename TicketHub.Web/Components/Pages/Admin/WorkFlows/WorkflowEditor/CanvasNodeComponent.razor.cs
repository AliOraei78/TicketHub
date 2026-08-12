using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows.WorkflowEditor;

public partial class CanvasNodeComponent : ComponentBase
{
    [Parameter] public CanvasNodeDto Node { get; set; } = null!;
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public bool IsConnecting { get; set; }
    [Parameter] public CanvasNodeDto? ConnectingFromNode { get; set; }
    [Parameter] public string CursorStyle { get; set; } = "grab";
    [Parameter] public string TextColor { get; set; } = "#1e293b";

    [Parameter] public bool ShowInitialButton { get; set; }
    [Parameter] public EventCallback<CanvasNodeDto> OnSetInitialNode { get; set; }
    [Parameter] public EventCallback<CanvasNodeDto> OnToggleFinalNode { get; set; }

    [Parameter] public EventCallback<(PointerEventArgs e, CanvasNodeDto node)> OnPointerDown { get; set; }
    [Parameter] public EventCallback<(MouseEventArgs e, CanvasNodeDto node)> OnClick { get; set; }
    [Parameter] public EventCallback<CanvasNodeDto> OnDelete { get; set; }

    [Parameter] public EventCallback<(PointerEventArgs e, CanvasNodeDto node, string port)> OnPortPointerDown { get; set; }
    [Parameter] public EventCallback<(PointerEventArgs e, CanvasNodeDto node, string port)> OnPortPointerUp { get; set; }
}
