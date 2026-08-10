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

    public WorkflowAutomationService(
        IDbContextFactory<AppDbContext> factory,
        ITicketRepository ticketRepository,
        ITicketEventBroker eventBroker,
        ILogger<WorkflowAutomationService> logger)
    {
        _factory = factory;
        _ticketRepository = ticketRepository;
        _eventBroker = eventBroker;
        _logger = logger;
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

    private async Task ExecuteAutomatedTransitionInternalAsync(Ticket ticket, Transition transition)
    {
        var targetStatusId = transition.ToStatus?.StatusId ?? 0;
        var targetWorkflowStatusId = transition.ToState;

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
            Comment = transition.ActivateAt.HasValue
                ? $"انتقال خودکار زمان‌بندی‌شده سیستم (زمان فعال‌سازی: {transition.ActivateAt.Value:yyyy/MM/dd HH:mm} UTC)"
                : "انتقال خودکار سیستم طبق قوانین جریان کاری",
            CreatedAt = DateTime.UtcNow
        };

        await _ticketRepository.ApplyTransitionAndSaveHistoryAsync(ticket.Id, targetStatusId, targetWorkflowStatusId, history);

        // Update DueDate if destination transition has DeadlineMinutes
        using (var updateContext = await _factory.CreateDbContextAsync())
        {
            var dbTicket = await updateContext.Set<Ticket>().FindAsync(ticket.Id);
            if (dbTicket != null)
            {
                if (transition.DeadlineMinutes.HasValue && transition.DeadlineMinutes.Value > 0)
                {
                    dbTicket.DueDate = DateTime.UtcNow.AddMinutes(transition.DeadlineMinutes.Value);
                }
                else
                {
                    dbTicket.DueDate = null;
                }
                await updateContext.SaveChangesAsync();
            }
        }

        _logger.LogInformation("انتقال خودکار '{TransitionName}' روی تیکت {TicketId} با موفقیت اعمال گردید.", transition.Name, ticket.Id);

        await _eventBroker.PublishTransitionOccurredAsync(ticket.Id);
        await _eventBroker.PublishTicketUpdatedAsync(ticket.Id);
    }

    public async Task ProcessDeadlinesAsync()
    {
        try
        {
            using var context = await _factory.CreateDbContextAsync();
            var now = DateTime.UtcNow;

            var overdueTickets = await context.Set<Ticket>()
                .Include(t => t.Status)
                .Where(t => t.DueDate != null && t.DueDate.Value <= now)
                .ToListAsync();

            if (!overdueTickets.Any()) return;

            _logger.LogWarning("{Count} تیکت دارای موعد زمانی سررسید شده (Overdue / Deadline Exceeded) شناسایی شدند.", overdueTickets.Count);

            foreach (var ticket in overdueTickets)
            {
                // Real-time broadcast so UI reflects the overdue state
                await _eventBroker.PublishTicketUpdatedAsync(ticket.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در پردازش موعد زمانی و مهلت تیکت‌ها.");
        }
    }
}
