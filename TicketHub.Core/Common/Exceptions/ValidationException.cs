namespace TicketHub.Core.Common.Exceptions;

public class ValidationException : TicketHubException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("خطای اعتبارسنجی رخ داده است.", 400)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message) : base(message, 400)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) : base("خطای اعتبارسنجی رخ داده است.", 400)
    {
        Errors = errors;
    }
}