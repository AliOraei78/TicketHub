namespace TicketHub.Core.Entities;

public class Priority
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // مثل: Low, Medium, High, Critical
    public string ColorCode { get; set; } = "#6B7280"; // رنگ نمایشی برای UI
    public int Level { get; set; } // برای مقایسه عددی (مثلا 1 یعنی کم، 4 یعنی بحرانی)
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}