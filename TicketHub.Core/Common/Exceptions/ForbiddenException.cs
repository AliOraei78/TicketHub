namespace TicketHub.Core.Common.Exceptions;

public class ForbiddenException : TicketHubException
{
    public ForbiddenException(string message = "شما دسترسی لازم برای انجام این عملیات را ندارید.")
        : base(message, 403, "FORBIDDEN")
    {
    }
}
