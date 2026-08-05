namespace TicketHub.Core.Common.Exceptions;

public class LogProcessingException : TicketHubException
{
    public LogProcessingException(string message)
        : base(message, 500, "LOG_PROCESSING_ERROR")
    {
    }
}