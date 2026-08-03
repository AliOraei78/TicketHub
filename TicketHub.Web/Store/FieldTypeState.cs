using Fluxor;
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

    public FieldTypeEffects(IFieldTypeService service)
    {
        _service = service;
    }

    [EffectMethod(typeof(LoadFieldTypesAction))]
    public async Task HandleLoad(IDispatcher dispatcher)
    {
        var types = await _service.GetAllAsync(); //[cite: 11]
        dispatcher.Dispatch(new FieldTypesLoadedAction(types));
    }
}