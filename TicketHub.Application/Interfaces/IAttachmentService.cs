using TicketHub.Core.Entities;

public interface IAttachmentService
{
    Task<IEnumerable<Attachment>> GetAllAsync();
    Task<Attachment?> GetByIdAsync(int id);
    Task CreateAsync(Attachment entity);
    Task DeleteAsync(int id);
}