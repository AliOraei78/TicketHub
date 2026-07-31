using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TicketHub.Application.DTOs;
using TicketHub.Core.Common;
using TicketHub.Web.Facades;
using TicketHub.Web.States;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows.WorkflowEditor
{
    public partial class WorkflowEditor : ComponentBase, IDisposable
    {
        [Parameter] public int? Id { get; set; }

        [Inject] private WorkflowEditorFacade Facade { get; set; } = default!;
        [Inject] private WorkflowEditorState State { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private IEnumerable<StatusDto> FilteredStatuses =>
            string.IsNullOrEmpty(State.StatusSearchTerm)
                ? State.AvailableStatuses
                : State.AvailableStatuses.Where(s => s.Name.Contains(State.StatusSearchTerm));

        protected override async Task OnInitializedAsync()
        {
            State.OnStateChanged += StateHasChanged;
            await Facade.InitializeAsync(Id);
        }

        public void Dispose()
        {
            State.OnStateChanged -= StateHasChanged;
        }

        private async Task SaveWorkflowAsync()
        {
            bool isSuccess = await Facade.SaveWorkflowAsync(Id);
            if (isSuccess)
            {
                Navigation.NavigateTo("/workflows");
            }
        }

        private void AddStatusToCanvas(StatusDto status)
        {
            State.CanvasNodes.Add(new CanvasNodeDto
            {
                Status = status,
                X = 250 + (State.CanvasNodes.Count * 20),
                Y = 150 + (State.CanvasNodes.Count * 20)
            });
            State.NotifyStateChanged();
        }

        private void RemoveNode(CanvasNodeDto node)
        {
            State.Connections.RemoveAll(c => c.FromNodeId == node.Id || c.ToNodeId == node.Id);
            State.CanvasNodes.Remove(node);
            State.SelectedNodes.Remove(node);
            State.SelectedConnections.RemoveWhere(c => c.FromNodeId == node.Id || c.ToNodeId == node.Id);
        }

        private void DeleteConnection(CanvasConnection conn)
        {
            State.Connections.Remove(conn);
            State.SelectedConnections.Remove(conn);
        }

        private void ToggleRole(int roleId)
        {
            var selectedConnection = State.SelectedConnections.FirstOrDefault();
            if (selectedConnection == null) return;

            if (selectedConnection.AllowedRoleIds.Contains(roleId))
                selectedConnection.AllowedRoleIds.Remove(roleId);
            else
                selectedConnection.AllowedRoleIds.Add(roleId);
        }

        private void NodeMouseDown(MouseEventArgs e, CanvasNodeDto node)
        {
            if (State.IsConnecting) return;
            State.IsDragging = true;
            State.DraggedNode = node;

            if (!State.SelectedNodes.Contains(node) && !e.CtrlKey && !e.ShiftKey)
            {
                SelectNode(e, node);
            }
            else if (e.CtrlKey || e.ShiftKey)
            {
                SelectNode(e, node);
            }

            State.StartMouseX = e.ClientX;
            State.StartMouseY = e.ClientY;
            State.InitialNodeX = node.X;
            State.InitialNodeY = node.Y;
        }

        private void CanvasPointerDown(PointerEventArgs e)
        {
            if (!e.CtrlKey && !e.ShiftKey)
            {
                State.SelectedNodes.Clear();
                State.SelectedConnections.Clear();
            }

            State.IsBoxSelecting = true;
            State.BoxStartX = e.ClientX;
            State.BoxStartY = e.ClientY - 90;
            State.BoxEndX = State.BoxStartX;
            State.BoxEndY = State.BoxStartY;
        }

        private void CanvasMouseMove(MouseEventArgs e)
        {
            State.MouseX = e.ClientX;
            State.MouseY = e.ClientY - 90;

            if (State.IsDragging && State.DraggedNode != null)
            {
                double dx = e.ClientX - State.StartMouseX;
                double dy = e.ClientY - State.StartMouseY;

                State.DraggedNode.X = State.InitialNodeX + dx;
                State.DraggedNode.Y = State.InitialNodeY + dy;
            }
            else if (State.IsBoxSelecting)
            {
                State.BoxEndX = State.MouseX;
                State.BoxEndY = State.MouseY;
            }
        }

        private void CanvasMouseUp(MouseEventArgs e)
        {
            if (State.IsBoxSelecting)
            {
                ApplyBoxSelection();
                State.IsBoxSelecting = false;
            }
            State.IsDragging = false;
            State.DraggedNode = null;
            if (State.IsConnecting)
            {
                State.IsConnecting = false;
                State.ConnectingFromNode = null;
            }
        }

        private void ApplyBoxSelection()
        {
            double left = Math.Min(State.BoxStartX, State.BoxEndX);
            double top = Math.Min(State.BoxStartY, State.BoxEndY);
            double right = Math.Max(State.BoxStartX, State.BoxEndX);
            double bottom = Math.Max(State.BoxStartY, State.BoxEndY);

            foreach (var node in State.CanvasNodes)
            {
                if (node.X + 150 > left && node.X < right && node.Y + 72 > top && node.Y < bottom)
                {
                    State.SelectedNodes.Add(node);
                }
            }

            foreach (var conn in State.Connections)
            {
                var fromNode = State.CanvasNodes.FirstOrDefault(n => n.Id == conn.FromNodeId);
                var toNode = State.CanvasNodes.FirstOrDefault(n => n.Id == conn.ToNodeId);

                if (fromNode != null && toNode != null)
                {
                    var sameNodeConnections = State.Connections.Where(c => (c.FromNodeId == fromNode.Id && c.ToNodeId == toNode.Id) || (c.FromNodeId == toNode.Id && c.ToNodeId == fromNode.Id)).ToList();
                    int index = sameNodeConnections.IndexOf(conn);

                    var p1 = GetPortCoordinates(fromNode, conn.SourcePort);
                    var p2 = GetPortCoordinates(toNode, conn.TargetPort);

                    var center = GetBezierCenter(p1.X, p1.Y, p2.X, p2.Y, index, fromNode.Id, toNode.Id);

                    if (center.X >= left && center.X <= right && center.Y >= top && center.Y <= bottom)
                    {
                        State.SelectedConnections.Add(conn);
                    }
                }
            }
        }

        private void SelectNode(MouseEventArgs e, CanvasNodeDto node)
        {
            if (!e.CtrlKey && !e.ShiftKey)
            {
                State.SelectedNodes.Clear();
                State.SelectedConnections.Clear();
            }
            State.SelectedNodes.Add(node);
        }

        private void SelectConnection(MouseEventArgs e, CanvasConnection conn)
        {
            if (!e.CtrlKey && !e.ShiftKey)
            {
                State.SelectedNodes.Clear();
                State.SelectedConnections.Clear();
            }
            State.SelectedConnections.Add(conn);
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Delete")
            {
                foreach (var node in State.SelectedNodes.ToList()) RemoveNode(node);
                foreach (var conn in State.SelectedConnections.ToList()) DeleteConnection(conn);

                State.SelectedNodes.Clear();
                State.SelectedConnections.Clear();
            }
        }

        private void OnPortPointerDown(PointerEventArgs e, CanvasNodeDto node, string port)
        {
            if (!State.IsConnecting)
            {
                State.IsConnecting = true;
                State.ConnectingFromNode = node;
                State.ActiveSourcePort = port;
                State.SelectedNodes.Clear();
                State.SelectedConnections.Clear();
            }
        }

        private void OnPortPointerUp(PointerEventArgs e, CanvasNodeDto node, string port)
        {
            if (State.IsConnecting && State.ConnectingFromNode != null && State.ConnectingFromNode != node)
            {
                CompleteConnection(node, port);
            }
        }

        private void CompleteConnection(CanvasNodeDto targetNode, string targetPort)
        {
            if (State.ConnectingFromNode == null) return;

            var newConn = new CanvasConnection
            {
                Id = Guid.NewGuid(),
                FromNodeId = State.ConnectingFromNode.Id,
                ToNodeId = targetNode.Id,
                SourcePort = State.ActiveSourcePort,
                TargetPort = targetPort,
                Name = "",
                AllowedRoleIds = new HashSet<int>(),
                CustomFields = new List<CanvasTransitionField>()
            };
            State.Connections.Add(newConn);

            State.SelectedNodes.Clear();
            State.SelectedConnections.Clear();
            State.SelectedConnections.Add(newConn);

            State.IsConnecting = false;
            State.ConnectingFromNode = null;
            State.ActiveSourcePort = "";
        }

        private string GetNodeName(Guid id) => State.CanvasNodes.FirstOrDefault(n => n.Id == id)?.Status.Name ?? "Unknown";

        private bool CheckCollision(double x1, double y1, double cx, double cy, double x2, double y2, Guid fromNodeId, Guid toNodeId)
        {
            double[] tValues = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 };

            foreach (var node in State.CanvasNodes)
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

                    if ((dx * dx) / (95 * 95) + (dy * dy) / (60 * 60) <= 1)
                    {
                        return true;
                    }
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
                {
                    return (testCx, testCy);
                }
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

        private void OnStatusDragStart(StatusDto status) => State.DraggingStatusFromSidebar = status;

        private void CanvasOnDrop(DragEventArgs e)
        {
            if (State.DraggingStatusFromSidebar != null)
            {
                State.CanvasNodes.Add(new CanvasNodeDto
                {
                    Status = State.DraggingStatusFromSidebar,
                    X = Math.Max(0, e.ClientX - 75),
                    Y = Math.Max(0, e.ClientY - 94)
                });
                State.DraggingStatusFromSidebar = null;
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
            catch
            {
                return "#1e293b";
            }
        }

        private void DeleteSelectedItems(object clickedItem)
        {
            bool isGroupDelete = false;

            if (clickedItem is CanvasNodeDto node && State.SelectedNodes.Contains(node)) isGroupDelete = true;
            if (clickedItem is CanvasConnection conn && State.SelectedConnections.Contains(conn)) isGroupDelete = true;

            if (isGroupDelete)
            {
                foreach (var n in State.SelectedNodes.ToList()) RemoveNode(n);
                foreach (var c in State.SelectedConnections.ToList()) DeleteConnection(c);

                State.SelectedNodes.Clear();
                State.SelectedConnections.Clear();
            }
            else
            {
                if (clickedItem is CanvasNodeDto n) RemoveNode(n);
                if (clickedItem is CanvasConnection c) DeleteConnection(c);
            }
        }
    }
}