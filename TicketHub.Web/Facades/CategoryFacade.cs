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

        _categoryState.Categories = await _categoryService.GetAllAsync();

        _categoryState.IsLoading = false;
        _categoryState.NotifyStateChanged();
    }

    public async Task AddOrUpdateAsync(Category category, bool isEditing)
    {
        if (isEditing)
            await _categoryService.UpdateAsync(category);
        else
            await _categoryService.AddAsync(category);

        await LoadCategoriesAsync();
    }

    public async Task DeleteAsync(Category category)
    {
        await _categoryService.DeleteAsync(category);
        await LoadCategoriesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        await _categoryService.DeleteRangeAsync(ids);
        await LoadCategoriesAsync();
    }
}