namespace TicketHub.Core.Common.Exceptions;

public class NotFoundException : TicketHubException
{
    public NotFoundException(string message, string errorCode = "RESOURCE_NOT_FOUND")
        : base(message, 404, errorCode)
    {
    }

    // سازنده کمکی برای حالت‌های رایج
    public NotFoundException(string name, object key)
        : base($"موجودی با مشخصات ({key}) برای {name} یافت نشد.", 404, "NOT_FOUND")
    {
    }
}