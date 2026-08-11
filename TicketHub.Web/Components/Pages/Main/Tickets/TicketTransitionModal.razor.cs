using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Web.Components.Pages.Main.Tickets;

public partial class TicketTransitionModal : ComponentBase
{
    [Inject] public ITicketService TicketService { get; set; } = default!;
    [Inject] public IWorkflowRepository WorkflowRepository { get; set; } = default!;
    [Inject] public IRoleService RoleService { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IValidator<ExecuteTransitionDto> Validator { get; set; } = default!;

    [Parameter] public EventCallback OnSaved { get; set; }

    public bool IsVisible { get; private set; }
    protected bool IsLoading { get; set; } = false;
    protected bool IsSubmitting { get; set; } = false;

    protected string TicketTitle { get; set; } = "";
    protected int TicketId { get; set; }
    protected int CurrentStatusId { get; set; }

    protected ExecuteTransitionDto Model { get; set; } = new();
    protected List<Transition> AvailableTransitions { get; set; } = new();
    protected List<DynamicFieldModel> DynamicFields { get; set; } = new();

    protected List<string> ValidationErrors { get; set; } = new();
    protected Dictionary<string, string> FieldValidationErrors { get; set; } = new();

    public async Task OpenAsync(int ticketId, string title, int currentStatusId, int workflowId, int? currentWorkflowStatusId = null)
    {
        IsVisible = true;
        IsLoading = true;
        ValidationErrors.Clear();
        FieldValidationErrors.Clear();
        
        TicketId = ticketId;
        TicketTitle = title;
        CurrentStatusId = currentStatusId;

        Model = new ExecuteTransitionDto
        {
            TicketId = ticketId
        };
        DynamicFields = new List<DynamicFieldModel>();
        AvailableTransitions = new List<Transition>();

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userRoles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            bool isAdmin = userRoles.Any(r => r == "مدیر سیستم" || r == "ادمین");

            List<int> userRoleIds = new();
            if (!isAdmin && userRoles.Any())
            {
                var allRoles = await RoleService.GetAllRolesAsync();
                userRoleIds = allRoles
                    .Where(r => userRoles.Contains(r.Name))
                    .Select(r => r.Id)
                    .ToList();
            }

            var workflow = await WorkflowRepository.GetWorkflowWithDetailsAsync(workflowId);
            if (workflow != null && workflow.Transitions != null && workflow.WorkflowStatuses != null)
            {
                WorkflowStatus? currentWorkflowStatus = null;
                if (currentWorkflowStatusId.HasValue && currentWorkflowStatusId.Value > 0)
                {
                    currentWorkflowStatus = workflow.WorkflowStatuses
                        .FirstOrDefault(ws => ws.Id == currentWorkflowStatusId.Value);
                }

                if (currentWorkflowStatus == null)
                {
                    currentWorkflowStatus = workflow.WorkflowStatuses
                        .FirstOrDefault(ws => ws.StatusId == currentStatusId);
                }

                if (currentWorkflowStatus != null)
                {
                    AvailableTransitions = workflow.Transitions
                        .Where(t => t.IsActive)
                        .Where(t => t.FromState == currentWorkflowStatus.Id)
                        .Where(t => isAdmin 
                                 || !t.AllowedRoles.Any() 
                                 || t.AllowedRoles.Any(ar => userRoleIds.Contains(ar.RoleId)))
                        .ToList();
                }
            }
        }
        catch
        {
            ToastService.ShowError("خطا در دریافت لیست عملیات‌ها");
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    public Task CloseAsync()
    {
        IsVisible = false;
        return Task.CompletedTask;
    }

    protected void SelectTransition(int transitionId)
    {
        if (Model.TransitionId == transitionId) return;

        Model.TransitionId = transitionId;
        ValidationErrors.Clear();
        FieldValidationErrors.Clear();

        var selectedTransition = AvailableTransitions.FirstOrDefault(t => t.Id == transitionId);
        if (selectedTransition != null && selectedTransition.TransitionFields != null)
        {
            DynamicFields = selectedTransition.TransitionFields
                .Where(f => f.IsActive)
                .OrderBy(f => f.SortOrder)
                .Select(f => new DynamicFieldModel
                {
                    OriginalFieldId = f.Id,
                    Name = f.FieldName,
                    Placeholder = f.Placeholder ?? "",
                    SortOrder = f.SortOrder,
                    DefaultValue = f.DefaultValue,
                    FieldTypeId = f.FieldTypeId,
                    Options = f.Options,
                    IsRequired = f.IsRequired,
                    Value = f.DefaultValue ?? "",
                    PendingUploads = new()
                }).ToList();
        }
        else
        {
            DynamicFields = new List<DynamicFieldModel>();
        }
    }

    protected async Task HandleValidSubmitAsync()
    {
        if (IsSubmitting) return;

        ValidationErrors.Clear();
        FieldValidationErrors.Clear();

        Model.FieldValues = DynamicFields.Select(f => new TransitionFieldValueDto
        {
            TransitionFieldId = f.OriginalFieldId,
            Value = f.Value,
            PendingUploads = f.PendingUploads,
            IsRequired = f.IsRequired,
            FieldName = f.Name
        }).ToList();

        var validationResult = await Validator.ValidateAsync(Model);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                if (error.PropertyName.StartsWith("FieldValues["))
                {
                    var startIndex = error.PropertyName.IndexOf('[') + 1;
                    var endIndex = error.PropertyName.IndexOf(']');
                    if (startIndex > 0 && endIndex > startIndex && int.TryParse(error.PropertyName.Substring(startIndex, endIndex - startIndex), out int index))
                    {
                        if (index >= 0 && index < DynamicFields.Count)
                        {
                            var fieldName = DynamicFields[index].Name;
                            FieldValidationErrors[fieldName] = error.ErrorMessage;
                        }
                    }
                }
                else
                {
                    ValidationErrors.Add(error.ErrorMessage);
                }
            }
            return;
        }

        try
        {
            IsSubmitting = true;
            StateHasChanged();
            await Task.Yield();
            
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userIdStr = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                await TicketService.ExecuteTransitionAsync(Model, userId);
                ToastService.ShowSuccess("عملیات با موفقیت انجام شد.");
                await CloseAsync();
                if (OnSaved.HasDelegate)
                {
                    await OnSaved.InvokeAsync();
                }
            }
            else
            {
                ValidationErrors.Add("کاربر نامعتبر است.");
            }
        }
        catch (Exception ex)
        {
            ValidationErrors.Add(ex.Message);
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
