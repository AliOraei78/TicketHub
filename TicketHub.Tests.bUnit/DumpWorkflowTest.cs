using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Infrastructure.Data;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class DumpWorkflowTest
    {
        [Fact]
        public async Task DumpGeneralWorkflow()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer("Server=.;Database=TicketHubDb;User Id=sa;Password=Ali433433;TrustServerCertificate=True;MultipleActiveResultSets=true;");

            using var context = new AppDbContext(optionsBuilder.Options);

            var workflow = await context.Workflows
                .Include(w => w.WorkflowStatuses)
                    .ThenInclude(ws => ws.Status)
                .Include(w => w.Transitions)
                    .ThenInclude(t => t.FromStatus)
                        .ThenInclude(ws => ws.Status)
                .Include(w => w.Transitions)
                    .ThenInclude(t => t.ToStatus)
                        .ThenInclude(ws => ws.Status)
                .Include(w => w.Transitions)
                    .ThenInclude(t => t.TransitionRoles)
                        .ThenInclude(tr => tr.Role)
                .Include(w => w.Transitions)
                    .ThenInclude(t => t.TransitionFields)
                        .ThenInclude(tf => tf.FieldType)
                .FirstOrDefaultAsync(w => w.Name == "عمومی" || w.Name == "جریان کاری عمومی");

            if (workflow == null)
            {
                var allWorkflows = await context.Workflows.Select(w => w.Name).ToListAsync();
                File.WriteAllText("dump_workflow.json", JsonSerializer.Serialize(new { NotFound = true, All = allWorkflows }, new JsonSerializerOptions { WriteIndented = true }));
                return;
            }

            var dump = new
            {
                workflow.Id,
                workflow.Name,
                workflow.Description,
                workflow.IsActive,
                WorkflowStatuses = workflow.WorkflowStatuses.Select(ws => new
                {
                    ws.Id,
                    ws.NodeId,
                    ws.StatusId,
                    StatusName = ws.Status?.Name,
                    ws.PositionX,
                    ws.PositionY,
                    ws.IsInitial
                }).ToList(),
                Transitions = workflow.Transitions.Select(t => new
                {
                    t.Id,
                    t.FromState,
                    FromStatusNodeId = t.FromStatus?.NodeId,
                    FromStatusName = t.FromStatus?.Status?.Name,
                    t.ToState,
                    ToStatusNodeId = t.ToStatus?.NodeId,
                    ToStatusName = t.ToStatus?.Status?.Name,
                    t.FromHandle,
                    t.ToHandle,
                    t.ActionName,
                    t.Path,
                    t.RequiredCommentType,
                    t.DeadlineMinutes,
                    t.DueDateRule,
                    Roles = t.TransitionRoles.Select(tr => new
                    {
                        tr.RoleId,
                        RoleName = tr.Role?.Name
                    }).ToList(),
                    Fields = t.TransitionFields.Select(tf => new
                    {
                        tf.Id,
                        tf.Label,
                        tf.FieldTypeId,
                        FieldTypeName = tf.FieldType?.Type,
                        tf.IsRequired,
                        tf.ValidationRegex,
                        tf.OptionsJson,
                        tf.ErrorMessage,
                        tf.Order,
                        tf.HelpText,
                        tf.Placeholder,
                        tf.DefaultValue
                    }).ToList()
                }).ToList()
            };

            var json = JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("dump_workflow.json", json);
        }
    }
}
