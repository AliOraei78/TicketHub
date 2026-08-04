namespace TicketHub.Core.Common.Exceptions;

public abstract class TicketHubException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    protected TicketHubException(string message, int statusCode = 500, string errorCode = "INTERNAL_SERVER_ERROR")
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}