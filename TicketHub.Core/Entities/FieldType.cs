namespace TicketHub.Core.Entities;

public class FieldType
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<TransitionField> TransitionFields { get; set; } = new List<TransitionField>();
}