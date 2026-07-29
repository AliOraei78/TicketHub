using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Web.States;

namespace TicketHub.Web.Facades;

public class PriorityFacade
{
    private readonly IPriorityService _service;
    private readonly PriorityState _state;

    public PriorityFacade(IPriorityService service, PriorityState state)
    {
        _service = service;
        _state = state;
    }

    public async Task LoadPrioritiesAsync()
    {
        var data = await _service.GetAllAsync();
        _state.SetPriorities(data.ToList());
    }

    public async Task SavePriorityAsync(PriorityDto priority, bool isEditing)
    {
        if (isEditing) await _service.UpdateAsync(priority);
        else await _service.AddAsync(priority);

        _state.SetMessage($"اولویت با موفقیت {(isEditing ? "ویرایش" : "ایجاد")} شد.");
        await LoadPrioritiesAsync();
        ClearMessage();
    }

    public async Task DeletePriorityAsync(PriorityDto priority)
    {
        try
        {
            await _service.DeleteAsync(priority.Id);
            _state.SetMessage("اولویت با موفقیت حذف شد.");
            await LoadPrioritiesAsync();
        }
        catch
        {
            _state.SetMessage("امکان حذف وجود ندارد! ابتدا باید تیکت‌های مرتبط با این اولویت را ویرایش کنید.", true);
        }
        finally { ClearMessage(); }
    }

    public async Task DeleteBulkAsync(IEnumerable<int> ids)
    {
        await _service.DeleteRangeAsync(ids);
        var count = ids.Count();
        _state.SetMessage($"{count} اولویت با موفقیت حذف {(count == 1 ? "شد" : "شدند")}.");
        await LoadPrioritiesAsync();
        ClearMessage();
    }

    private void ClearMessage() =>
        _ = Task.Delay(4000).ContinueWith(_ => _state.SetMessage(null));
}