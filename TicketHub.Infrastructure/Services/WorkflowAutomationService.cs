using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Infrastructure.Services;

public class WorkflowAutomationService : IWorkflowAutomationService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketEventBroker _eventBroker;
    private readonly ILogger<WorkflowAutomationService> _logger;
    private readonly INotificationService? _notificationService;

    public WorkflowAutomationService(
        IDbContextFactory<AppDbContext> factory,
        ITicketRepository ticketRepository,
        ITicketEventBroker eventBroker,
        ILogger<WorkflowAutomationService> logger,
        INotificationService? notificationService = null)
    {
        _factory = factory;
        _ticketRepository = ticketRepository;
        _eventBroker = eventBroker;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task ProcessAutomaticTransitionsAsync()
    {
        try
        {
            using var context = await _factory.CreateDbContextAsync();

            var eligibleTickets = await context.Set<Ticket>()
                .Include(t => t.Project)
                    .ThenInclude(p => p.Workflow)
                        .ThenInclude(w => w.Transitions)
                            .ThenInclude(tr => tr.FromStatus)
                                .ThenInclude(fs => fs.Status)
                .Include(t => t.Project)
                    .ThenInclude(p => p.Workflow)
                        .ThenInclude(w => w.Transitions)
                            .ThenInclude(tr => tr.ToStatus)
                                .ThenInclude(ts => ts.Status)
                .Include(t => t.Status)
                .Where(t => t.WorkflowStatusId != null && t.Project != null && t.Project.Workflow != null && t.Project.Workflow.IsActive)
                .AsSplitQuery()
                .ToListAsync();

            if (!eligibleTickets.Any()) return;

            var now = DateTime.UtcNow;

            foreach (var ticket in eligibleTickets)
            {
                var workflow = ticket.Project?.Workflow;
                if (workflow == null) continue;

                var automaticTransition = workflow.Transitions.FirstOrDefault(tr =>
                    tr.IsActive &&
                    tr.IsAutomated == 1 &&
                    tr.FromState == ticket.WorkflowStatusId &&
                    (tr.ActivateAt == null || tr.ActivateAt.Value <= now));

                if (automaticTransition != null)
                {
                    await ExecuteAutomatedTransitionInternalAsync(ticket, automaticTransition);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در پردازش پس‌زمینه انتقالات خودکار جریان کاری.");
        }
    }

    public async Task TriggerImmediateAutomaticTransitionsAsync(int ticketId, int maxDepth = 5)
    {
        if (maxDepth <= 0) return;

        try
        {
            using var context = await _factory.CreateDbContextAsync();
            var ticket = await context.Set<Ticket>()
                .Include(t => t.Project)
                    .ThenInclude(p => p.Workflow)
                        .ThenInclude(w => w.Transitions)
                            .ThenInclude(tr => tr.FromStatus)
                                .ThenInclude(fs => fs.Status)
                .Include(t => t.Project)
                    .ThenInclude(p => p.Workflow)
                        .ThenInclude(w => w.Transitions)
                            .ThenInclude(tr => tr.ToStatus)
                                .ThenInclude(ts => ts.Status)
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null || ticket.WorkflowStatusId == null || ticket.Project?.Workflow == null || !ticket.Project.Workflow.IsActive)
                return;

            var now = DateTime.UtcNow;
            var immediateTransition = ticket.Project.Workflow.Transitions.FirstOrDefault(tr =>
                tr.IsActive &&
                tr.IsAutomated == 1 &&
                tr.FromState == ticket.WorkflowStatusId &&
                (tr.ActivateAt == null || tr.ActivateAt.Value <= now));

            if (immediateTransition != null)
            {
                await ExecuteAutomatedTransitionInternalAsync(ticket, immediateTransition);
                // Recursively check if the next state also has an immediate automatic transition
                await TriggerImmediateAutomaticTransitionsAsync(ticketId, maxDepth - 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در اجرای بلادرنگ انتقال خودکار برای تیکت {TicketId}.", ticketId);
        }
    }

    private async Task ExecuteAutomatedTransitionInternalAsync(Ticket ticket, Transition transition, bool isDeadlineTriggered = false)
    {
        var targetStatusId = transition.ToStatus?.StatusId ?? 0;
        var targetWorkflowStatusId = transition.ToState;

        var historyComment = isDeadlineTriggered
            ? $"انتقال خودکار سیستم به دلیل اتمام مهلت زمانی (Deadline Exceeded) - ترنزیشن: {transition.Name}"
            : (transition.ActivateAt.HasValue
                ? $"انتقال خودکار زمان‌بندی‌شده سیستم (زمان فعال‌سازی: {transition.ActivateAt.Value:yyyy/MM/dd HH:mm} UTC)"
                : "انتقال خودکار سیستم طبق قوانین جریان کاری");

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            TicketTitle = ticket.Title,
            TransitionId = transition.Id,
            TransitionTitle = transition.Name,
            UserId = ticket.UserId,
            WorkFlowId = ticket.Project?.WorkflowId ?? 0,
            WorkFlowName = ticket.Project?.Workflow?.Name ?? string.Empty,
            FromStatusId = transition.FromStatus?.StatusId ?? 0,
            FromStatusName = transition.FromStatus?.Status?.Name ?? ticket.Status?.Name,
            ToStatusId = targetStatusId,
            ToStatusName = transition.ToStatus?.Status?.Name,
            Comment = historyComment,
            CreatedAt = DateTime.UtcNow
        };

        await _ticketRepository.ApplyTransitionAndSaveHistoryAsync(ticket.Id, targetStatusId, targetWorkflowStatusId, history);

        // Update ticket in database with new status and DueDate based on destination transition's DeadlineMinutes or outgoing automated transition
        using (var updateContext = await _factory.CreateDbContextAsync())
        {
            var dbTicket = await updateContext.Set<Ticket>().FindAsync(ticket.Id);
            if (dbTicket != null)
            {
                dbTicket.StatusId = targetStatusId;
                dbTicket.WorkflowStatusId = targetWorkflowStatusId;

                var outgoingAutoTransition = ticket.Project?.Workflow?.Transitions
                    .FirstOrDefault(tr => tr.IsActive && tr.IsAutomated == 1 && tr.FromState == targetWorkflowStatusId);

                int? effectiveDeadline = outgoingAutoTransition?.DeadlineMinutes ?? transition.DeadlineMinutes;

                if (effectiveDeadline.HasValue && effectiveDeadline.Value > 0)
                {
                    dbTicket.DueDate = DateTime.UtcNow.AddMinutes(effectiveDeadline.Value);
                }
                else
                {
                    dbTicket.DueDate = null;
                }
                await updateContext.SaveChangesAsync();
            }
        }


        _logger.LogInformation("انتقال خودکار '{TransitionName}' روی تیکت {TicketId} با موفقیت اعمال گردید (علت: {Reason}).",
            transition.Name, ticket.Id, isDeadlineTriggered ? "انقضای ددلاین" : "قانون اتوماسیون");

        await _eventBroker.PublishTransitionOccurredAsync(ticket.Id);
        await _eventBroker.PublishTicketUpdatedAsync(ticket.Id);

        if (_notificationService != null && ticket.UserId > 0)
        {
            try
            {
                var targetStatusName = transition.ToStatus?.Status?.Name ?? "وضعیت خودکار";
                await _notificationService.CreateNotificationAsync(new Application.DTOs.CreateNotificationDto
                {
                    UserId = ticket.UserId,
                    Title = isDeadlineTriggered ? $"هشدار انقضای مهلت تیکت #{ticket.Id}" : $"انتقال خودکار تیکت #{ticket.Id}",
                    Message = isDeadlineTriggered
                        ? $"مهلت اقدام تیکت «{ticket.Title}» به پایان رسید و وضعیت به «{targetStatusName}» تغییر یافت."
                        : $"تیکت «{ticket.Title}» بر اساس روال سیستم به وضعیت «{targetStatusName}» منتقل شد.",
                    Type = isDeadlineTriggered ? Core.Enums.NotificationType.DeadlineBreached : Core.Enums.NotificationType.StatusChanged,
                    Severity = isDeadlineTriggered ? Core.Enums.NotificationSeverity.Warning : Core.Enums.NotificationSeverity.Info,
                    ReferenceId = ticket.Id,
                    ActionUrl = $"/tickets/{ticket.Id}"
                });
            }
            catch (Exception notifEx)
            {
                _logger.LogWarning(notifEx, "خطا در ارسال اعلان اتوماسیون تیکت {TicketId}.", ticket.Id);
            }
        }
    }

    public async Task ProcessDeadlinesAsync()
    {
        try
        {
            using var context = await _factory.CreateDbContextAsync();
            var now = DateTime.UtcNow;

            var overdueTickets = await context.Set<Ticket>()
                .Include(t => t.Project)
                    .ThenInclude(p => p.Workflow)
                        .ThenInclude(w => w.Transitions)
                            .ThenInclude(tr => tr.FromStatus)
                                .ThenInclude(fs => fs.Status)
                .Include(t => t.Project)
                    .ThenInclude(p => p.Workflow)
                        .ThenInclude(w => w.Transitions)
                            .ThenInclude(tr => tr.ToStatus)
                                .ThenInclude(ts => ts.Status)
                .Include(t => t.Status)
                .Where(t => t.DueDate != null && t.DueDate.Value <= now && t.WorkflowStatusId != null && t.Project != null && t.Project.Workflow != null && t.Project.Workflow.IsActive)
                .AsSplitQuery()
                .ToListAsync();

            if (!overdueTickets.Any()) return;

            _logger.LogWarning("{Count} تیکت دارای موعد زمانی سررسید شده (Overdue / Deadline Exceeded) شناسایی شدند.", overdueTickets.Count);

            foreach (var ticket in overdueTickets)
            {
                var workflow = ticket.Project?.Workflow;
                if (workflow == null) continue;

                // Find active automated transition out of current workflow status
                var deadlineTransition = workflow.Transitions.FirstOrDefault(tr =>
                    tr.IsActive &&
                    tr.IsAutomated == 1 &&
                    tr.FromState == ticket.WorkflowStatusId);

                if (deadlineTransition != null)
                {
                    await ExecuteAutomatedTransitionInternalAsync(ticket, deadlineTransition, isDeadlineTriggered: true);
                }
                else
                {
                    // Real-time broadcast so UI reflects the overdue state even when no automated transition is defined
                    await _eventBroker.PublishTicketUpdatedAsync(ticket.Id);

                    // Send deadline breach warning notification to user
                    if (_notificationService != null && ticket.UserId > 0)
                    {
                        try
                        {
                            await _notificationService.CreateNotificationAsync(new Application.DTOs.CreateNotificationDto
                            {
                                UserId = ticket.UserId,
                                Title = $"هشدار انقضای مهلت تیکت #{ticket.Id}",
                                Message = $"مهلت اقدام تیکت «{ticket.Title}» در وضعیت «{ticket.Status?.Name ?? "فعلی"}» به پایان رسیده و نیازمند پیگیری است.",
                                Type = Core.Enums.NotificationType.DeadlineBreached,
                                Severity = Core.Enums.NotificationSeverity.Warning,
                                ReferenceId = ticket.Id,
                                ActionUrl = $"/tickets/{ticket.Id}"
                            });
                        }
                        catch (Exception notifEx)
                        {
                            _logger.LogWarning(notifEx, "خطا در ارسال اعلان انقضای مهلت تیکت {TicketId}.", ticket.Id);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در پردازش موعد زمانی و مهلت تیکت‌ها.");
        }
    }
}

