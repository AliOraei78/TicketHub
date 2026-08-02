using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
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

        // نیازی به Adapt مجدد نیست
        _categoryState.Categories = await _categoryService.GetAllAsync();

        _categoryState.IsLoading = false;
        _categoryState.NotifyStateChanged();
    }

    public async Task AddOrUpdateAsync(CategoryDto categoryDto, bool isEditing)
    {
        if (isEditing)
            await _categoryService.UpdateAsync(categoryDto);
        else
            await _categoryService.AddAsync(categoryDto);

        await LoadCategoriesAsync();
    }

    public async Task DeleteAsync(CategoryDto categoryDto)
    {
        await _categoryService.DeleteAsync(categoryDto);
        await LoadCategoriesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        await _categoryService.DeleteRangeAsync(ids);
        await LoadCategoriesAsync();
    }

    public async Task UpdateStatusRangeAsync(IEnumerable<int> ids, bool isActive)
    {
        await _categoryService.UpdateCategoriesStatusAsync(ids, isActive);
        await LoadCategoriesAsync();
    }
}