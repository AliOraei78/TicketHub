using Fluxor;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Web.Store;

// 1. State
[FeatureState]
public record FieldTypeState(bool IsLoading, IEnumerable<FieldTypeDto> FieldTypes)
{
    private FieldTypeState() : this(true, Array.Empty<FieldTypeDto>()) { }
}

// 2. Actions
public record LoadFieldTypesAction();
public record FieldTypesLoadedAction(IEnumerable<FieldTypeDto> FieldTypes);

// 3. Reducers
public static class FieldTypeReducers
{
    [ReducerMethod]
    public static FieldTypeState ReduceLoad(FieldTypeState state, LoadFieldTypesAction action) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static FieldTypeState ReduceLoaded(FieldTypeState state, FieldTypesLoadedAction action) =>
        state with { IsLoading = false, FieldTypes = action.FieldTypes };
}

// 4. Effects
public class FieldTypeEffects
{
    private readonly IFieldTypeService _service;
    private readonly ILogger<FieldTypeEffects> _logger;

    public FieldTypeEffects(IFieldTypeService service, ILogger<FieldTypeEffects> logger)
    {
        _service = service;
        _logger = logger;
    }

    [EffectMethod(typeof(LoadFieldTypesAction))]
    public async Task HandleLoad(IDispatcher dispatcher)
    {
        try
        {
            _logger.LogInformation("شروع دریافت لیست انواع فیلدها.");

            var types = await _service.GetAllAsync();

            _logger.LogInformation("دریافت انواع فیلدها با موفقیت انجام شد.");
            dispatcher.Dispatch(new FieldTypesLoadedAction(types));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست انواع فیلدها.");
            // بازگرداندن یک لیست خالی برای خروج State از حالت Loading
            dispatcher.Dispatch(new FieldTypesLoadedAction(Array.Empty<FieldTypeDto>()));
        }
    }
}