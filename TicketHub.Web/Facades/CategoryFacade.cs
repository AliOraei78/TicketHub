using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Web.State;

namespace TicketHub.Web.Facades;

public class CategoryFacade
{
    private readonly ICategoryService _categoryService;
    private readonly CategoryState _categoryState;

    public CategoryFacade(ICategoryService categoryService, CategoryState categoryState)
    {
        _categoryService = categoryService;
        _categoryState = categoryState;
    }

    public async Task LoadCategoriesAsync()
    {
        _categoryState.IsLoading = true;
        _categoryState.NotifyStateChanged();

        var categories = await _categoryService.GetAllAsync();
        _categoryState.Categories = categories.Adapt<List<CategoryDto>>();

        _categoryState.IsLoading = false;
        _categoryState.NotifyStateChanged();
    }

    public async Task AddOrUpdateAsync(CategoryDto categoryDto, bool isEditing)
    {
        var category = categoryDto.Adapt<Category>();

        if (isEditing)
            await _categoryService.UpdateAsync(category);
        else
            await _categoryService.AddAsync(category);

        await LoadCategoriesAsync();
    }

    public async Task DeleteAsync(CategoryDto categoryDto)
    {
        var category = categoryDto.Adapt<Category>();
        await _categoryService.DeleteAsync(category);
        await LoadCategoriesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        await _categoryService.DeleteRangeAsync(ids);
        await LoadCategoriesAsync();
    }
}