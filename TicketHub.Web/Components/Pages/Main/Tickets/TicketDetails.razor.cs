using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;

namespace TicketHub.Web.Components.Pages.Main.Tickets;

public partial class TicketDetails : ComponentBase, IDisposable
{
    [Inject] public ITicketService TicketService { get; set; } = default!;
    [Inject] public ICommentService CommentService { get; set; } = default!;
    [Inject] public IStatusService StatusService { get; set; } = default!;
    [Inject] public ICategoryService CategoryService { get; set; } = default!;
    [Inject] public IPriorityService PriorityService { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IPermissionService PermissionService { get; set; } = default!;
    [Inject] public IWorkflowService WorkflowService { get; set; } = default!;
    [Inject] public IRoleService RoleService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public ITicketEventBroker EventBroker { get; set; } = default!;

    [Parameter] public int TicketId { get; set; }

    protected TicketDto? Ticket { get; set; }
    protected IEnumerable<StatusDto> Statuses { get; set; } = new List<StatusDto>();
    protected IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    protected IEnumerable<PriorityDto> Priorities { get; set; } = new List<PriorityDto>();
    protected List<CommentDto> Comments { get; set; } = new();
    protected List<TicketHistoryDto> Transitions { get; set; } = new();
    protected List<AttachmentDto> AllAttachments { get; set; } = new();

    protected TicketTransitionModal TransitionModal { get; set; } = default!;

    protected bool CanEdit { get; set; } = false;
    protected int CurrentUserId { get; set; } = 0;
    protected bool HasFullAccess { get; set; } = false;
    protected bool HasAvailableTransitions { get; set; } = false;

    protected bool IsEditingTitle { get; set; } = false;
    protected string TitleValue { get; set; } = string.Empty;

    protected bool IsEditingDescription { get; set; } = false;
    protected string DescriptionValue { get; set; } = string.Empty;

    protected string NewCommentContent { get; set; } = string.Empty;
    protected bool IsSubmittingComment { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Ticket = await TicketService.GetByIdAsync(TicketId);
        }
        catch (ForbiddenException ex)
        {
            ToastService.ShowWarning(ex.Message, "عدم دسترسی");
            Navigation.NavigateTo("/tickets");
            return;
        }
        catch (NotFoundException ex)
        {
            ToastService.ShowWarning(ex.Message, "یافت نشد");
            Navigation.NavigateTo("/tickets");
            return;
        }
        catch (Exception ex)
        {
            ToastService.ShowError("خطا در بارگذاری تیکت: " + ex.Message);
            Navigation.NavigateTo("/tickets");
            return;
        }

        if (Ticket == null)
        {
            Navigation.NavigateTo("/tickets");
            return;
        }

        Statuses = await StatusService.GetAllAsync();
        Categories = await CategoryService.GetAllAsync();
        Priorities = await PriorityService.GetAllAsync();

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value;
        CurrentUserId = int.TryParse(userIdString, out var id) ? id : 0;

        HasFullAccess = await PermissionService.HasAccessAsync(user, "/tickets", PermissionType.Full);
        bool isSender = (CurrentUserId > 0 && Ticket.UserId == CurrentUserId);
        CanEdit = isSender || HasFullAccess;

        Comments = await CommentService.GetCommentsByTicketIdAsync(TicketId);
        Transitions = await TicketService.GetTransitionsByTicketIdAsync(TicketId);

        await CheckAvailableTransitionsAsync();

        LoadAttachments();

        EventBroker.OnCommentAdded += HandleCommentAddedAsync;
        EventBroker.OnCommentDeleted += HandleCommentDeletedAsync;
        EventBroker.OnTicketUpdated += HandleTicketUpdatedAsync;
        EventBroker.OnTransitionOccurred += HandleTransitionOccurredAsync;
    }

    private void LoadAttachments()
    {
        AllAttachments.Clear();

        if (Ticket != null)
        {
            if (Ticket.Attachments != null && Ticket.Attachments.Any())
            {
                AllAttachments.AddRange(Ticket.Attachments);
            }

            if (Ticket.FieldValues != null)
            {
                foreach (var fv in Ticket.FieldValues)
                {
                    if (fv.Attachments != null && fv.Attachments.Any())
                    {
                        AllAttachments.AddRange(fv.Attachments);
                    }
                }
            }

            AllAttachments = AllAttachments.DistinctBy(a => a.Id).ToList();
        }
    }

    private async Task HandleCommentAddedAsync(int tId, CommentDto commentDto)
    {
        if (tId == TicketId)
        {
            await InvokeAsync(() =>
            {
                if (!Comments.Any(c => c.Id == commentDto.Id))
                {
                    Comments.Add(commentDto);
                    StateHasChanged();
                }
            });
        }
    }

