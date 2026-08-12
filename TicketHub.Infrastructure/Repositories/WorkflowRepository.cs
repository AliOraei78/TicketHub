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
                RemoveTransitionsInternal(context, entity.Transitions);
            }

            if (entity.WorkflowStatuses.Any())
            {
                var statusIds = entity.WorkflowStatuses.Select(s => s.Id).Where(sid => sid > 0).ToList();
                if (statusIds.Any())
                {
                    var tickets = await context.Set<Ticket>()
                        .Where(t => t.WorkflowStatusId != null && statusIds.Contains(t.WorkflowStatusId.Value))
                        .ToListAsync();
                    foreach (var t in tickets)
                    {
                        t.WorkflowStatusId = null;
                    }
                }

                context.Set<WorkflowStatus>().RemoveRange(entity.WorkflowStatuses);
            }

            var workflowHistories = await context.Set<TicketHistory>()
                .Where(th => th.WorkFlowId == id)
                .ToListAsync();
            foreach (var h in workflowHistories)
            {
                h.WorkFlowId = null;
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
        var roleIds = roles.Select(r => r.Id).Where(id => id > 0).Distinct().ToList();
        if (roleIds.Any())
        {
            var dbRoles = context.Set<TransitionRole>()
                .Where(r => roleIds.Contains(r.Id))
                .ToList();
            if (dbRoles.Any())
            {
                context.Set<TransitionRole>().RemoveRange(dbRoles);
                context.SaveChanges();
            }
        }
    }

    public void RemoveTransitionFields(IEnumerable<TransitionField> fields)
    {
        using var context = _factory.CreateDbContext();
        RemoveTransitionFieldsInternal(context, fields);
        context.SaveChanges();
    }

    public void RemoveWorkflowStatuses(IEnumerable<WorkflowStatus> statuses)
    {
        using var context = _factory.CreateDbContext();
        var statusIds = statuses.Select(s => s.Id).Where(id => id > 0).Distinct().ToList();
        if (!statusIds.Any()) return;

        var dbStatuses = context.Set<WorkflowStatus>()
            .Where(s => statusIds.Contains(s.Id))
            .ToList();

        if (!dbStatuses.Any()) return;

        var existingStatusIds = dbStatuses.Select(s => s.Id).ToList();

        var tickets = context.Set<Ticket>()
            .Where(t => t.WorkflowStatusId != null && existingStatusIds.Contains(t.WorkflowStatusId.Value))
            .ToList();
        if (tickets.Any())
        {
            foreach (var t in tickets)
            {
                t.WorkflowStatusId = null;
            }
        }

        var transitionsToRemove = context.Set<Transition>()
            .Where(t => existingStatusIds.Contains(t.FromState) || existingStatusIds.Contains(t.ToState))
            .ToList();

        if (transitionsToRemove.Any())
        {
            RemoveTransitionsInternal(context, transitionsToRemove);
        }

        context.Set<WorkflowStatus>().RemoveRange(dbStatuses);
        context.SaveChanges();
    }

    public void RemoveTransitions(IEnumerable<Transition> transitions)
    {
        using var context = _factory.CreateDbContext();
        RemoveTransitionsInternal(context, transitions);
        context.SaveChanges();
    }

    private void RemoveTransitionsInternal(AppDbContext context, IEnumerable<Transition> transitions)
    {
        var transitionIds = transitions.Select(t => t.Id).Where(id => id > 0).Distinct().ToList();
        if (!transitionIds.Any()) return;

        var dbTransitions = context.Set<Transition>()
            .Where(t => transitionIds.Contains(t.Id))
            .ToList();

        if (!dbTransitions.Any()) return;

        var existingIds = dbTransitions.Select(t => t.Id).ToList();

        // ۱. قطع وابستگی کلید خارجی در تاریخچه تیکت‌ها برای حفظ لاگ و جلوگیری از خطای FK Conflict
        var histories = context.Set<TicketHistory>()
            .Where(th => th.TransitionId != null && existingIds.Contains(th.TransitionId.Value))
            .ToList();
        if (histories.Any())
        {
            foreach (var history in histories)
            {
                history.TransitionId = null;
            }
            context.SaveChanges();
        }

        // ۲. پاکسازی TransitionFieldValue و Attachments مربوط به فیلدهای این Transitionها
        var transitionFields = context.Set<TransitionField>()
            .Where(tf => existingIds.Contains(tf.TransitionId))
            .ToList();
        if (transitionFields.Any())
        {
            RemoveTransitionFieldsInternal(context, transitionFields);
        }

        // ۳. حذف خود Transitionها (EF Core / SQL Server به صورت خودکار TransitionRole و TransitionField را آبشاری حذف می‌کند)
        context.Set<Transition>().RemoveRange(dbTransitions);
    }

    private void RemoveTransitionFieldsInternal(AppDbContext context, IEnumerable<TransitionField> fields)
    {
        var fieldIds = fields.Select(f => f.Id).Where(id => id > 0).Distinct().ToList();
        if (!fieldIds.Any()) return;

        var dbFields = context.Set<TransitionField>()
            .Where(f => fieldIds.Contains(f.Id))
            .ToList();

        if (!dbFields.Any()) return;

        var existingFieldIds = dbFields.Select(f => f.Id).ToList();

        var fieldValues = context.Set<TransitionFieldValue>()
            .Include(tfv => tfv.Attachments)
            .Where(tfv => existingFieldIds.Contains(tfv.TransitionFieldId))
            .ToList();

        if (fieldValues.Any())
        {
            var valueAttachments = fieldValues.SelectMany(tfv => tfv.Attachments).DistinctBy(a => a.Id).ToList();
            if (valueAttachments.Any())
            {
                context.Set<Attachment>().RemoveRange(valueAttachments);
            }
            context.Set<TransitionFieldValue>().RemoveRange(fieldValues);
            context.SaveChanges();
        }

        var fieldAttachments = context.Set<Attachment>()
            .Where(a => a.TransitionFieldId != null && existingFieldIds.Contains(a.TransitionFieldId.Value))
            .ToList()
            .DistinctBy(a => a.Id)
            .ToList();
        if (fieldAttachments.Any())
        {
            context.Set<Attachment>().RemoveRange(fieldAttachments);
            context.SaveChanges();
        }

        context.Set<TransitionField>().RemoveRange(dbFields);
        context.SaveChanges();
    }

    public override async Task UpdateAsync(Workflow entity)
    {
        using var context = await _factory.CreateDbContextAsync();
        var dbWorkflow = await context.Set<Workflow>()
            .Include(w => w.WorkflowStatuses)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.AllowedRoles)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.TransitionFields)
            .AsSplitQuery()
            .FirstOrDefaultAsync(w => w.Id == entity.Id);

        if (dbWorkflow == null)
            return;

        // ۱. آپدیت فیلدهای اصلی جریان کاری
        dbWorkflow.Name = entity.Name;
        dbWorkflow.Description = entity.Description;
        dbWorkflow.IsActive = entity.IsActive;

        // ۲. مدیریت وضعیت‌ها (WorkflowStatuses) روی بوم
        var activeNodeIds = entity.WorkflowStatuses.Select(ws => ws.NodeId).ToHashSet();
        var statusesToRemove = dbWorkflow.WorkflowStatuses.Where(ws => !activeNodeIds.Contains(ws.NodeId)).ToList();

        if (statusesToRemove.Any())
        {
            var statusIdsToRemove = statusesToRemove.Select(s => s.Id).Where(id => id > 0).ToList();
            if (statusIdsToRemove.Any())
            {
                // قطع وابستگی تیکت‌ها به وضعیت‌های حذف‌شده
                var tickets = await context.Set<Ticket>()
                    .Where(t => t.WorkflowStatusId != null && statusIdsToRemove.Contains(t.WorkflowStatusId.Value))
                    .ToListAsync();
                foreach (var t in tickets)
                {
                    t.WorkflowStatusId = null;
                }

                // حذف ترنزیشن‌های متصل به وضعیت‌های حذف‌شده
                var connectedTransitions = dbWorkflow.Transitions
                    .Where(t => statusIdsToRemove.Contains(t.FromState) || statusIdsToRemove.Contains(t.ToState))
                    .ToList();
                if (connectedTransitions.Any())
                {
                    RemoveTransitionsInternal(context, connectedTransitions);
                    foreach (var ct in connectedTransitions)
                    {
                        dbWorkflow.Transitions.Remove(ct);
                    }
                }
            }

            context.Set<WorkflowStatus>().RemoveRange(statusesToRemove);
            foreach (var st in statusesToRemove)
            {
                dbWorkflow.WorkflowStatuses.Remove(st);
            }
        }

        foreach (var ws in entity.WorkflowStatuses)
        {
            var existingWs = dbWorkflow.WorkflowStatuses.FirstOrDefault(w => w.NodeId == ws.NodeId);
            if (existingWs != null)
            {
                existingWs.PositionX = ws.PositionX;
                existingWs.PositionY = ws.PositionY;
                existingWs.StatusId = ws.StatusId;
                existingWs.IsInitial = ws.IsInitial;
                existingWs.IsFinal = ws.IsFinal;
            }
            else
            {
                var newWs = new WorkflowStatus
                {
                    NodeId = ws.NodeId,
                    PositionX = ws.PositionX,
                    PositionY = ws.PositionY,
                    StatusId = ws.StatusId,
                    IsInitial = ws.IsInitial,
                    IsFinal = ws.IsFinal,
                    WorkflowId = dbWorkflow.Id
                };
                dbWorkflow.WorkflowStatuses.Add(newWs);
            }
        }

        // ۳. مدیریت انتقالات (Transitions)
        var activeTransitionIds = entity.Transitions.Where(t => t.Id > 0).Select(t => t.Id).ToHashSet();
        var transitionsToRemove = dbWorkflow.Transitions.Where(t => !activeTransitionIds.Contains(t.Id)).ToList();

        if (transitionsToRemove.Any())
        {
            RemoveTransitionsInternal(context, transitionsToRemove);
            foreach (var tr in transitionsToRemove)
            {
                dbWorkflow.Transitions.Remove(tr);
            }
        }

        foreach (var t in entity.Transitions)
        {
            if (t.Id > 0)
            {
                var existingDbT = dbWorkflow.Transitions.FirstOrDefault(dbT => dbT.Id == t.Id);
                if (existingDbT != null)
                {
                    existingDbT.Name = t.Name;
                    existingDbT.SourcePort = t.SourcePort;
                    existingDbT.TargetPort = t.TargetPort;
                    existingDbT.FromNodeId = t.FromNodeId;
                    existingDbT.ToNodeId = t.ToNodeId;
                    existingDbT.FromStatus = dbWorkflow.WorkflowStatuses.First(ws => ws.NodeId == t.FromNodeId);
                    existingDbT.ToStatus = dbWorkflow.WorkflowStatuses.First(ws => ws.NodeId == t.ToNodeId);
                    existingDbT.IsAutomated = t.IsAutomated;
                    existingDbT.IsActive = t.IsActive;
                    existingDbT.ActivateAt = t.ActivateAt;
                    existingDbT.DeadlineMinutes = t.DeadlineMinutes;

                    // Sync AllowedRoles
                    var incomingRoleIds = t.AllowedRoles.Select(r => r.RoleId).ToHashSet();
                    var rolesToRemove = existingDbT.AllowedRoles.Where(r => !incomingRoleIds.Contains(r.RoleId)).ToList();
                    if (rolesToRemove.Any())
                    {
                        context.Set<TransitionRole>().RemoveRange(rolesToRemove);
                        foreach (var r in rolesToRemove)
                        {
                            existingDbT.AllowedRoles.Remove(r);
                        }
                    }
                    var newRoleIds = incomingRoleIds.Where(id => !existingDbT.AllowedRoles.Any(r => r.RoleId == id)).ToList();
                    foreach (var roleId in newRoleIds)
                    {
                        existingDbT.AllowedRoles.Add(new TransitionRole { RoleId = roleId, TransitionId = existingDbT.Id });
                    }

                    // Sync TransitionFields
                    var incomingFieldIds = t.TransitionFields.Where(f => f.Id > 0).Select(f => f.Id).ToHashSet();
                    var fieldsToRemove = existingDbT.TransitionFields.Where(f => !incomingFieldIds.Contains(f.Id)).ToList();
                    if (fieldsToRemove.Any())
                    {
                        RemoveTransitionFieldsInternal(context, fieldsToRemove);
                        foreach (var f in fieldsToRemove)
                        {
                            existingDbT.TransitionFields.Remove(f);
                        }
                    }
                    foreach (var f in t.TransitionFields)
                    {
                        if (f.Id > 0)
                        {
                            var existingF = existingDbT.TransitionFields.FirstOrDefault(tf => tf.Id == f.Id);
                            if (existingF != null)
                            {
                                existingF.FieldName = f.FieldName;
                                existingF.FieldTypeId = f.FieldTypeId;
                                existingF.IsRequired = f.IsRequired;
                                existingF.SortOrder = f.SortOrder;
                                existingF.Options = f.Options;
                                existingF.Placeholder = f.Placeholder;
                                existingF.DefaultValue = f.DefaultValue;
                                existingF.IsActive = f.IsActive;
                            }
                        }
                        else
                        {
                            existingDbT.TransitionFields.Add(new TransitionField
                            {
                                FieldName = f.FieldName,
                                FieldTypeId = f.FieldTypeId,
                                IsRequired = f.IsRequired,
                                SortOrder = f.SortOrder,
                                Options = f.Options,
                                Placeholder = f.Placeholder,
                                DefaultValue = f.DefaultValue,
                                IsActive = f.IsActive,
                                TransitionId = existingDbT.Id
                            });
                        }
                    }
                }
            }
            else
            {
                var newTransition = new Transition
                {
                    Name = t.Name,
                    SourcePort = t.SourcePort,
                    TargetPort = t.TargetPort,
                    FromNodeId = t.FromNodeId,
                    ToNodeId = t.ToNodeId,
                    FromStatus = dbWorkflow.WorkflowStatuses.First(ws => ws.NodeId == t.FromNodeId),
                    ToStatus = dbWorkflow.WorkflowStatuses.First(ws => ws.NodeId == t.ToNodeId),
                    IsAutomated = t.IsAutomated,
                    IsActive = t.IsActive,
                    ActivateAt = t.ActivateAt,
                    DeadlineMinutes = t.DeadlineMinutes,
                    WorkflowId = dbWorkflow.Id
                };

                foreach (var r in t.AllowedRoles)
                {
                    newTransition.AllowedRoles.Add(new TransitionRole { RoleId = r.RoleId });
                }

                foreach (var f in t.TransitionFields)
                {
                    newTransition.TransitionFields.Add(new TransitionField
                    {
                        FieldName = f.FieldName,
                        FieldTypeId = f.FieldTypeId,
                        IsRequired = f.IsRequired,
                        SortOrder = f.SortOrder,
                        Options = f.Options,
                        Placeholder = f.Placeholder,
                        DefaultValue = f.DefaultValue,
                        IsActive = f.IsActive
                    });
                }

                dbWorkflow.Transitions.Add(newTransition);
            }
        }

        await context.SaveChangesAsync();
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
        var idList = ids.ToList();
        if (!idList.Any()) return;

        foreach (var id in idList)
        {
            await DeleteAsync(id);
        }
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