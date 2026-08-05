using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common;
using TicketHub.Core.Common.Exceptions;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record WorkflowEditorState(
    bool IsLoading,
    bool IsSaving,
    List<StatusDto> AvailableStatuses,
    List<RoleDto> AvailableRoles,
    List<FieldTypeDto> AvailableFieldTypes,
    string? ErrorMessage)
{
    private WorkflowEditorState() : this(true, false, new(), new(), new(), null) { }
}

// 2. Actions
public record LoadEditorDataAction(int? WorkflowId);
public record EditorDataLoadedAction(
    List<StatusDto> Statuses,
    List<RoleDto> Roles,
    List<FieldTypeDto> Fields,
    WorkflowDto? CurrentWorkflow,
    string Name,
    string Description,
    List<CanvasNodeDto> Nodes,
    List<CanvasConnection> Connections);
public record SaveWorkflowEditorAction(
    int? WorkflowId,
    WorkflowDto? CurrentWorkflow,
    string Name,
    string Description,
    List<CanvasNodeDto> Nodes,
    List<CanvasConnection> Connections);
public record SaveWorkflowEditorSuccessAction();
public record SaveWorkflowEditorFailedAction(string ErrorMessage);
public record ClearEditorErrorAction();

// 3. Reducers
public static class WorkflowEditorReducers
{
    [ReducerMethod]
    public static WorkflowEditorState ReduceLoadData(WorkflowEditorState state, LoadEditorDataAction action) => state with { IsLoading = true, ErrorMessage = null };

    [ReducerMethod]
    public static WorkflowEditorState ReduceDataLoaded(WorkflowEditorState state, EditorDataLoadedAction action) =>
        state with { IsLoading = false, AvailableStatuses = action.Statuses, AvailableRoles = action.Roles, AvailableFieldTypes = action.Fields };

    [ReducerMethod]
    public static WorkflowEditorState ReduceSaveData(WorkflowEditorState state, SaveWorkflowEditorAction action) => state with { IsSaving = true, ErrorMessage = null };

    [ReducerMethod(typeof(SaveWorkflowEditorSuccessAction))]
    public static WorkflowEditorState ReduceSaveSuccess(WorkflowEditorState state) => state with { IsSaving = false, ErrorMessage = null };

    [ReducerMethod]
    public static WorkflowEditorState ReduceSaveFailed(WorkflowEditorState state, SaveWorkflowEditorFailedAction action) => state with { IsSaving = false, ErrorMessage = action.ErrorMessage };

    [ReducerMethod(typeof(ClearEditorErrorAction))]
    public static WorkflowEditorState ReduceClearError(WorkflowEditorState state) => state with { ErrorMessage = null };
}

