using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _repository; // تغییر به Category

    public CategoryService(IRepository<Category> repository)
    {
        _repository = repository;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var result = await _repository.GetAllAsync();
        return result.Adapt<List<CategoryDto>>(); // تبدیل به DTO
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var result = await _repository.GetByIdAsync(id);
        return result?.Adapt<CategoryDto>();
    }

    public async Task<CategoryDto> AddAsync(CategoryDto categoryDto)
    {
        var category = categoryDto.Adapt<Category>();
        category.CreatedAt = DateTime.UtcNow;
        await _repository.AddAsync(category);
        return category.Adapt<CategoryDto>();
    }

    public async Task UpdateAsync(CategoryDto categoryDto)
    {
        var category = categoryDto.Adapt<Category>();
        await _repository.UpdateAsync(category);
    }

    public async Task DeleteAsync(CategoryDto categoryDto) =>
        await _repository.DeleteAsync(categoryDto.Id);

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