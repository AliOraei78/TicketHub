using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

public class AttachmentService : IAttachmentService
{
    private readonly IRepository<Attachment> _repo;
    public AttachmentService(IRepository<Attachment> repo) => _repo = repo;
    public async Task<IEnumerable<Attachment>> GetAllAsync() => await _repo.GetAllAsync();
    public async Task<Attachment?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);
    public async Task CreateAsync(Attachment entity) => await _repo.AddAsync(entity);
    public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);
}