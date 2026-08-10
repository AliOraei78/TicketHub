namespace TicketHub.Core.Common;

public class CanvasConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int DbId { get; set; } = 0; // <--- این خط باید اضافه شود
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string SourcePort { get; set; } = "Right";
    public string TargetPort { get; set; } = "Left";
    public string Name { get; set; } = string.Empty;
    public bool IsAutomatic { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? ActivateAt { get; set; }
    public int? DeadlineMinutes { get; set; }
    public HashSet<int> AllowedRoleIds { get; set; } = new();

    public List<CanvasTransitionField> CustomFields { get; set; } = new();
}