namespace TicketHub.Web.Components.Shared;

public class SortOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    public SortOption() { }

    public SortOption(string value, string label)
    {
        Value = value;
        Label = label;
    }
}
