using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _repository;

    public CategoryService(IRepository<Category> repository)
    {
        _repository = repository;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        var result = await _repository.GetAllAsync();
        return result.ToList();
    }

    public async Task<Category?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);

    public async Task<Category> AddAsync(Category category)
    {
        category.CreatedAt = DateTime.UtcNow;
        await _repository.AddAsync(category);
        return category;
    }

    public async Task UpdateAsync(Category category) => await _repository.UpdateAsync(category);

    public async Task DeleteAsync(Category category) => await _repository.DeleteAsync(category.Id);

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var allCategories = await _repository.GetAllAsync();
        var toDelete = allCategories.Where(c => ids.Contains(c.Id)).ToList();

        if (toDelete.Any())
        {
            await _repository.DeleteRangeAsync(toDelete);
        }
    }
}
