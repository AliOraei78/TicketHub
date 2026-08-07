using System.Collections.Generic;

namespace TicketHub.Application.DTOs;

public class ExecuteTransitionDto
{
    public int TicketId { get; set; }
    public int TransitionId { get; set; }
    public string? Comment { get; set; }
    public List<TransitionFieldValueDto> FieldValues { get; set; } = new();
}
