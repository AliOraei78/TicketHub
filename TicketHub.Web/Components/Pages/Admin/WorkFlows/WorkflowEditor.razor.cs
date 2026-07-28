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
        [Inject] private IRepository<FieldType> FieldTypeRepository { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        private List<Status> availableStatuses = new();
        private List<Role> AvailableRoles = new();

        private string workflowName = "";
        private string workflowDescription = "";
        private string activeSourcePort = "";

        private string errorMessage = "";
        private Workflow? currentWorkflow;

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

        private List<FieldType> availableFieldTypes = new();

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

            // دریافت انواع فیلد از دیتابیس
            var fieldTypesFromDb = await FieldTypeRepository.GetAllAsync();
            availableFieldTypes = fieldTypesFromDb.ToList();

            if (Id.HasValue)
            {
                await LoadWorkflowAsync(Id.Value);
            }
        }

        private async Task LoadWorkflowAsync(int workflowId)
        {
            currentWorkflow = await WorkflowRepository.GetWorkflowWithDetailsAsync(workflowId);
            if (currentWorkflow == null) return;

            workflowName = currentWorkflow.Name ?? "";
            workflowDescription = currentWorkflow.Description ?? "";

            // استفاده از NodeId که در دیتابیس ذخیره شده است
            canvasNodes = currentWorkflow.WorkflowStatuses.Select(ws => new CanvasNode
            {
                Id = ws.NodeId != Guid.Empty ? ws.NodeId : Guid.NewGuid(), // سازگاری با دیتای قبلی
                Status = ws.Status,
                X = ws.PositionX,
                Y = ws.PositionY
            }).ToList();

            connections = currentWorkflow.Transitions
                    .Where(t => t.IsActive)
                    .Select(t => new CanvasConnection
                    {
                        Id = Guid.NewGuid(),
                        DbId = t.Id, // <--- این خط اضافه شود
                        FromNodeId = t.FromNodeId != Guid.Empty ? t.FromNodeId : canvasNodes.FirstOrDefault(n => n.Status.Id == t.FromState)?.Id ?? Guid.Empty,
                ToNodeId = t.ToNodeId != Guid.Empty ? t.ToNodeId : canvasNodes.FirstOrDefault(n => n.Status.Id == t.ToState)?.Id ?? Guid.Empty,

                        // 👇 این دو خط باید اضافه شوند تا پورت‌های رسم شده بازخوانی شوند
                SourcePort = string.IsNullOrEmpty(t.SourcePort) ? "Right" : t.SourcePort,
                TargetPort = string.IsNullOrEmpty(t.TargetPort) ? "Left" : t.TargetPort,

                Name = t.Name,
                IsAutomatic = t.IsAutomated == 1,
                IsActive = t.IsActive,
                ActivateAt = t.ActivateAt,
                AllowedRoleIds = t.AllowedRoles.Select(r => r.RoleId).ToHashSet(),
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
            if (!isConnecting)
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
            if (connectingFromNode == null) return;

            var newConn = new CanvasConnection
            {
                Id = Guid.NewGuid(),
                FromNodeId = connectingFromNode.Id,
                ToNodeId = targetNode.Id,
                SourcePort = activeSourcePort,
                TargetPort = targetPort,
                Name = "", // مقداردهی اولیه برای جلوگیری از خطای بایندینگ در Sidebar
                AllowedRoleIds = new HashSet<int>(), // مقداردهی برای جلوگیری از خطای Null
                CustomFields = new List<CanvasTransitionField>() // مقداردهی برای جلوگیری از خطای Null
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
            errorMessage = ""; // پاک کردن خطاهای قبلی

            if (string.IsNullOrWhiteSpace(workflowName))
            {
                // نمایش ارور شیک در صورت خالی بودن نام
                errorMessage = "لطفاً عنوان جریان کاری را وارد کنید. این فیلد الزامی است.";
                return;
            }

            try
            {
                if (Id.HasValue && Id.Value > 0)
                {
                    // --- حالت ویرایش ---
                    // به جای ساخت شیء جدید، شیء قبلی را ویرایش می‌کنیم تا EF Core دچار تداخل نشود
                    if (currentWorkflow == null)
                        currentWorkflow = await WorkflowRepository.GetWorkflowWithDetailsAsync(Id.Value);

                    if (currentWorkflow != null)
                    {
                        currentWorkflow.Name = workflowName;
                        currentWorkflow.Description = workflowDescription;

                        // ۱. مدیریت وضعیت‌ها (WorkflowStatuses) روی بوم
                        var activeNodeIds = canvasNodes.Select(n => n.Id).ToList();

                        var statusesToRemove = currentWorkflow.WorkflowStatuses.Where(ws => !activeNodeIds.Contains(ws.NodeId)).ToList();
                        foreach (var st in statusesToRemove) currentWorkflow.WorkflowStatuses.Remove(st);

                        foreach (var n in canvasNodes)
                        {
                            // بررسی بر اساس NodeId به جای StatusId
                            var existingWs = currentWorkflow.WorkflowStatuses.FirstOrDefault(ws => ws.NodeId == n.Id);
                            if (existingWs != null)
                            {
                                existingWs.PositionX = n.X;
                                existingWs.PositionY = n.Y;
                                // در صورتی که شناسه وضعیت تغییر کرده باشد
                                existingWs.StatusId = n.Status.Id;
                            }
                            else
                            {
                                currentWorkflow.WorkflowStatuses.Add(new WorkflowStatus
                                {
                                    NodeId = n.Id,
                                    StatusId = n.Status.Id,
                                    PositionX = n.X,
                                    PositionY = n.Y
                                });
                            }
                        }

                        // ۲. مدیریت انتقال‌ها بر اساس شناسه دیتابیس (DbId)
                        var activeUiConnectionDbIds = connections.Where(c => c.DbId > 0).Select(c => c.DbId).ToList();

                        // غیرفعال کردن انتقال‌هایی که روی بوم حذف شده‌اند (Soft Delete)
                        foreach (var dbTransition in currentWorkflow.Transitions)
                        {
                            if (!activeUiConnectionDbIds.Contains(dbTransition.Id))
                            {
                                dbTransition.IsActive = false;
                            }
                        }

                        // بروزرسانی یا افزودن مسیرهای روی بوم
                        foreach (var conn in connections)
                        {
                            var fromStatusId = canvasNodes.First(n => n.Id == conn.FromNodeId).Status.Id;
                            var toStatusId = canvasNodes.First(n => n.Id == conn.ToNodeId).Status.Id;

                            if (conn.DbId > 0)
                            {
                                // مسیر از قبل در دیتابیس وجود دارد -> آپدیت
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
                                    existingDbTransition.IsActive = true;

                                    // آپدیت نقش‌ها به صورت ایمن
                                    var rolesToRemove = existingDbTransition.AllowedRoles.Where(r => !conn.AllowedRoleIds.Contains(r.RoleId)).ToList();
                                    foreach (var r in rolesToRemove) existingDbTransition.AllowedRoles.Remove(r);

                                    var newRoles = conn.AllowedRoleIds.Where(id => !existingDbTransition.AllowedRoles.Any(r => r.RoleId == id));
                                    foreach (var id in newRoles) existingDbTransition.AllowedRoles.Add(new TransitionRole { RoleId = id });

                                    // مدیریت فیلدهای کاستوم
                                    var currentFieldIds = conn.CustomFields.Where(f => f.Id > 0).Select(f => f.Id).ToList();
                                    var fieldsToRemove = existingDbTransition.TransitionFields.Where(f => !currentFieldIds.Contains(f.Id)).ToList();
                                    foreach (var f in fieldsToRemove) existingDbTransition.TransitionFields.Remove(f);

                                    foreach (var f in conn.CustomFields)
                                    {
                                        if (f.FieldTypeId <= 0) continue;

                                        if (f.Id > 0)
                                        {
                                            var existingField = existingDbTransition.TransitionFields.FirstOrDefault(tf => tf.Id == f.Id);
                                            if (existingField != null)
                                            {
                                                existingField.FieldName = f.FieldName;
                                                existingField.FieldTypeId = f.FieldTypeId;
                                                existingField.IsRequired = f.IsRequired;
                                                existingField.SortOrder = f.SortOrder;
                                                existingField.Options = f.Options;
                                                existingField.Placeholder = f.Placeholder;
                                                existingField.DefaultValue = f.DefaultValue;
                                                existingField.IsActive = f.IsActive;
                                            }
                                        }
                                        else
                                        {
                                            existingDbTransition.TransitionFields.Add(new TransitionField
                                            {
                                                FieldTypeId = f.FieldTypeId,
                                                FieldName = f.FieldName,
                                                IsRequired = f.IsRequired,
                                                SortOrder = f.SortOrder,
                                                Options = f.Options,
                                                Placeholder = f.Placeholder,
                                                DefaultValue = f.DefaultValue,
                                                IsActive = f.IsActive
                                            });
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // مسیر جدید است -> درج رکورد جدید در دیتابیس
                                currentWorkflow.Transitions.Add(new Transition
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
                                    AllowedRoles = conn.AllowedRoleIds.Select(roleId => new TransitionRole { RoleId = roleId }).ToList(),
                                    TransitionFields = conn.CustomFields
                                                        .Where(f => f.FieldTypeId > 0)
                                                        .Select(f => new TransitionField
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

                        await WorkflowRepository.UpdateAsync(currentWorkflow);
                    }
                }

                else
                {
                    // --- حالت ساخت جریان کاری جدید ---
                    var workflow = new Workflow
                    {
                        Name = workflowName,
                        Description = workflowDescription,
                        WorkflowStatuses = canvasNodes.Select(n => new WorkflowStatus
                        {
                            NodeId = n.Id, // <--- این خط اضافه شد تا خطای دیتابیس رفع شود
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
                                Name = string.IsNullOrWhiteSpace(c.Name) ? "انتقال" : c.Name,
                                FromState = fromStatusId,
                                ToState = toStatusId,
                                FromNodeId = c.FromNodeId, // <--- اضافه شد برای بارگذاری صحیح در آینده
                                ToNodeId = c.ToNodeId,     // <--- اضافه شد برای بارگذاری صحیح در آینده
                                SourcePort = c.SourcePort,
                                TargetPort = c.TargetPort,
                                IsAutomated = c.IsAutomatic ? 1 : 0,
                                IsActive = c.IsActive,
                                AllowedRoles = c.AllowedRoleIds.Select(roleId => new TransitionRole { RoleId = roleId }).ToList(),
                                TransitionFields = c.CustomFields
                                                    .Where(f => f.FieldTypeId > 0)
                                                    .Select(f => new TransitionField
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

                    await WorkflowRepository.AddAsync(workflow);
                }
                // در صورت موفقیت آمیز بودن، به صفحه لیست برمی‌گردیم
                Navigation.NavigateTo("/workflows");
            }
            catch (Exception ex)
            {
                // اگر خطای دیتابیس یا EF Core رخ دهد، ارور قرمز رنگ بالای صفحه نمایش داده می‌شود
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