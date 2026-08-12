using Fluxor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TicketHub.Application.DTOs;
using TicketHub.Core.Common;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows.WorkflowEditor
{
    public partial class WorkflowEditor : IDisposable
    {
        [Parameter] public int? Id { get; set; }

        [Inject] private IState<WorkflowEditorState> EdState { get; set; } = default!;
        [Inject] private IDispatcher Dispatcher { get; set; } = default!;
        [Inject] private IActionSubscriber ActionSubscriber { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // Form & DB Data
        private WorkflowDto? CurrentWorkflow;
        private string WorkflowName = "";
        private string WorkflowDescription = "";
        private string StatusSearchTerm = "";
        private bool WorkflowIsActive = true;

        // Canvas UI State
        private List<CanvasNodeDto> CanvasNodes = new();
        private List<CanvasConnection> Connections = new();
        private HashSet<CanvasNodeDto> SelectedNodes = new();
        private HashSet<CanvasConnection> SelectedConnections = new();

        private bool IsDragging;
        private CanvasNodeDto? DraggedNode;
        private double StartMouseX, StartMouseY, InitialNodeX, InitialNodeY;
        private StatusDto? DraggingStatusFromSidebar;

        private bool IsConnecting;
        private CanvasNodeDto? ConnectingFromNode;
        private string ActiveSourcePort = "";

        private bool IsBoxSelecting;
        private double BoxStartX, BoxStartY, BoxEndX, BoxEndY, MouseX, MouseY;

        private IEnumerable<StatusDto> FilteredStatuses =>
            string.IsNullOrEmpty(StatusSearchTerm)
                ? EdState.Value.AvailableStatuses
                : EdState.Value.AvailableStatuses.Where(s => s.Name.Contains(StatusSearchTerm, StringComparison.OrdinalIgnoreCase));

        protected override void OnInitialized()
        {
            base.OnInitialized();

            ActionSubscriber.SubscribeToAction<EditorDataLoadedAction>(this, action =>
            {
                CurrentWorkflow = action.CurrentWorkflow;
                WorkflowName = action.Name;
                WorkflowDescription = action.Description;
                WorkflowIsActive = action.IsActive;
                CanvasNodes = action.Nodes;
                Connections = action.Connections;
                InvokeAsync(StateHasChanged);
            });

            ActionSubscriber.SubscribeToAction<SaveWorkflowEditorSuccessAction>(this, action =>
            {
                Navigation.NavigateTo("/workflows");
            });

            Dispatcher.Dispatch(new LoadEditorDataAction(Id));
        }

        public void Dispose()
        {
            ActionSubscriber.UnsubscribeFromAllActions(this);
        }

        private void SaveWorkflowAsync()
        {
            Dispatcher.Dispatch(new SaveWorkflowEditorAction(Id, CurrentWorkflow, WorkflowName, WorkflowDescription, WorkflowIsActive, CanvasNodes, Connections));
        }
        private void ClearError() => Dispatcher.Dispatch(new ClearEditorErrorAction());

        private void AddStatusToCanvas(StatusDto status)
        {
            CanvasNodes.Add(new CanvasNodeDto
            {
                Status = status,
                X = 250 + (CanvasNodes.Count * 20),
                Y = 150 + (CanvasNodes.Count * 20)
            });
        }

        private void RemoveNode(CanvasNodeDto node)
        {
            Connections.RemoveAll(c => c.FromNodeId == node.Id || c.ToNodeId == node.Id);
            CanvasNodes.Remove(node);
            SelectedNodes.Remove(node);
            SelectedConnections.RemoveWhere(c => c.FromNodeId == node.Id || c.ToNodeId == node.Id);
        }

        private void DeleteConnection(CanvasConnection conn)
        {
            Connections.Remove(conn);
            SelectedConnections.Remove(conn);
        }

        private void ToggleRole(int roleId)
        {
            var selectedConnection = SelectedConnections.FirstOrDefault();
            if (selectedConnection == null) return;

            if (selectedConnection.AllowedRoleIds.Contains(roleId))
                selectedConnection.AllowedRoleIds.Remove(roleId);
            else
                selectedConnection.AllowedRoleIds.Add(roleId);
        }

        private void NodeMouseDown(MouseEventArgs e, CanvasNodeDto node)
        {
            if (IsConnecting) return;
            IsDragging = true;
            DraggedNode = node;

            if (!SelectedNodes.Contains(node) && !e.CtrlKey && !e.ShiftKey)
                SelectNode(e, node);
            else if (e.CtrlKey || e.ShiftKey)
                SelectNode(e, node);

            StartMouseX = e.ClientX;
            StartMouseY = e.ClientY;
            InitialNodeX = node.X;
            InitialNodeY = node.Y;
        }

        private void CanvasPointerDown(PointerEventArgs e)
        {
            if (!e.CtrlKey && !e.ShiftKey)
            {
                SelectedNodes.Clear();
                SelectedConnections.Clear();
            }

            IsBoxSelecting = true;
            BoxStartX = e.ClientX;
            BoxStartY = e.ClientY - 90;
            BoxEndX = BoxStartX;
            BoxEndY = BoxStartY;
        }

        private void CanvasMouseMove(MouseEventArgs e)
        {
            MouseX = e.ClientX;
            MouseY = e.ClientY - 90;

            if (IsDragging && DraggedNode != null)
            {
                double dx = e.ClientX - StartMouseX;
                double dy = e.ClientY - StartMouseY;
                DraggedNode.X = InitialNodeX + dx;
                DraggedNode.Y = InitialNodeY + dy;
            }
            else if (IsBoxSelecting)
            {
                BoxEndX = MouseX;
                BoxEndY = MouseY;
            }
        }

        private void CanvasMouseUp(MouseEventArgs e)
        {
            if (IsBoxSelecting)
            {
                ApplyBoxSelection();
                IsBoxSelecting = false;
            }
            IsDragging = false;
            DraggedNode = null;

            if (IsConnecting)
            {
                IsConnecting = false;
                ConnectingFromNode = null;
            }
        }

        private void ApplyBoxSelection()
        {
            double left = Math.Min(BoxStartX, BoxEndX);
            double top = Math.Min(BoxStartY, BoxEndY);
            double right = Math.Max(BoxStartX, BoxEndX);
            double bottom = Math.Max(BoxStartY, BoxEndY);

            foreach (var node in CanvasNodes)
            {
                if (node.X + 150 > left && node.X < right && node.Y + 72 > top && node.Y < bottom)
                    SelectedNodes.Add(node);
            }

            foreach (var conn in Connections)
            {
                var fromNode = CanvasNodes.FirstOrDefault(n => n.Id == conn.FromNodeId);
                var toNode = CanvasNodes.FirstOrDefault(n => n.Id == conn.ToNodeId);

                if (fromNode != null && toNode != null)
                {
                    var sameNodeConnections = Connections.Where(c => (c.FromNodeId == fromNode.Id && c.ToNodeId == toNode.Id) || (c.FromNodeId == toNode.Id && c.ToNodeId == fromNode.Id)).ToList();
                    int index = sameNodeConnections.IndexOf(conn);

                    var p1 = GetPortCoordinates(fromNode, conn.SourcePort);
                    var p2 = GetPortCoordinates(toNode, conn.TargetPort);
                    var center = GetBezierCenter(p1.X, p1.Y, p2.X, p2.Y, index, fromNode.Id, toNode.Id);

                    if (center.X >= left && center.X <= right && center.Y >= top && center.Y <= bottom)
                        SelectedConnections.Add(conn);
                }
            }
        }

        private void SelectNode(MouseEventArgs e, CanvasNodeDto node)
        {
            if (!e.CtrlKey && !e.ShiftKey)
            {
                SelectedNodes.Clear();
                SelectedConnections.Clear();
            }
            SelectedNodes.Add(node);
        }

        private void SelectConnection(MouseEventArgs e, CanvasConnection conn)
        {
            if (!e.CtrlKey && !e.ShiftKey)
            {
                SelectedNodes.Clear();
                SelectedConnections.Clear();
            }
            SelectedConnections.Add(conn);
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Delete")
            {
                foreach (var node in SelectedNodes.ToList()) RemoveNode(node);
                foreach (var conn in SelectedConnections.ToList()) DeleteConnection(conn);

                SelectedNodes.Clear();
                SelectedConnections.Clear();
            }
        }

        private void OnPortPointerDown(PointerEventArgs e, CanvasNodeDto node, string port)
        {
            if (!IsConnecting)
            {
                IsConnecting = true;
                ConnectingFromNode = node;
                ActiveSourcePort = port;
                SelectedNodes.Clear();
                SelectedConnections.Clear();
            }
        }

        private void OnPortPointerUp(PointerEventArgs e, CanvasNodeDto node, string port)
        {
            if (IsConnecting && ConnectingFromNode != null && ConnectingFromNode != node)
            {
                CompleteConnection(node, port);
            }
        }

        private void CompleteConnection(CanvasNodeDto targetNode, string targetPort)
        {
            if (ConnectingFromNode == null) return;

            var newConn = new CanvasConnection
            {
                Id = Guid.NewGuid(),
                FromNodeId = ConnectingFromNode.Id,
                ToNodeId = targetNode.Id,
                SourcePort = ActiveSourcePort,
                TargetPort = targetPort,
                Name = "",
                AllowedRoleIds = new HashSet<int>(),
                CustomFields = new List<CanvasTransitionField>()
            };
            Connections.Add(newConn);

            SelectedNodes.Clear();
            SelectedConnections.Clear();
            SelectedConnections.Add(newConn);

            IsConnecting = false;
            ConnectingFromNode = null;
            ActiveSourcePort = "";
        }

        private string GetNodeName(Guid id) => CanvasNodes.FirstOrDefault(n => n.Id == id)?.Status.Name ?? "Unknown";

        private bool CheckCollision(double x1, double y1, double cx, double cy, double x2, double y2, Guid fromNodeId, Guid toNodeId)
        {
            double[] tValues = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 };

            foreach (var node in CanvasNodes)
            {
                if (node.Id == fromNodeId || node.Id == toNodeId) continue;

                double nodeCenterX = node.X + 75;
                double nodeCenterY = node.Y + 36;

                foreach (var t in tValues)
                {
                    double u = 1 - t;
                    double px = u * u * x1 + 2 * u * t * cx + t * t * x2;
                    double py = u * u * y1 + 2 * u * t * cy + t * t * y2;

                    double dx = px - nodeCenterX;
                    double dy = py - nodeCenterY;

                    if ((dx * dx) / (95 * 95) + (dy * dy) / (60 * 60) <= 1) return true;
                }
            }
            return false;
        }

        private (double cx, double cy) CalculateControlPoint(double x1, double y1, double x2, double y2, int lineIndex, Guid fromNodeId, Guid toNodeId)
        {
            double midX = x1 + (x2 - x1) / 2;
            double midY = y1 + (y2 - y1) / 2;

            double dx = x2 - x1;
            double dy = y2 - y1;
            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length == 0) return (midX, midY);

            double nx = -dy / length;
            double ny = dx / length;

            int multiplier = (lineIndex % 2 == 0) ? (lineIndex / 2) : -(lineIndex / 2 + 1);
            double baseOffset = multiplier * 40;

            double cx = midX + nx * baseOffset;
            double cy = midY + ny * baseOffset;

            if (fromNodeId == Guid.Empty || toNodeId == Guid.Empty)
            {
                if (lineIndex == 0) return (midX, midY);
                return (cx, cy);
            }

            int maxIterations = 20;
            double step = 35;
            double[] offsetsToTry = new double[maxIterations];

            for (int i = 0; i < maxIterations; i++)
            {
                int sign = (i % 2 == 0) ? 1 : -1;
                int magnitude = (i / 2) + 1;
                offsetsToTry[i] = sign * magnitude * step;
            }

            if (!CheckCollision(x1, y1, cx, cy, x2, y2, fromNodeId, toNodeId))
            {
                if (lineIndex == 0 && baseOffset == 0) return (midX, midY);
                return (cx, cy);
            }

            foreach (var testOffset in offsetsToTry)
            {
                double testCx = midX + nx * (baseOffset + testOffset);
                double testCy = midY + ny * (baseOffset + testOffset);

                if (!CheckCollision(x1, y1, testCx, testCy, x2, y2, fromNodeId, toNodeId))
                    return (testCx, testCy);
            }
            return (cx, cy);
        }

        private string GetBezierPath(double x1, double y1, double x2, double y2, int lineIndex = 0, Guid fromNodeId = default, Guid toNodeId = default)
        {
            var cp = CalculateControlPoint(x1, y1, x2, y2, lineIndex, fromNodeId, toNodeId);
            return $"M {x1} {y1} Q {cp.cx} {cp.cy} {x2} {y2}";
        }

        private (double X, double Y) GetBezierCenter(double x1, double y1, double x2, double y2, int lineIndex = 0, Guid fromNodeId = default, Guid toNodeId = default)
        {
            var cp = CalculateControlPoint(x1, y1, x2, y2, lineIndex, fromNodeId, toNodeId);
            double curveMidX = 0.25 * x1 + 0.5 * cp.cx + 0.25 * x2;
            double curveMidY = 0.25 * y1 + 0.5 * cp.cy + 0.25 * y2;
            return (curveMidX, curveMidY);
        }

        private void OnStatusDragStart(StatusDto status) => DraggingStatusFromSidebar = status;

        private void CanvasOnDrop(DragEventArgs e)
        {
            if (DraggingStatusFromSidebar != null)
            {
                CanvasNodes.Add(new CanvasNodeDto
                {
                    Status = DraggingStatusFromSidebar,
                    X = Math.Max(0, e.ClientX - 75),
                    Y = Math.Max(0, e.ClientY - 94)
                });
                DraggingStatusFromSidebar = null;
            }
        }

        private (double X, double Y) GetPortCoordinates(CanvasNodeDto node, string port)
        {
            return port switch
            {
                "Top" => (node.X + 75, node.Y),
                "Bottom" => (node.X + 75, node.Y + 72),
                "Left" => (node.X, node.Y + 36),
                "Right" => (node.X + 150, node.Y + 36),
                _ => (node.X + 75, node.Y + 36)
            };
        }

        private string GetTextColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#") || hexColor.Length < 7)
                return "#1e293b";

            try
            {
                var r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
                var g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
                var b = Convert.ToInt32(hexColor.Substring(5, 2), 16);
                var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
                return luminance > 0.6 ? "#1e293b" : "#ffffff";
            }
            catch { return "#1e293b"; }
        }

        private void DeleteSelectedItems(object clickedItem)
        {
            bool isGroupDelete = false;

            if (clickedItem is CanvasNodeDto node && SelectedNodes.Contains(node)) isGroupDelete = true;
            if (clickedItem is CanvasConnection conn && SelectedConnections.Contains(conn)) isGroupDelete = true;

            if (isGroupDelete)
            {
                foreach (var n in SelectedNodes.ToList()) RemoveNode(n);
                foreach (var c in SelectedConnections.ToList()) DeleteConnection(c);
                SelectedNodes.Clear();
                SelectedConnections.Clear();
            }
            else
            {
                if (clickedItem is CanvasNodeDto n) RemoveNode(n);
                if (clickedItem is CanvasConnection c) DeleteConnection(c);
            }
        }

        private void SetInitialNode(CanvasNodeDto node)
        {
            if (node.IsInitial) return;

            foreach (var n in CanvasNodes) n.IsInitial = false;
            node.IsInitial = true;
            node.IsFinal = false;

            InvokeAsync(StateHasChanged);
        }

        private void ToggleFinalNode(CanvasNodeDto node)
        {
            node.IsFinal = !node.IsFinal;
            if (node.IsFinal)
            {
                node.IsInitial = false;
            }

            InvokeAsync(StateHasChanged);
        }
    }
}