using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common;
using TicketHub.Application.DTOs;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows.WorkflowEditor
{
    public partial class WorkflowEditor : ComponentBase
    {
        [Parameter] public int? Id { get; set; }

        [Inject] private IWorkflowService WorkflowService { get; set; } = default!;
        [Inject] private IStatusService StatusService { get; set; } = default!;
        [Inject] private IRoleService RoleService { get; set; } = default!;
        [Inject] private IFieldTypeService FieldTypeService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private List<StatusDto> availableStatuses = new();
        private List<RoleDto> AvailableRoles = new();

        private string workflowName = "";
        private string workflowDescription = "";
        private string activeSourcePort = "";

        private string errorMessage = "";
        private WorkflowDto? currentWorkflow;

        private bool isBoxSelecting = false;
        private double boxStartX, boxStartY, boxEndX, boxEndY;

        private string statusSearchTerm = "";
        private IEnumerable<StatusDto> FilteredStatuses =>
            string.IsNullOrEmpty(statusSearchTerm) ? availableStatuses : availableStatuses.Where(s => s.Name.Contains(statusSearchTerm));

        private List<CanvasNodeDto> canvasNodes = new();
        private List<CanvasConnection> connections = new();

        private bool isDragging = false;
        private CanvasNodeDto? draggedNode;
        private double startMouseX, startMouseY;
        private double initialNodeX, initialNodeY;

        private List<FieldTypeDto> availableFieldTypes = new();

        private bool isConnecting = false;
        private CanvasNodeDto? connectingFromNode;
        private double mouseX, mouseY;

        private HashSet<CanvasNodeDto> selectedNodes = new();
        private HashSet<CanvasConnection> selectedConnections = new();

        private StatusDto? draggingStatusFromSidebar;

        protected override async Task OnInitializedAsync()
        {
            var statusesFromDb = await StatusService.GetAllAsync();
            availableStatuses = statusesFromDb.ToList();

            var rolesFromDb = await RoleService.GetAllRolesAsync();
            AvailableRoles = rolesFromDb.ToList();

            var fieldTypesFromDb = await FieldTypeService.GetAllAsync();
            availableFieldTypes = fieldTypesFromDb.ToList();

            if (Id.HasValue)
            {
                await LoadWorkflowAsync(Id.Value);
            }
        }

        private async Task LoadWorkflowAsync(int workflowId)
        {
            currentWorkflow = await WorkflowService.GetByIdWithDetailsAsync(workflowId);
            if (currentWorkflow == null) return;

            workflowName = currentWorkflow.Name ?? "";
            workflowDescription = currentWorkflow.Description ?? "";

            canvasNodes = currentWorkflow.WorkflowStatuses.Select(ws => new CanvasNodeDto
            {
                Id = ws.NodeId != Guid.Empty ? ws.NodeId : Guid.NewGuid(),
                Status = ws.Status!,
                X = ws.PositionX,
                Y = ws.PositionY
            }).ToList();

            connections = currentWorkflow.Transitions
                    .Select(t => new CanvasConnection
                    {
                        Id = Guid.NewGuid(),
                        DbId = t.Id,
                        FromNodeId = t.FromNodeId != Guid.Empty ? t.FromNodeId : canvasNodes.FirstOrDefault(n => n.Status.Id == t.FromState)?.Id ?? Guid.Empty,
                        ToNodeId = t.ToNodeId != Guid.Empty ? t.ToNodeId : canvasNodes.FirstOrDefault(n => n.Status.Id == t.ToState)?.Id ?? Guid.Empty,
                        SourcePort = string.IsNullOrEmpty(t.SourcePort) ? "Right" : t.SourcePort,
                        TargetPort = string.IsNullOrEmpty(t.TargetPort) ? "Left" : t.TargetPort,
                        Name = t.Name,
                        IsAutomatic = t.IsAutomated == 1,
                        IsActive = t.IsActive,
                        ActivateAt = t.ActivateAt,
                        AllowedRoleIds = t.AllowedRoleIds.ToHashSet(),
                        CustomFields = t.TransitionFields.Select(tf => new CanvasTransitionField
                        {
                            Id = tf.Id,
                            FieldTypeId = tf.FieldTypeId,
                            FieldName = tf.FieldName,
                            IsRequired = tf.IsRequired,
                            SortOrder = tf.SortOrder,
                            Options = tf.Options,
                            Placeholder = tf.Placeholder,
                            DefaultValue = tf.DefaultValue,
                            IsActive = tf.IsActive
                        }).ToList()
                    }).Where(c => c.FromNodeId != Guid.Empty && c.ToNodeId != Guid.Empty).ToList();
        }

        private void AddStatusToCanvas(StatusDto status)
        {
            canvasNodes.Add(new CanvasNodeDto
            {
                Status = status,
                X = 250 + (canvasNodes.Count * 20),
                Y = 150 + (canvasNodes.Count * 20)
            });
        }

        private void RemoveNode(CanvasNodeDto node)
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

        private void NodeMouseDown(MouseEventArgs e, CanvasNodeDto node)
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

            foreach (var node in canvasNodes)
            {
                if (node.X + 150 > left && node.X < right && node.Y + 72 > top && node.Y < bottom)
                {
                    selectedNodes.Add(node);
                }
            }

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

                    var center = GetBezierCenter(p1.X, p1.Y, p2.X, p2.Y, index, fromNode.Id, toNode.Id);

                    if (center.X >= left && center.X <= right && center.Y >= top && center.Y <= bottom)
                    {
                        selectedConnections.Add(conn);
                    }
                }
            }
        }

        private void SelectNode(MouseEventArgs e, CanvasNodeDto node)
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
                foreach (var node in selectedNodes.ToList()) RemoveNode(node);
                foreach (var conn in selectedConnections.ToList()) DeleteConnection(conn);

                selectedNodes.Clear();
                selectedConnections.Clear();
            }
        }

        private void OnPortPointerDown(PointerEventArgs e, CanvasNodeDto node, string port)
        {
            if (!isConnecting)
            {
                isConnecting = true;
                connectingFromNode = node;
                activeSourcePort = port;
                selectedNodes.Clear();
                selectedConnections.Clear();
            }
        }

        private void OnPortPointerUp(PointerEventArgs e, CanvasNodeDto node, string port)
        {
            if (isConnecting && connectingFromNode != null && connectingFromNode != node)
            {
                CompleteConnection(node, port);
            }
        }

        private void CompleteConnection(CanvasNodeDto targetNode, string targetPort)
        {
            if (connectingFromNode == null) return;

            var newConn = new CanvasConnection
            {
                Id = Guid.NewGuid(),
                FromNodeId = connectingFromNode.Id,
                ToNodeId = targetNode.Id,
                SourcePort = activeSourcePort,
                TargetPort = targetPort,
                Name = "",
                AllowedRoleIds = new HashSet<int>(),
                CustomFields = new List<CanvasTransitionField>()
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

        private bool CheckCollision(double x1, double y1, double cx, double cy, double x2, double y2, Guid fromNodeId, Guid toNodeId)
        {
            double[] tValues = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 };

            foreach (var node in canvasNodes)
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

        private void OnStatusDragStart(StatusDto status) => draggingStatusFromSidebar = status;

        private void CanvasOnDrop(DragEventArgs e)
        {
            if (draggingStatusFromSidebar != null)
            {
                canvasNodes.Add(new CanvasNodeDto
                {
                    Status = draggingStatusFromSidebar,
                    X = Math.Max(0, e.ClientX - 75),
                    Y = Math.Max(0, e.ClientY - 94)
                });
                draggingStatusFromSidebar = null;
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

        private async Task SaveWorkflowAsync()
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(workflowName))
            {
                errorMessage = "لطفاً عنوان جریان کاری را وارد کنید. این فیلد الزامی است.";
                return;
            }

            try
            {
                if (Id.HasValue && Id.Value > 0)
                {
                    if (currentWorkflow == null)
                        currentWorkflow = await WorkflowService.GetByIdWithDetailsAsync(Id.Value);

                    if (currentWorkflow != null)
                    {
                        currentWorkflow.Name = workflowName;
                        currentWorkflow.Description = workflowDescription;

                        var activeNodeIds = canvasNodes.Select(n => n.Id).ToList();
                        var statusesToRemove = currentWorkflow.WorkflowStatuses.Where(ws => !activeNodeIds.Contains(ws.NodeId)).ToList();
                        foreach (var st in statusesToRemove) currentWorkflow.WorkflowStatuses.Remove(st);

                        foreach (var n in canvasNodes)
                        {
                            var existingWs = currentWorkflow.WorkflowStatuses.FirstOrDefault(ws => ws.NodeId == n.Id);
                            if (existingWs != null)
                            {
                                existingWs.PositionX = n.X;
                                existingWs.PositionY = n.Y;
                                existingWs.StatusId = n.Status.Id;
                            }
                            else
                            {
                                currentWorkflow.WorkflowStatuses.Add(new WorkflowStatusDto
                                {
                                    NodeId = n.Id,
                                    StatusId = n.Status.Id,
                                    PositionX = n.X,
                                    PositionY = n.Y
                                });
                            }
                        }

                        var activeUiConnectionDbIds = connections.Where(c => c.DbId > 0).Select(c => c.DbId).ToList();

                        var transitionsToRemoveFromDto = currentWorkflow.Transitions
                            .Where(t => !activeUiConnectionDbIds.Contains(t.Id))
                            .ToList();

                        foreach (var t in transitionsToRemoveFromDto)
                        {
                            currentWorkflow.Transitions.Remove(t);
                        }

                        foreach (var conn in connections)
                        {
                            var fromStatusId = canvasNodes.First(n => n.Id == conn.FromNodeId).Status.Id;
                            var toStatusId = canvasNodes.First(n => n.Id == conn.ToNodeId).Status.Id;

                            if (conn.DbId > 0)
                            {
                                var existingDbTransition = currentWorkflow.Transitions.FirstOrDefault(t => t.Id == conn.DbId);

                                if (existingDbTransition != null)
                                {
                                    existingDbTransition.Name = string.IsNullOrWhiteSpace(conn.Name) ? "انتقال" : conn.Name;
                                    existingDbTransition.SourcePort = conn.SourcePort;
                                    existingDbTransition.TargetPort = conn.TargetPort;
                                    existingDbTransition.FromNodeId = conn.FromNodeId;
                                    existingDbTransition.ToNodeId = conn.ToNodeId;
                                    existingDbTransition.FromState = fromStatusId;
                                    existingDbTransition.ToState = toStatusId;
                                    existingDbTransition.IsAutomated = conn.IsAutomatic ? 1 : 0;
                                    existingDbTransition.IsActive = conn.IsActive;

                                    existingDbTransition.AllowedRoleIds = conn.AllowedRoleIds.ToList();

                                    existingDbTransition.TransitionFields = conn.CustomFields
                                        .Where(f => f.FieldTypeId > 0)
                                        .Select(f => new TransitionFieldDto
                                        {
                                            Id = f.Id,
                                            FieldTypeId = f.FieldTypeId,
                                            FieldName = f.FieldName,
                                            IsRequired = f.IsRequired,
                                            SortOrder = f.SortOrder,
                                            Options = f.Options,
                                            Placeholder = f.Placeholder,
                                            DefaultValue = f.DefaultValue,
                                            IsActive = f.IsActive
                                        }).ToList();
                                }
                            }
                            else
                            {
                                currentWorkflow.Transitions.Add(new TransitionDto
                                {
                                    Name = string.IsNullOrWhiteSpace(conn.Name) ? "انتقال" : conn.Name,
                                    FromState = fromStatusId,
                                    ToState = toStatusId,
                                    FromNodeId = conn.FromNodeId,
                                    ToNodeId = conn.ToNodeId,
                                    SourcePort = conn.SourcePort,
                                    TargetPort = conn.TargetPort,
                                    IsAutomated = conn.IsAutomatic ? 1 : 0,
                                    IsActive = conn.IsActive,
                                    AllowedRoleIds = conn.AllowedRoleIds.ToList(),
                                    TransitionFields = conn.CustomFields
                                                        .Where(f => f.FieldTypeId > 0)
                                                        .Select(f => new TransitionFieldDto
                                                        {
                                                            FieldTypeId = f.FieldTypeId,
                                                            FieldName = f.FieldName,
                                                            IsRequired = f.IsRequired,
                                                            SortOrder = f.SortOrder,
                                                            Options = f.Options,
                                                            Placeholder = f.Placeholder,
                                                            DefaultValue = f.DefaultValue,
                                                            IsActive = f.IsActive
                                                        }).ToList()
                                });
                            }
                        }

                        await WorkflowService.UpdateAsync(currentWorkflow);
                    }
                }
                else
                {
                    var workflow = new WorkflowDto
                    {
                        Name = workflowName,
                        Description = workflowDescription,
                        WorkflowStatuses = canvasNodes.Select(n => new WorkflowStatusDto
                        {
                            NodeId = n.Id,
                            StatusId = n.Status.Id,
                            PositionX = n.X,
                            PositionY = n.Y
                        }).ToList(),
                        Transitions = connections.Select(c =>
                        {
                            var fromStatusId = canvasNodes.First(n => n.Id == c.FromNodeId).Status.Id;
                            var toStatusId = canvasNodes.First(n => n.Id == c.ToNodeId).Status.Id;

                            return new TransitionDto
                            {
                                Name = string.IsNullOrWhiteSpace(c.Name) ? "انتقال" : c.Name,
                                FromState = fromStatusId,
                                ToState = toStatusId,
                                FromNodeId = c.FromNodeId,
                                ToNodeId = c.ToNodeId,
                                SourcePort = c.SourcePort,
                                TargetPort = c.TargetPort,
                                IsAutomated = c.IsAutomatic ? 1 : 0,
                                IsActive = c.IsActive,
                                AllowedRoleIds = c.AllowedRoleIds.ToList(),
                                TransitionFields = c.CustomFields
                                                    .Where(f => f.FieldTypeId > 0)
                                                    .Select(f => new TransitionFieldDto
                                                    {
                                                        FieldTypeId = f.FieldTypeId,
                                                        FieldName = f.FieldName,
                                                        IsRequired = f.IsRequired,
                                                        SortOrder = f.SortOrder,
                                                        Options = f.Options,
                                                        Placeholder = f.Placeholder,
                                                        DefaultValue = f.DefaultValue,
                                                        IsActive = f.IsActive
                                                    }).ToList()
                            };
                        }).ToList()
                    };

                    await WorkflowService.CreateAsync(workflow);
                }

                Navigation.NavigateTo("/workflows");
            }
            catch (Exception ex)
            {
                errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }
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

            if (clickedItem is CanvasNodeDto node && selectedNodes.Contains(node)) isGroupDelete = true;
            if (clickedItem is CanvasConnection conn && selectedConnections.Contains(conn)) isGroupDelete = true;

            if (isGroupDelete)
            {
                foreach (var n in selectedNodes.ToList()) RemoveNode(n);
                foreach (var c in selectedConnections.ToList()) DeleteConnection(c);

                selectedNodes.Clear();
                selectedConnections.Clear();
            }
            else
            {
                if (clickedItem is CanvasNodeDto n) RemoveNode(n);
                if (clickedItem is CanvasConnection c) DeleteConnection(c);
            }
        }
    }
}