// 4. Effects
public class WorkflowEditorEffects
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowEditorEffects> _logger;
    private readonly IToastService _toastService;

    public WorkflowEditorEffects(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowEditorEffects> logger,
        IToastService toastService)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _toastService = toastService;
    }

    [EffectMethod]
    public async Task HandleLoadData(LoadEditorDataAction action, IDispatcher dispatcher)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var _workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();
            var _statusService = scope.ServiceProvider.GetRequiredService<IStatusService>();
            var _roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
            var _fieldTypeService = scope.ServiceProvider.GetRequiredService<IFieldTypeService>();

            var statuses = (await _statusService.GetAllAsync()).ToList();
            var roles = (await _roleService.GetAllRolesAsync()).ToList();
            var fields = (await _fieldTypeService.GetAllAsync()).ToList();

            WorkflowDto? currentWf = null;
            string name = "", desc = "";
            List<CanvasNodeDto> nodes = new();
            List<CanvasConnection> connections = new();

            if (action.WorkflowId.HasValue)
            {
                currentWf = await _workflowService.GetByIdWithDetailsAsync(action.WorkflowId.Value);
                if (currentWf != null)
                {
                    name = currentWf.Name ?? "";
                    desc = currentWf.Description ?? "";

                    nodes = currentWf.WorkflowStatuses.Select(ws => new CanvasNodeDto
                    {
                        Id = ws.NodeId != Guid.Empty ? ws.NodeId : Guid.NewGuid(),
                        Status = ws.Status!,
                        X = ws.PositionX,
                        Y = ws.PositionY
                    }).ToList();

                    connections = currentWf.Transitions
                        .Select(t => new CanvasConnection
                        {
                            Id = Guid.NewGuid(),
                            DbId = t.Id,
                            FromNodeId = t.FromNodeId != Guid.Empty ? t.FromNodeId : nodes.FirstOrDefault(n => n.Status.Id == t.FromState)?.Id ?? Guid.Empty,
                            ToNodeId = t.ToNodeId != Guid.Empty ? t.ToNodeId : nodes.FirstOrDefault(n => n.Status.Id == t.ToState)?.Id ?? Guid.Empty,
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
            dispatcher.Dispatch(new EditorDataLoadedAction(statuses, roles, fields, currentWf, name, desc, nodes, connections));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در بارگذاری اطلاعات ویرایشگر جریان کاری.");
        }
    }

    [EffectMethod]
    public async Task HandleSaveWorkflow(SaveWorkflowEditorAction action, IDispatcher dispatcher)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var _workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();

            if (action.WorkflowId.HasValue && action.WorkflowId.Value > 0)
            {
                var wf = action.CurrentWorkflow ?? await _workflowService.GetByIdWithDetailsAsync(action.WorkflowId.Value);
                if (wf != null)
                {
                    wf.Name = action.Name;
                    wf.Description = action.Description;

                    var activeNodeIds = action.Nodes.Select(n => n.Id).ToList();
                    var statusesToRemove = wf.WorkflowStatuses.Where(ws => !activeNodeIds.Contains(ws.NodeId)).ToList();
                    foreach (var st in statusesToRemove) wf.WorkflowStatuses.Remove(st);

                    foreach (var n in action.Nodes)
                    {
                        var existingWs = wf.WorkflowStatuses.FirstOrDefault(ws => ws.NodeId == n.Id);
                        if (existingWs != null) { existingWs.PositionX = n.X; existingWs.PositionY = n.Y; existingWs.StatusId = n.Status.Id; }
                        else { wf.WorkflowStatuses.Add(new WorkflowStatusDto { NodeId = n.Id, StatusId = n.Status.Id, PositionX = n.X, PositionY = n.Y }); }
                    }

                    var activeUiConnectionDbIds = action.Connections.Where(c => c.DbId > 0).Select(c => c.DbId).ToList();
                    var transitionsToRemove = wf.Transitions.Where(t => !activeUiConnectionDbIds.Contains(t.Id)).ToList();
                    foreach (var t in transitionsToRemove) wf.Transitions.Remove(t);

                    foreach (var conn in action.Connections)
                    {
                        var fromStatusId = action.Nodes.First(n => n.Id == conn.FromNodeId).Status.Id;
                        var toStatusId = action.Nodes.First(n => n.Id == conn.ToNodeId).Status.Id;

                        if (conn.DbId > 0)
                        {
                            var dbTrans = wf.Transitions.FirstOrDefault(t => t.Id == conn.DbId);
                            if (dbTrans != null)
                            {
                                dbTrans.Name = string.IsNullOrWhiteSpace(conn.Name) ? "انتقال" : conn.Name;
                                dbTrans.SourcePort = conn.SourcePort; dbTrans.TargetPort = conn.TargetPort;
                                dbTrans.FromNodeId = conn.FromNodeId; dbTrans.ToNodeId = conn.ToNodeId;
                                dbTrans.FromState = fromStatusId; dbTrans.ToState = toStatusId;
                                dbTrans.IsAutomated = conn.IsAutomatic ? 1 : 0;
                                dbTrans.IsActive = conn.IsActive;
                                dbTrans.ActivateAt = conn.ActivateAt;
                                dbTrans.AllowedRoleIds = conn.AllowedRoleIds.ToList();
                                dbTrans.TransitionFields = conn.CustomFields.Where(f => f.FieldTypeId > 0).Select(f => new TransitionFieldDto
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
                            wf.Transitions.Add(new TransitionDto
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
                                ActivateAt = conn.ActivateAt,
                                AllowedRoleIds = conn.AllowedRoleIds.ToList(),
                                TransitionFields = conn.CustomFields.Where(f => f.FieldTypeId > 0).Select(f => new TransitionFieldDto
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
                    await _workflowService.UpdateAsync(wf);
                }
            }
            else
            {
                var workflow = new WorkflowDto
                {
                    Name = action.Name,
                    Description = action.Description,
                    WorkflowStatuses = action.Nodes.Select(n => new WorkflowStatusDto { NodeId = n.Id, StatusId = n.Status.Id, PositionX = n.X, PositionY = n.Y }).ToList(),
                    Transitions = action.Connections.Select(c =>
                    {
                        var fromStatusId = action.Nodes.First(n => n.Id == c.FromNodeId).Status.Id;
                        var toStatusId = action.Nodes.First(n => n.Id == c.ToNodeId).Status.Id;
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
                            ActivateAt = c.ActivateAt,
                            AllowedRoleIds = c.AllowedRoleIds.ToList(),
                            TransitionFields = c.CustomFields.Where(f => f.FieldTypeId > 0).Select(f => new TransitionFieldDto
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

            dispatcher.Dispatch(new SaveWorkflowEditorSuccessAction());
            _toastService.ShowSuccess("جریان کاری با موفقیت ذخیره شد.");
        }
        catch (ValidationException ex)
        {
            var errorMessage = string.Join(" | ", ex.Errors.SelectMany(e => e.Value));
            _toastService.ShowWarning(errorMessage, "خطای اطلاعات ورودی");
            dispatcher.Dispatch(new SaveWorkflowEditorFailedAction(errorMessage));
        }
        catch (TicketHubException ex)
        {
            _toastService.ShowWarning(ex.Message, "توجه");
            dispatcher.Dispatch(new SaveWorkflowEditorFailedAction(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای سیستمی غیرمنتظره در ذخیره‌سازی جریان کاری.");
            var fallbackError = "یک خطای سیستمی رخ داد. لطفاً دوباره تلاش کنید.";
            _toastService.ShowError(fallbackError);
            dispatcher.Dispatch(new SaveWorkflowEditorFailedAction(fallbackError));
        }
    }
}