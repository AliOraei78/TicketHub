using TicketHub.Application.DTOs;
using TicketHub.Core.Common;

namespace TicketHub.Web.States;

public class WorkflowEditorState
{
    // Data Collections
    public List<StatusDto> AvailableStatuses { get; set; } = new();
    public List<RoleDto> AvailableRoles { get; set; } = new();
    public List<FieldTypeDto> AvailableFieldTypes { get; set; } = new();

    // Form & Workflow Status
    public WorkflowDto? CurrentWorkflow { get; set; }
    public string WorkflowName { get; set; } = "";
    public string WorkflowDescription { get; set; } = "";
    public string ActiveSourcePort { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public string StatusSearchTerm { get; set; } = "";

    // Canvas Elements
    public List<CanvasNodeDto> CanvasNodes { get; set; } = new();
    public List<CanvasConnection> Connections { get; set; } = new();
    public HashSet<CanvasNodeDto> SelectedNodes { get; set; } = new();
    public HashSet<CanvasConnection> SelectedConnections { get; set; } = new();

    // Drag & Drop State
    public bool IsDragging { get; set; }
    public CanvasNodeDto? DraggedNode { get; set; }
    public double StartMouseX { get; set; }
    public double StartMouseY { get; set; }
    public double InitialNodeX { get; set; }
    public double InitialNodeY { get; set; }
    public StatusDto? DraggingStatusFromSidebar { get; set; }

    // Connection State
    public bool IsConnecting { get; set; }
    public CanvasNodeDto? ConnectingFromNode { get; set; }

    // Box Selection & Mouse Position
    public bool IsBoxSelecting { get; set; }
    public double BoxStartX { get; set; }
    public double BoxStartY { get; set; }
    public double BoxEndX { get; set; }
    public double BoxEndY { get; set; }
    public double MouseX { get; set; }
    public double MouseY { get; set; }

    // UI Notification Pattern
    public event Action? OnStateChanged;
    public void NotifyStateChanged() => OnStateChanged?.Invoke();
}
