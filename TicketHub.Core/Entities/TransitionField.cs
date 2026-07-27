namespace TicketHub.Core.Entities;

public class TransitionField
{
    public int Id { get; set; } // Primary Key

    public int TransitionId { get; set; }
    public Transition Transition { get; set; } = null!;

    public int FieldTypeId { get; set; }
    public FieldType FieldType { get; set; } = null!;

    public string FieldName { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public int SortOrder { get; set; }
    public string? Options { get; set; }
    public string? Placeholder { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsActive { get; set; } = true;
}