    private async Task HandleCommentDeletedAsync(int tId, int commentId)
    {
        if (tId == TicketId)
        {
            await InvokeAsync(() =>
            {
                Comments.RemoveAll(c => c.Id == commentId);
                StateHasChanged();
            });
        }
    }

    private async Task HandleTicketUpdatedAsync(int tId)
    {
        if (tId == TicketId)
        {
            await InvokeAsync(async () =>
            {
                Ticket = await TicketService.GetByIdAsync(TicketId);
                LoadAttachments();
                await CheckAvailableTransitionsAsync();
                StateHasChanged();
            });
        }
    }

    private async Task HandleTransitionOccurredAsync(int tId)
    {
        if (tId == TicketId)
        {
            await InvokeAsync(async () =>
            {
                Ticket = await TicketService.GetByIdAsync(TicketId);
                Transitions = await TicketService.GetTransitionsByTicketIdAsync(TicketId);
                await CheckAvailableTransitionsAsync();
                StateHasChanged();
            });
        }
    }

    private async Task CheckAvailableTransitionsAsync()
    {
        HasAvailableTransitions = false;
        if (Ticket?.Project == null || !Ticket.Project.WorkflowId.HasValue) return;

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

            var workflow = await WorkflowService.GetByIdWithDetailsAsync(Ticket.Project.WorkflowId.Value);
            if (workflow != null && workflow.Transitions != null && workflow.WorkflowStatuses != null)
            {
                WorkflowStatusDto? currentWorkflowStatus = null;
                if (Ticket.WorkflowStatusId.HasValue && Ticket.WorkflowStatusId.Value > 0)
                {
                    currentWorkflowStatus = workflow.WorkflowStatuses
                        .FirstOrDefault(ws => ws.Id == Ticket.WorkflowStatusId.Value);
                }

                if (currentWorkflowStatus == null)
                {
                    currentWorkflowStatus = workflow.WorkflowStatuses
                        .FirstOrDefault(ws => ws.StatusId == Ticket.StatusId);
                }

                if (currentWorkflowStatus != null)
                {
                    HasAvailableTransitions = workflow.Transitions
                        .Where(t => t.IsActive)
                        .Where(t => t.FromState == currentWorkflowStatus.Id)
                        .Any(t => isAdmin
                                 || !t.AllowedRoleIds.Any()
                                 || t.AllowedRoleIds.Any(roleId => userRoleIds.Contains(roleId)));
                }
            }
        }
        catch
        {
            HasAvailableTransitions = false;
        }
    }

    public void Dispose()
    {
        EventBroker.OnCommentAdded -= HandleCommentAddedAsync;
        EventBroker.OnCommentDeleted -= HandleCommentDeletedAsync;
        EventBroker.OnTicketUpdated -= HandleTicketUpdatedAsync;
        EventBroker.OnTransitionOccurred -= HandleTransitionOccurredAsync;
    }

    protected void EnableTitleEdit()
    {
        if (Ticket == null) return;
        TitleValue = Ticket.Title;
        IsEditingTitle = true;
    }

    protected async Task SaveTitleAsync()
    {
        if (Ticket == null || string.IsNullOrWhiteSpace(TitleValue)) return;

        try
        {
            Ticket.Title = TitleValue.Trim();
            await TicketService.UpdateAsync(Ticket);
            Ticket = await TicketService.GetByIdAsync(TicketId);
            IsEditingTitle = false;
            ToastService.ShowSuccess("عنوان تیکت با موفقیت ویرایش شد.");
            StateHasChanged();
        }
        catch (ConcurrencyException ex)
        {
            ToastService.ShowWarning(ex.Message, "تداخل همزمانی");
            Ticket = await TicketService.GetByIdAsync(TicketId);
            IsEditingTitle = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError("خطا در ویرایش عنوان: " + ex.Message);
        }
    }

    protected void EnableDescriptionEdit()
    {
        if (Ticket == null) return;
        DescriptionValue = Ticket.Description;
        IsEditingDescription = true;
    }

    protected async Task SaveDescriptionAsync()
    {
        if (Ticket == null || string.IsNullOrWhiteSpace(DescriptionValue)) return;

        try
        {
            Ticket.Description = DescriptionValue.Trim();
            await TicketService.UpdateAsync(Ticket);
            Ticket = await TicketService.GetByIdAsync(TicketId);
            IsEditingDescription = false;
            ToastService.ShowSuccess("توضیحات تیکت با موفقیت ویرایش شد.");
            StateHasChanged();
        }
        catch (ConcurrencyException ex)
        {
            ToastService.ShowWarning(ex.Message, "تداخل همزمانی");
            Ticket = await TicketService.GetByIdAsync(TicketId);
            IsEditingDescription = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError("خطا در ویرایش توضیحات: " + ex.Message);
        }
    }

    protected async Task UpdatePriorityAsync(ChangeEventArgs e)
    {
        if (Ticket == null || e.Value == null) return;

        try
        {
            if (int.TryParse(e.Value.ToString(), out int priId))
            {
                Ticket.PriorityId = priId == 0 ? null : priId;
                await TicketService.UpdateAsync(Ticket);
                Ticket = await TicketService.GetByIdAsync(TicketId);
                ToastService.ShowSuccess("اولویت تیکت بروزرسانی شد.");
                StateHasChanged();
            }
        }
        catch (ConcurrencyException ex)
        {
            ToastService.ShowWarning(ex.Message, "تداخل همزمانی");
            Ticket = await TicketService.GetByIdAsync(TicketId);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError("خطا در بروزرسانی اولویت: " + ex.Message);
        }
    }

    protected async Task UpdateStatusAsync(ChangeEventArgs e)
    {
        if (Ticket == null || e.Value == null) return;

        try
        {
            if (int.TryParse(e.Value.ToString(), out int statusId))
            {
                Ticket.StatusId = statusId;
                await TicketService.UpdateAsync(Ticket);
                Ticket = await TicketService.GetByIdAsync(TicketId);
                ToastService.ShowSuccess("وضعیت تیکت بروزرسانی شد.");
                StateHasChanged();
            }
        }
        catch (ConcurrencyException ex)
        {
            ToastService.ShowWarning(ex.Message, "تداخل همزمانی");
            Ticket = await TicketService.GetByIdAsync(TicketId);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError("خطا در بروزرسانی وضعیت: " + ex.Message);
        }
    }

    protected async Task DeleteAttachmentAsync(int attachmentId)
    {
        try
        {
            await TicketService.DeleteAttachmentAsync(attachmentId, CurrentUserId, HasFullAccess);
            Ticket = await TicketService.GetByIdAsync(TicketId);
            LoadAttachments();
            ToastService.ShowSuccess("فایل ضمیمه با موفقیت حذف شد.");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
    }

    protected async Task UploadFilesAsync(InputFileChangeEventArgs e)
    {
        if (Ticket == null) return;

        try
        {
            foreach (var file in e.GetMultipleFiles(5))
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 15 * 1024 * 1024);
                await TicketService.UploadTicketAttachmentAsync(TicketId, null, stream, file.Name, file.ContentType);
            }

            Ticket = await TicketService.GetByIdAsync(TicketId);
            LoadAttachments();
            ToastService.ShowSuccess("فایل(های) جدید با موفقیت بارگذاری شد.");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError("خطا در بارگذاری فایل: " + ex.Message);
        }
    }

    protected async Task AddCommentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCommentContent)) return;

        try
        {
            IsSubmittingComment = true;
            await CommentService.AddCommentAsync(new CommentDto
            {
                TicketId = TicketId,
                Content = NewCommentContent
            }, CurrentUserId);

            NewCommentContent = string.Empty;
            Comments = await CommentService.GetCommentsByTicketIdAsync(TicketId);
            ToastService.ShowSuccess("نظر شما با موفقیت ثبت شد.");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
        finally
        {
            IsSubmittingComment = false;
        }
    }

    protected async Task DeleteCommentAsync(int commentId)
    {
        try
        {
            await CommentService.DeleteCommentAsync(commentId, CurrentUserId, HasFullAccess);
            Comments = await CommentService.GetCommentsByTicketIdAsync(TicketId);
            ToastService.ShowSuccess("نظر با موفقیت حذف شد.");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
    }

    protected async Task OpenTransitionModal()
    {
        if (Ticket != null && Ticket.Project != null && Ticket.Project.WorkflowId.HasValue)
        {
            await TransitionModal.OpenAsync(Ticket.Id, Ticket.Title, Ticket.StatusId, Ticket.Project.WorkflowId.Value, Ticket.WorkflowStatusId, Ticket.RowVersion);
        }
    }

    protected async Task HandleTransitionSaved()
    {
        Ticket = await TicketService.GetByIdAsync(TicketId);
        Transitions = await TicketService.GetTransitionsByTicketIdAsync(TicketId);
        StateHasChanged();
    }

    protected void GoBack()
    {
        Navigation.NavigateTo("/tickets");
    }

    protected string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "ک";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][0].ToString();
        return $"{parts[0][0]}{parts[^1][0]}";
    }
}
