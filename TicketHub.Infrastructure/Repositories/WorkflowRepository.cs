// TicketHub.Infrastructure/Repositories/WorkflowRepository.cs
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TicketHub.Core.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Infrastructure.Repositories;

public class WorkflowRepository : GenericRepository<Workflow>, IWorkflowRepository
{
    public WorkflowRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<Workflow?> GetWorkflowWithDetailsAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Workflow>()
            .Include(w => w.WorkflowStatuses)
                .ThenInclude(ws => ws.Status)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.AllowedRoles)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.FromStatus)
                    .ThenInclude(ws => ws.Status)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.ToStatus)
                    .ThenInclude(ws => ws.Status)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.TransitionFields)
            .AsSplitQuery()
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public override async Task DeleteAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();
        var entity = await context.Set<Workflow>()
            .Include(w => w.Transitions)
            .Include(w => w.WorkflowStatuses)
            .AsSplitQuery()
            .FirstOrDefaultAsync(w => w.Id == id);

        if (entity != null)
        {
            if (entity.Transitions.Any())
            {
                context.Set<Transition>().RemoveRange(entity.Transitions);
            }

            if (entity.WorkflowStatuses.Any())
            {
                context.Set<WorkflowStatus>().RemoveRange(entity.WorkflowStatuses);
            }

            context.Set<Workflow>().Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<Status>> GetAllStatusesAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Status>().ToListAsync();
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Project>().ToListAsync();
    }

    public async Task<List<WorkflowStatus>> GetStatusesAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<WorkflowStatus>().ToListAsync();
    }

    public void RemoveTransitionRoles(IEnumerable<TransitionRole> roles)
    {
        using var context = _factory.CreateDbContext();
        context.Set<TransitionRole>().RemoveRange(roles);
        context.SaveChanges();
    }

    public void RemoveTransitionFields(IEnumerable<TransitionField> fields)
    {
        using var context = _factory.CreateDbContext();
        var fieldIds = fields.Select(f => f.Id).Where(id => id > 0).ToList();
        if (fieldIds.Any())
        {
            var fieldAttachments = context.Set<Attachment>()
                .Where(a => a.TransitionFieldId != null && fieldIds.Contains(a.TransitionFieldId.Value))
                .ToList();
            if (fieldAttachments.Any())
            {
                context.Set<Attachment>().RemoveRange(fieldAttachments);
            }

            var fieldValues = context.Set<TransitionFieldValue>()
                .Include(tfv => tfv.Attachments)
                .Where(tfv => fieldIds.Contains(tfv.TransitionFieldId))
                .ToList();

            if (fieldValues.Any())
            {
                var valueAttachments = fieldValues.SelectMany(tfv => tfv.Attachments).ToList();
                if (valueAttachments.Any())
                {
                    context.Set<Attachment>().RemoveRange(valueAttachments);
                }
                context.Set<TransitionFieldValue>().RemoveRange(fieldValues);
            }
        }

        context.Set<TransitionField>().RemoveRange(fields);
        context.SaveChanges();
    }

    public void RemoveWorkflowStatuses(IEnumerable<WorkflowStatus> statuses)
    {
        using var context = _factory.CreateDbContext();
        var statusList = statuses.ToList();
        var statusIds = statusList.Select(s => s.Id).Where(id => id > 0).ToList();
        if (statusIds.Any())
        {
            var transitionsToRemove = context.Set<Transition>()
                .Where(t => statusIds.Contains(t.FromState) || statusIds.Contains(t.ToState))
                .ToList();

            if (transitionsToRemove.Any())
            {
                RemoveTransitions(transitionsToRemove);
            }
        }

        context.Set<WorkflowStatus>().RemoveRange(statusList);
        context.SaveChanges();
    }

    public void RemoveTransitions(IEnumerable<Transition> transitions)
    {
        using var context = _factory.CreateDbContext();
        var transitionList = transitions.ToList();
        var transitionIds = transitionList.Select(t => t.Id).Where(id => id > 0).ToList();
        if (transitionIds.Any())
        {
            var transitionFields = context.Set<TransitionField>()
                .Where(tf => transitionIds.Contains(tf.TransitionId))
                .ToList();
            if (transitionFields.Any())
            {
                var fieldIds = transitionFields.Select(tf => tf.Id).ToList();

                var fieldAttachments = context.Set<Attachment>()
                    .Where(a => a.TransitionFieldId != null && fieldIds.Contains(a.TransitionFieldId.Value))
                    .ToList();
                if (fieldAttachments.Any())
                {
                    context.Set<Attachment>().RemoveRange(fieldAttachments);
                }

                var fieldValues = context.Set<TransitionFieldValue>()
                    .Include(tfv => tfv.Attachments)
                    .Where(tfv => fieldIds.Contains(tfv.TransitionFieldId))
                    .ToList();

                if (fieldValues.Any())
                {
                    var valueAttachments = fieldValues.SelectMany(tfv => tfv.Attachments).ToList();
                    if (valueAttachments.Any())
                    {
                        context.Set<Attachment>().RemoveRange(valueAttachments);
                    }
                    context.Set<TransitionFieldValue>().RemoveRange(fieldValues);
                }

                context.Set<TransitionField>().RemoveRange(transitionFields);
            }

            var transitionRoles = context.Set<TransitionRole>()
                .Where(tr => transitionIds.Contains(tr.TransitionId))
                .ToList();
            if (transitionRoles.Any())
            {
                context.Set<TransitionRole>().RemoveRange(transitionRoles);
            }
        }

        context.Set<Transition>().RemoveRange(transitionList);
        context.SaveChanges();
    }


    public async Task CommitChangesAsync()
    {
        // در الگوی Factory تغییرات درون خود متدها (به صورت ایزوله) Save می‌شوند
        await Task.CompletedTask;
    }

    public async Task<List<Workflow>> GetAllWithDetailsAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Workflow>()
            .Include(w => w.WorkflowStatuses)
            .Include(w => w.Transitions)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        using var context = await _factory.CreateDbContextAsync();
        await context.Set<Workflow>()
            .Where(w => ids.Contains(w.Id))
            .ExecuteDeleteAsync();
    }

    public async Task<Transition?> GetTransitionWithDetailsAsync(int transitionId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Transition>()
            .Include(t => t.FromStatus)
                .ThenInclude(ws => ws.Status)
            .Include(t => t.ToStatus)
                .ThenInclude(ws => ws.Status)
            .Include(t => t.AllowedRoles)
            .Include(t => t.TransitionFields)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == transitionId);
    }


    public async Task<WorkflowStatus?> GetWorkflowStatusAsync(int workflowId, int statusId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<WorkflowStatus>()
            .FirstOrDefaultAsync(ws => ws.WorkflowId == workflowId && ws.StatusId == statusId);
    }
}