using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows
{
    public partial class WorkflowEditor : ComponentBase
    {
        [Parameter] public int? Id { get; set; }

        [Inject] private IWorkflowRepository WorkflowRepository { get; set; } = default!;
        [Inject] private IRepository<Status> StatusRepository { get; set; } = default!;
        [Inject] private IRepository<Role> RoleRepository { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private List<Status> availableStatuses = new();
        private List<Role> AvailableRoles = new();

        private string workflowName = "";
        private string workflowDescription = "";
        private string activeSourcePort = "";

        private bool isBoxSelecting = false;
        private double boxStartX, boxStartY, boxEndX, boxEndY;

        private string statusSearchTerm = "";
        private IEnumerable<Status> FilteredStatuses =>
            string.IsNullOrEmpty(statusSearchTerm) ? availableStatuses : availableStatuses.Where(s => s.Name.Contains(statusSearchTerm));

        private List<CanvasNode> canvasNodes = new();
        private List<CanvasConnection> connections = new();

        private bool isDragging = false;
        private CanvasNode? draggedNode;
        private double startMouseX, startMouseY;
        private double initialNodeX, initialNodeY;

        private bool isConnecting = false;
        private CanvasNode? connectingFromNode;
        private double mouseX, mouseY;

        private HashSet<CanvasNode> selectedNodes = new();
        private HashSet<CanvasConnection> selectedConnections = new();

        private Status? draggingStatusFromSidebar;

        protected override async Task OnInitializedAsync()
        {
            var statusesFromDb = await StatusRepository.GetAllAsync();
            availableStatuses = statusesFromDb.ToList();

            var rolesFromDb = await RoleRepository.GetAllAsync();
            AvailableRoles = rolesFromDb.ToList();

            if (Id.HasValue)
            {
                await LoadWorkflowAsync(Id.Value);
            }
        }

        private async Task LoadWorkflowAsync(int workflowId)
        {
            var workflow = await WorkflowRepository.GetWorkflowWithDetailsAsync(workflowId);
            if (workflow == null) return;

            canvasNodes = workflow.WorkflowStatuses.Select(ws => new CanvasNode
            {
                Id = Guid.NewGuid(),
                Status = ws.Status,
                X = ws.PositionX,
                Y = ws.PositionY
            }).ToList();

            connections = workflow.Transitions.Select(t => new CanvasConnection
            {
                Id = Guid.NewGuid(),
                FromNodeId = canvasNodes.First(n => n.Status.Id == t.FromState).Id,
                ToNodeId = canvasNodes.First(n => n.Status.Id == t.ToState).Id,
                Name = t.Name,
                IsAutomatic = t.IsAutomated == 1,
                IsActive = t.IsActive,
                AllowedRoleIds = t.AllowedRoles.Select(r => r.RoleId).ToHashSet()
            }).ToList();
        }

        private void AddStatusToCanvas(Status status)
        {
            canvasNodes.Add(new CanvasNode
            {
                Status = status,
                X = 250 + (canvasNodes.Count * 20),
                Y = 150 + (canvasNodes.Count * 20)
            });
        }

        private void RemoveNode(CanvasNode node)
        {
            connections.RemoveAll(c => c.FromNodeId == node.Id || c.ToNodeId == node.Id);
            canvasNodes.Remove(node);
            selectedNodes.Remove(node);
            selectedConnections.RemoveWhere(c => c.FromNodeId == node.Id || c.ToNodeId == node.Id);
        }

        private void DeleteConnection(CanvasConnection conn)
        {
            connections.Remove(conn);
            selectedConnections.Remove(conn);
        }

        private void ToggleRole(int roleId)
        {
            var selectedConnection = selectedConnections.FirstOrDefault();
            if (selectedConnection == null) return;

            if (selectedConnection.AllowedRoleIds.Contains(roleId))
                selectedConnection.AllowedRoleIds.Remove(roleId);
            else
                selectedConnection.AllowedRoleIds.Add(roleId);
        }

        private void NodeMouseDown(MouseEventArgs e, CanvasNode node)
        {
            if (isConnecting) return;
            isDragging = true;
            draggedNode = node;

            if (!selectedNodes.Contains(node) && !e.CtrlKey && !e.ShiftKey)
            {
                SelectNode(e, node);
            }
            else if (e.CtrlKey || e.ShiftKey)
            {
                SelectNode(e, node);
            }

            startMouseX = e.ClientX;
            startMouseY = e.ClientY;
            initialNodeX = node.X;
            initialNodeY = node.Y;
        }

        private void CanvasPointerDown(PointerEventArgs e)
        {
            if (!e.CtrlKey && !e.ShiftKey)
            {
                selectedNodes.Clear();
                selectedConnections.Clear();
            }

            isBoxSelecting = true;
            boxStartX = e.ClientX;
            boxStartY = e.ClientY - 90;
            boxEndX = boxStartX;
            boxEndY = boxStartY;
        }

        private void CanvasMouseMove(MouseEventArgs e)
        {
            mouseX = e.ClientX;
            mouseY = e.ClientY - 90;

            if (isDragging && draggedNode != null)
            {
                double dx = e.ClientX - startMouseX;
                double dy = e.ClientY - startMouseY;

                if (selectedNodes.Contains(draggedNode))
                {
                    foreach (var node in selectedNodes)
                    {
                        // در اینجا جابجایی گروهی نیاز به ذخیره آفست‌های اولیه همه نودها دارد
                        // فعلاً فقط نود درگ‌شده حرکت می‌کند
                    }
                }

                draggedNode.X = initialNodeX + dx;
                draggedNode.Y = initialNodeY + dy;
            }
            else if (isBoxSelecting)
            {
                boxEndX = mouseX;
                boxEndY = mouseY;
            }
        }

        private void CanvasMouseUp(MouseEventArgs e)
        {
            if (isBoxSelecting)
            {
                ApplyBoxSelection();
                isBoxSelecting = false;
            }
            isDragging = false;
            draggedNode = null;
            if (isConnecting)
            {
                isConnecting = false;
                connectingFromNode = null;
            }
        }

        private void ApplyBoxSelection()
        {
            double left = Math.Min(boxStartX, boxEndX);
            double top = Math.Min(boxStartY, boxEndY);
            double right = Math.Max(boxStartX, boxEndX);
            double bottom = Math.Max(boxStartY, boxEndY);

            // ۱. انتخاب وضعیت‌ها
            foreach (var node in canvasNodes)
            {
                if (node.X + 150 > left && node.X < right && node.Y + 72 > top && node.Y < bottom)
                {
                    selectedNodes.Add(node);
                }
            }

            // ۲. انتخاب مسیرها (Transitions)
            foreach (var conn in connections)
            {
                var fromNode = canvasNodes.FirstOrDefault(n => n.Id == conn.FromNodeId);
                var toNode = canvasNodes.FirstOrDefault(n => n.Id == conn.ToNodeId);

                if (fromNode != null && toNode != null)
                {
                    var sameNodeConnections = connections.Where(c => (c.FromNodeId == fromNode.Id && c.ToNodeId == toNode.Id) || (c.FromNodeId == toNode.Id && c.ToNodeId == fromNode.Id)).ToList();
                    int index = sameNodeConnections.IndexOf(conn);

                    var p1 = GetPortCoordinates(fromNode, conn.SourcePort);
                    var p2 = GetPortCoordinates(toNode, conn.TargetPort);

                    var center = GetBezierCenter(p1.X, p1.Y, p2.X, p2.Y, index);

                    // بررسی اینکه آیا نقطه مرکزی مسیر داخل کادر قرار گرفته است یا خیر
                    if (center.X >= left && center.X <= right && center.Y >= top && center.Y <= bottom)
                    {
                        selectedConnections.Add(conn);
                    }
                }
            }
        }
        private void SelectNode(MouseEventArgs e, CanvasNode node)
        {
            if (!e.CtrlKey && !e.ShiftKey)
            {
                selectedNodes.Clear();
                selectedConnections.Clear();
            }
            selectedNodes.Add(node);
        }

        private void SelectConnection(MouseEventArgs e, CanvasConnection conn)
        {
            if (!e.CtrlKey && !e.ShiftKey)
            {
                selectedNodes.Clear();
                selectedConnections.Clear();
            }
            selectedConnections.Add(conn);
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Delete")
            {
                foreach (var node in selectedNodes.ToList())
                {
                    RemoveNode(node);
                }
                foreach (var conn in selectedConnections.ToList())
                {
                    DeleteConnection(conn);
                }
                selectedNodes.Clear();
                selectedConnections.Clear();
            }
        }

        private void OnPortPointerDown(PointerEventArgs e, CanvasNode node, string port)
        {
            if (isConnecting)
            {
                if (connectingFromNode != null && connectingFromNode != node)
                {
                    CompleteConnection(node, port);
                }
            }
            else
            {
                isConnecting = true;
                connectingFromNode = node;
                activeSourcePort = port;
                selectedNodes.Clear();
                selectedConnections.Clear();
            }
        }

        private void OnPortPointerUp(PointerEventArgs e, CanvasNode node, string port)
        {
            if (isConnecting && connectingFromNode != null && connectingFromNode != node)
            {
                CompleteConnection(node, port);
            }
        }

        private void CompleteConnection(CanvasNode targetNode, string targetPort)
        {
            var newConn = new CanvasConnection
            {
                FromNodeId = connectingFromNode.Id,
                ToNodeId = targetNode.Id,
                SourcePort = activeSourcePort,
                TargetPort = targetPort
            };
            connections.Add(newConn);

            selectedNodes.Clear();
            selectedConnections.Clear();
            selectedConnections.Add(newConn);

            isConnecting = false;
            connectingFromNode = null;
            activeSourcePort = "";
        }

        private string GetNodeName(Guid id) => canvasNodes.FirstOrDefault(n => n.Id == id)?.Status.Name ?? "Unknown";

        private string GetBezierPath(double x1, double y1, double x2, double y2, int lineIndex = 0)
        {
            double midX = x1 + (x2 - x1) / 2;
            double midY = y1 + (y2 - y1) / 2;

            if (lineIndex == 0)
                return $"M {x1} {y1} Q {midX} {midY} {x2} {y2}";

            int multiplier = (lineIndex % 2 == 0) ? (lineIndex / 2) : -(lineIndex / 2 + 1);
            double offset = multiplier * 40;

            double dx = x2 - x1;
            double dy = y2 - y1;
            double length = Math.Sqrt(dx * dx + dy * dy);

            double nx = -dy / length;
            double ny = dx / length;

            double cx = midX + nx * offset;
            double cy = midY + ny * offset;

            return $"M {x1} {y1} Q {cx} {cy} {x2} {y2}";
        }

        private (double X, double Y) GetBezierCenter(double x1, double y1, double x2, double y2, int lineIndex = 0)
        {
            double midX = x1 + (x2 - x1) / 2;
            double midY = y1 + (y2 - y1) / 2;

            double dx = x2 - x1;
            double dy = y2 - y1;
            double length = Math.Sqrt(dx * dx + dy * dy);

            double nx = length > 0 ? -dy / length : 0;
            double ny = length > 0 ? dx / length : 0;

            if (lineIndex == 0) return (midX + nx * 20, midY + ny * 20);

            int multiplier = (lineIndex % 2 == 0) ? (lineIndex / 2) : -(lineIndex / 2 + 1);
            double offset = multiplier * 40;

            double curveMidX = midX + (nx * offset) / 2;
            double curveMidY = midY + (ny * offset) / 2;

            return (curveMidX + nx * 20, curveMidY + ny * 20);
        }

        private void OnStatusDragStart(Status status) => draggingStatusFromSidebar = status;

        private void CanvasOnDrop(DragEventArgs e)
        {
            if (draggingStatusFromSidebar != null)
            {
                canvasNodes.Add(new CanvasNode
                {
                    Status = draggingStatusFromSidebar,
                    X = Math.Max(0, e.ClientX - 75),
                    Y = Math.Max(0, e.ClientY - 94)
                });
                draggingStatusFromSidebar = null;
            }
        }

        private (double X, double Y) GetPortCoordinates(CanvasNode node, string port)
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

        private async Task SaveWorkflowAsync()
        {
            if (string.IsNullOrWhiteSpace(workflowName)) return;

            var workflow = new Workflow
            {
                Name = workflowName,
                Description = workflowDescription,
                WorkflowStatuses = canvasNodes.Select(n => new WorkflowStatus
                {
                    StatusId = n.Status.Id,
                    PositionX = n.X,
                    PositionY = n.Y
                }).ToList(),
                Transitions = connections.Select(c =>
                {
                    var fromStatusId = canvasNodes.First(n => n.Id == c.FromNodeId).Status.Id;
                    var toStatusId = canvasNodes.First(n => n.Id == c.ToNodeId).Status.Id;

                    return new Transition
                    {
                        Name = c.Name,
                        FromState = fromStatusId,
                        ToState = toStatusId,
                        SourcePort = c.SourcePort,
                        TargetPort = c.TargetPort,
                        IsAutomated = c.IsAutomatic ? 1 : 0,
                        IsActive = c.IsActive,
                        AllowedRoles = c.AllowedRoleIds.Select(roleId => new TransitionRole { RoleId = roleId }).ToList()
                    };
                }).ToList()
            };

            if (Id.HasValue)
            {
                workflow.Id = Id.Value;
                await WorkflowRepository.UpdateAsync(workflow);
            }
            else
            {
                await WorkflowRepository.AddAsync(workflow);
            }

            Navigation.NavigateTo("/workflows");
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
            // بررسی اینکه آیا آیتم کلیک شده در لیست انتخاب‌ها هست یا خیر
            bool isGroupDelete = false;

            if (clickedItem is CanvasNode node && selectedNodes.Contains(node)) isGroupDelete = true;
            if (clickedItem is CanvasConnection conn && selectedConnections.Contains(conn)) isGroupDelete = true;

            if (isGroupDelete)
            {
                // حذف همه موارد انتخاب شده (گروهی)
                foreach (var n in selectedNodes.ToList()) RemoveNode(n);
                foreach (var c in selectedConnections.ToList()) DeleteConnection(c);

                selectedNodes.Clear();
                selectedConnections.Clear();
            }
            else
            {
                // حذف انفرادی (اگر فقط یک آیتم کلیک شده بود)
                if (clickedItem is CanvasNode n) RemoveNode(n);
                if (clickedItem is CanvasConnection c) DeleteConnection(c);
            }
        }
    }
}