namespace TicketHub.Application.Interfaces;

public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan? ExpirationRelativeToNow => TimeSpan.FromMinutes(10);
}
