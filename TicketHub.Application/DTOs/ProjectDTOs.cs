using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Application.DTOs;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // فیلدهای زیر را اضافه کنید
    public int? WorkflowId { get; set; }
    public DateTime CreatedAt { get; set; }
    public WorkflowDto? Workflow { get; set; }
    public List<int> RoleIds { get; set; } = new();
}
