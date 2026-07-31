using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common;
using TicketHub.Web.States;

namespace TicketHub.Web.Facades
{
    public class WorkflowEditorFacade
    {
        private readonly IWorkflowService _workflowService;
        private readonly IStatusService _statusService;
        private readonly IRoleService _roleService;
        private readonly IFieldTypeService _fieldTypeService;
        private readonly WorkflowEditorState _state;

        public WorkflowEditorFacade(
            IWorkflowService workflowService,
            IStatusService statusService,
            IRoleService roleService,
            IFieldTypeService fieldTypeService,
            WorkflowEditorState state)
        {
            _workflowService = workflowService;
            _statusService = statusService;
            _roleService = roleService;
            _fieldTypeService = fieldTypeService;
            _state = state;
        }

        public async Task InitializeAsync(int? workflowId)
        {
            _state.AvailableStatuses = (await _statusService.GetAllAsync()).ToList();
            _state.AvailableRoles = (await _roleService.GetAllRolesAsync()).ToList();
            _state.AvailableFieldTypes = (await _fieldTypeService.GetAllAsync()).ToList();

            if (workflowId.HasValue)
            {
                _state.CurrentWorkflow = await _workflowService.GetByIdWithDetailsAsync(workflowId.Value);
                if (_state.CurrentWorkflow != null)
                {
                    _state.WorkflowName = _state.CurrentWorkflow.Name ?? "";
                    _state.WorkflowDescription = _state.CurrentWorkflow.Description ?? "";

                    _state.CanvasNodes = _state.CurrentWorkflow.WorkflowStatuses.Select(ws => new CanvasNodeDto
                    {
                        Id = ws.NodeId != Guid.Empty ? ws.NodeId : Guid.NewGuid(),
                        Status = ws.Status!,
                        X = ws.PositionX,
                        Y = ws.PositionY
                    }).ToList();

                    _state.Connections = _state.CurrentWorkflow.Transitions
                        .Select(t => new CanvasConnection
                        {
                            Id = Guid.NewGuid(),
                            DbId = t.Id,
                            FromNodeId = t.FromNodeId != Guid.Empty ? t.FromNodeId : _state.CanvasNodes.FirstOrDefault(n => n.Status.Id == t.FromState)?.Id ?? Guid.Empty,
                            ToNodeId = t.ToNodeId != Guid.Empty ? t.ToNodeId : _state.CanvasNodes.FirstOrDefault(n => n.Status.Id == t.ToState)?.Id ?? Guid.Empty,
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
            }
            _state.NotifyStateChanged();
        }

        public async Task<bool> SaveWorkflowAsync(int? id)
        {
            _state.ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(_state.WorkflowName))
            {
                _state.ErrorMessage = "لطفاً عنوان جریان کاری را وارد کنید. این فیلد الزامی است.";
                _state.NotifyStateChanged();
                return false;
            }

            try
            {
                if (id.HasValue && id.Value > 0)
                {
                    if (_state.CurrentWorkflow == null)
                        _state.CurrentWorkflow = await _workflowService.GetByIdWithDetailsAsync(id.Value);

                    if (_state.CurrentWorkflow != null)
                    {
                        _state.CurrentWorkflow.Name = _state.WorkflowName;
                        _state.CurrentWorkflow.Description = _state.WorkflowDescription;

                        var activeNodeIds = _state.CanvasNodes.Select(n => n.Id).ToList();
                        var statusesToRemove = _state.CurrentWorkflow.WorkflowStatuses.Where(ws => !activeNodeIds.Contains(ws.NodeId)).ToList();
                        foreach (var st in statusesToRemove) _state.CurrentWorkflow.WorkflowStatuses.Remove(st);

                        foreach (var n in _state.CanvasNodes)
                        {
                            var existingWs = _state.CurrentWorkflow.WorkflowStatuses.FirstOrDefault(ws => ws.NodeId == n.Id);
                            if (existingWs != null)
                            {
                                existingWs.PositionX = n.X;
                                existingWs.PositionY = n.Y;
                                existingWs.StatusId = n.Status.Id;
                            }
                            else
                            {
                                _state.CurrentWorkflow.WorkflowStatuses.Add(new WorkflowStatusDto
                                {
                                    NodeId = n.Id,
                                    StatusId = n.Status.Id,
                                    PositionX = n.X,
                                    PositionY = n.Y
                                });
                            }
                        }

                        var activeUiConnectionDbIds = _state.Connections.Where(c => c.DbId > 0).Select(c => c.DbId).ToList();
                        var transitionsToRemoveFromDto = _state.CurrentWorkflow.Transitions
                            .Where(t => !activeUiConnectionDbIds.Contains(t.Id))
                            .ToList();

                        foreach (var t in transitionsToRemoveFromDto) _state.CurrentWorkflow.Transitions.Remove(t);

                        foreach (var conn in _state.Connections)
                        {
                            var fromStatusId = _state.CanvasNodes.First(n => n.Id == conn.FromNodeId).Status.Id;
                            var toStatusId = _state.CanvasNodes.First(n => n.Id == conn.ToNodeId).Status.Id;

                            if (conn.DbId > 0)
                            {
                                var existingDbTransition = _state.CurrentWorkflow.Transitions.FirstOrDefault(t => t.Id == conn.DbId);
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
                                _state.CurrentWorkflow.Transitions.Add(new TransitionDto
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
                        await _workflowService.UpdateAsync(_state.CurrentWorkflow);
                    }
                }
                else
                {
                    var workflow = new WorkflowDto
                    {
                        Name = _state.WorkflowName,
                        Description = _state.WorkflowDescription,
                        WorkflowStatuses = _state.CanvasNodes.Select(n => new WorkflowStatusDto
                        {
                            NodeId = n.Id,
                            StatusId = n.Status.Id,
                            PositionX = n.X,
                            PositionY = n.Y
                        }).ToList(),
                        Transitions = _state.Connections.Select(c =>
                        {
                            var fromStatusId = _state.CanvasNodes.First(n => n.Id == c.FromNodeId).Status.Id;
                            var toStatusId = _state.CanvasNodes.First(n => n.Id == c.ToNodeId).Status.Id;

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
                    await _workflowService.CreateAsync(workflow);
                }

                return true;
            }
            catch (Exception ex)
            {
                _state.ErrorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _state.NotifyStateChanged();
                return false;
            }
        }
    }
}