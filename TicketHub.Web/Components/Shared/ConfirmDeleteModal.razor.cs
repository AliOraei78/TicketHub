using Microsoft.AspNetCore.Components;

namespace TicketHub.Web.Components.Shared;

public partial class ConfirmDeleteModal : ComponentBase
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public string Title { get; set; } = "حذف اطلاعات";
    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;
}
