using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TicketHub.Application.DTOs;
using TicketHub.Core.Common;
using TicketHub.Core.Entities;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows.WorkflowEditor;

public partial class WorkflowCanvas : ComponentBase
{
    [Parameter] public List<CanvasNodeDto> Nodes { get; set; } = new();
    [Parameter] public List<CanvasConnection> Connections { get; set; } = new();
    [Parameter] public HashSet<CanvasNodeDto> SelectedNodes { get; set; } = new();
    [Parameter] public HashSet<CanvasConnection> SelectedConnections { get; set; } = new();

    [Parameter] public EventCallback<CanvasNodeDto> OnSetInitialNode { get; set; }
    [Parameter] public EventCallback<CanvasNodeDto> OnToggleFinalNode { get; set; }

    [Parameter] public bool IsBoxSelecting { get; set; }
    [Parameter] public double BoxStartX { get; set; }
    [Parameter] public double BoxStartY { get; set; }
    [Parameter] public double BoxEndX { get; set; }
    [Parameter] public double BoxEndY { get; set; }

    [Parameter] public bool IsConnecting { get; set; }
    [Parameter] public CanvasNodeDto? ConnectingFromNode { get; set; }
    [Parameter] public string ActiveSourcePort { get; set; } = "";
    [Parameter] public double MouseX { get; set; }
    [Parameter] public double MouseY { get; set; }

    [Parameter] public bool IsDragging { get; set; }
    [Parameter] public CanvasNodeDto? DraggedNode { get; set; }

    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }
    [Parameter] public EventCallback<PointerEventArgs> OnPointerDown { get; set; }
    [Parameter] public EventCallback<PointerEventArgs> OnPointerMove { get; set; }
    [Parameter] public EventCallback<PointerEventArgs> OnPointerUp { get; set; }
    [Parameter] public EventCallback<DragEventArgs> OnDrop { get; set; }

    [Parameter] public EventCallback<(MouseEventArgs e, CanvasNodeDto node)> OnNodeMouseDown { get; set; }
    [Parameter] public EventCallback<(MouseEventArgs e, CanvasNodeDto node)> OnSelectNode { get; set; }
    [Parameter] public EventCallback<(MouseEventArgs e, CanvasConnection conn)> OnSelectConnection { get; set; }
    [Parameter] public EventCallback<(PointerEventArgs e, CanvasNodeDto node, string port)> OnPortPointerDown { get; set; }
    [Parameter] public EventCallback<(PointerEventArgs e, CanvasNodeDto node, string port)> OnPortPointerUp { get; set; }
    [Parameter] public EventCallback<object> OnDeleteItem { get; set; }
    [Parameter] public Func<string, string> GetTextColor { get; set; } = _ => "#1e293b";
    [Parameter] public Func<double, double, double, double, int, Guid, Guid, string> GetBezierPath { get; set; } = null!;
    [Parameter] public Func<double, double, double, double, int, Guid, Guid, (double X, double Y)> GetBezierCenter { get; set; } = null!;
    [Parameter] public Func<CanvasNodeDto, string, (double X, double Y)> GetPortCoordinates { get; set; } = null!;
}
