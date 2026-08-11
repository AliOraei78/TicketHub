namespace TicketHub.Core.Enums;

public enum NotificationType
{
    TicketComment = 1,
    StatusChanged = 2,
    TicketAssigned = 3,
    TicketCreated = 4,
    DeadlineApproaching = 5,
    DeadlineBreached = 6,
    System = 7
}

public enum NotificationSeverity
{
    Info = 1,
    Success = 2,
    Warning = 3,
    Danger = 4
}
