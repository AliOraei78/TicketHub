using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;

namespace TicketHub.Web.Components.Pages.Admin.Settings.Permissions;

public partial class PermissionForm : ComponentBase
{
    [Parameter] public PermissionDto Model { get; set; } = new();
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public IEnumerable<RoleDto> AvailableRoles { get; set; } = new List<RoleDto>();

    public class PermissionTypeItem
    {
        public PermissionType Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    protected List<PermissionTypeItem> PermissionTypes { set; get; } = new()
    {
        new PermissionTypeItem { Value = PermissionType.Menu, DisplayName = "منو (Menu)" },
        new PermissionTypeItem { Value = PermissionType.SystemSection, DisplayName = "بخش سیستم (System Section)" },
        new PermissionTypeItem { Value = PermissionType.Full, DisplayName = "کامل (Full)" }
    };
}
