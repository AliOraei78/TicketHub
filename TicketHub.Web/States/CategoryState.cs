using System;
using System.Collections.Generic;
using TicketHub.Core.Entities;

namespace TicketHub.Web.State;

public class CategoryState
{
    public List<Category> Categories { get; set; } = new();
    public bool IsLoading { get; set; } = false;

    public event Action? OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();
}