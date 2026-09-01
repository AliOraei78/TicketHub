namespace TicketHub.Core.Common.Exceptions;

public class ConcurrencyException : TicketHubException
{
    public ConcurrencyException(string message = "اطلاعات این تیکت همزمان توسط کاربر یا فرآیند دیگری تغییر کرده است. لطفاً صفحه را تازه‌سازی کنید.")
        : base(message, 409, "CONCURRENCY_CONFLICT")
    {
    }
}
