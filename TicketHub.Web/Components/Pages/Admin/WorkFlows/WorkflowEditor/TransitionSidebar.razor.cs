using System.Globalization;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Core.Common;

namespace TicketHub.Web.Components.Pages.Admin.WorkFlows.WorkflowEditor;

public partial class TransitionSidebar : ComponentBase
{
    [Parameter] public IEnumerable<FieldTypeDto> AvailableFieldTypes { get; set; } = new List<FieldTypeDto>();
    [Parameter] public CanvasConnection? Connection { get; set; }
    [Parameter] public IEnumerable<RoleDto> AvailableRoles { get; set; } = new List<RoleDto>();
    [Parameter] public Func<Guid, string>? GetNodeName { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<int> OnRoleToggle { get; set; }
    [Parameter] public EventCallback<CanvasConnection> OnDelete { get; set; }

    protected CanvasTransitionField? DraggedField { get; set; }

    protected string ActivateAtString
    {
        get
        {
            if (Connection == null || !Connection.ActivateAt.HasValue) return string.Empty;
            var dt = Connection.ActivateAt.Value;
            var pc = new PersianCalendar();
            return $"{pc.GetYear(dt):0000}/{pc.GetMonth(dt):00}/{pc.GetDayOfMonth(dt):00} {pc.GetHour(dt):00}:{pc.GetMinute(dt):00}";
        }
        set
        {
            if (Connection == null) return;
            if (string.IsNullOrWhiteSpace(value))
            {
                Connection.ActivateAt = null;
                return;
            }
            try
            {
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var dateParts = parts[0].Split('/');
                int year = int.Parse(dateParts[0]);
                int month = int.Parse(dateParts[1]);
                int day = int.Parse(dateParts[2]);

                int hour = 0, minute = 0;
                if (parts.Length > 1)
                {
                    var timeParts = parts[1].Split(':');
                    hour = int.Parse(timeParts[0]);
                    minute = int.Parse(timeParts[1]);
                }
                var pc = new PersianCalendar();
                Connection.ActivateAt = pc.ToDateTime(year, month, day, hour, minute, 0, 0);
            }
            catch
            {
                Connection.ActivateAt = null;
            }
        }
    }

    protected void AddCustomField()
    {
        if (Connection != null)
        {
            int nextSortOrder = Connection.CustomFields.Any() ? Connection.CustomFields.Max(f => f.SortOrder) + 1 : 0;
            Connection.CustomFields.Add(new CanvasTransitionField
            {
                Id = 0,
                SortOrder = nextSortOrder,
                IsActive = true
            });
        }
    }

    protected void RemoveCustomField(CanvasTransitionField field)
    {
        Connection?.CustomFields.Remove(field);
    }

    protected void HandleDragStart(CanvasTransitionField field)
    {
        DraggedField = field;
    }

    protected void HandleDragEnter(CanvasTransitionField targetField)
    {
        if (DraggedField == null || Connection == null || DraggedField == targetField) return;

        var orderedFields = Connection.CustomFields.OrderBy(f => f.SortOrder).ToList();
        int draggedIndex = orderedFields.IndexOf(DraggedField);
        int targetIndex = orderedFields.IndexOf(targetField);

        orderedFields.RemoveAt(draggedIndex);
        orderedFields.Insert(targetIndex, DraggedField);

        for (int i = 0; i < orderedFields.Count; i++)
        {
            orderedFields[i].SortOrder = i;
        }
    }

    protected void HandleDragEnd()
    {
        DraggedField = null;
    }
}
