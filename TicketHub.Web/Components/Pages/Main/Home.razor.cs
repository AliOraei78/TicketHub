using System.Security.Claims;
using Fluxor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Common;
using TicketHub.Web.Store;

namespace TicketHub.Web.Components.Pages.Main;

public partial class Home : Fluxor.Blazor.Web.Components.FluxorComponent, IDisposable
{
    [Inject] public ITicketService TicketService { get; set; } = default!;
    [Inject] public IProjectService ProjectService { get; set; } = default!;
    [Inject] public IPriorityService PriorityService { get; set; } = default!;
    [Inject] public IStatusService StatusService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public IState<TicketState> TicketState { get; set; } = default!;
    [Inject] public IDispatcher Dispatcher { get; set; } = default!;
    [Inject] public IActionSubscriber ActionSubscriber { get; set; } = default!;
    [Inject] public ITicketEventBroker EventBroker { get; set; } = default!;
    [Inject] public ICacheService CacheService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected string UserName { get; set; } = "کاربر گرامی";
    protected string CurrentPersianDate { get; set; } = string.Empty;

    // Create Modal State
    protected bool IsCreateModalOpen { get; set; } = false;
    protected TicketDto NewTicket { get; set; } = new TicketDto();

    protected int TotalTickets { get; set; } = 0;
    protected int NewTicketsCount { get; set; } = 0;
    protected int InProgressCount { get; set; } = 0;
    protected int CriticalAndOverdueCount { get; set; } = 0;
    protected int OverdueCount { get; set; } = 0;
    protected int SlaOnTimePercentage { get; set; } = 100;

    protected List<DailyTrendDto>? TrendData { get; set; }
    protected List<PriorityStatDto>? PriorityStats { get; set; }
    protected List<ProjectWorkloadDto>? ProjectStats { get; set; }
    protected IEnumerable<TicketDto>? RecentTickets { get; set; }

    // Modal state
    protected bool IsChartModalOpen { get; set; } = false;
    protected string ActiveModalType { get; set; } = string.Empty;
    protected string ActiveModalTitle { get; set; } = string.Empty;

    // SVG Path Memoization (Allocations & Render Loop Optimization)
    protected string CachedTrendLinePath { get; private set; } = string.Empty;
    protected string CachedTrendAreaPath { get; private set; } = string.Empty;
    protected string CachedModalTrendLinePath { get; private set; } = string.Empty;
    protected string CachedModalTrendAreaPath { get; private set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        CurrentPersianDate = GetPersianDate(DateTime.UtcNow);

        ActionSubscriber.SubscribeToAction<SaveTicketSuccessAction>(this, async action =>
        {
            IsCreateModalOpen = false;
            await LoadDashboardDataAsync(forceRefresh: true);
            await InvokeAsync(StateHasChanged);
        });

        EventBroker.OnTicketUpdated += HandleLiveTicketEventAsync;
        EventBroker.OnTransitionOccurred += HandleLiveTicketEventAsync;

        var authState = await AuthState;
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            UserName = user.FindFirst(ClaimTypes.Name)?.Value
                       ?? user.FindFirst("name")?.Value
                       ?? user.Identity.Name
                       ?? "کاربر محترم";

            var roles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            Dispatcher.Dispatch(new LoadTicketInitialDataAction(roles));
        }

        await LoadDashboardDataAsync();
        IsLoading = false;
    }

    private async Task HandleLiveTicketEventAsync(int ticketId)
    {
        await InvokeAsync(async () =>
        {
            await LoadDashboardDataAsync(forceRefresh: true);
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        ActionSubscriber.UnsubscribeFromAllActions(this);
        EventBroker.OnTicketUpdated -= HandleLiveTicketEventAsync;
        EventBroker.OnTransitionOccurred -= HandleLiveTicketEventAsync;
    }

    protected async Task LoadDashboardDataAsync(bool forceRefresh = false)
    {
        var authState = await AuthState;
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true) return;

        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value
                           ?? "0";

        var cacheKey = $"dashboard_summary_{userIdString}";

        if (!forceRefresh)
        {
            var cachedSummary = await CacheService.GetAsync<DashboardSummaryDto>(cacheKey);
            if (cachedSummary != null)
            {
                TotalTickets = cachedSummary.TotalTickets;
                NewTicketsCount = cachedSummary.NewTicketsCount;
                InProgressCount = cachedSummary.InProgressCount;
                OverdueCount = cachedSummary.OverdueCount;
                CriticalAndOverdueCount = cachedSummary.CriticalAndOverdueCount;
                SlaOnTimePercentage = cachedSummary.SlaOnTimePercentage;
                TrendData = cachedSummary.TrendData;
                PriorityStats = cachedSummary.PriorityStats;
                ProjectStats = cachedSummary.ProjectStats;
                RecentTickets = cachedSummary.RecentTickets;

                CachedTrendLinePath = BuildSvgLinePath(TrendData, 400, 120);
                CachedTrendAreaPath = BuildSvgAreaPath(TrendData, 400, 120);
                CachedModalTrendLinePath = BuildSvgLinePath(TrendData, 600, 200);
                CachedModalTrendAreaPath = BuildSvgAreaPath(TrendData, 600, 200);
                return;
            }
        }

        var (tickets, total) = await TicketService.GetFilteredTicketsAsync(
            searchTerm: null,
            projectIds: null,
            statusIds: null,
            userId: null,
            page: 1,
            pageSize: 100);

        TotalTickets = total;
        NewTicketsCount = tickets.Count(t => t.WorkflowStatus?.IsInitial == true || (t.WorkflowStatusId == null && t.Status?.Name == "Open"));
        InProgressCount = tickets.Count(t => t.WorkflowStatus != null && !t.WorkflowStatus.IsInitial);
        OverdueCount = tickets.Count(t => t.IsOverdue);
        CriticalAndOverdueCount = tickets.Count(t => (t.Priority != null && t.Priority.Level > 4) || t.IsOverdue);

        SlaOnTimePercentage = TotalTickets > 0 ? (int)Math.Round((double)(TotalTickets - OverdueCount) * 100 / TotalTickets) : 100;

        RecentTickets = tickets.Take(6).ToList();

        var dbPriorities = (await PriorityService.GetAllAsync()).Where(p => p.IsActive).OrderByDescending(p => p.Level).ToList();
        PriorityStats = dbPriorities.Select(p =>
        {
            int count = tickets.Count(t => t.PriorityId == p.Id);
            int pct = TotalTickets > 0 ? (int)Math.Round((double)count * 100 / TotalTickets) : 0;
            return new PriorityStatDto
            {
                PriorityId = p.Id,
                Name = p.Name,
                ColorCode = string.IsNullOrWhiteSpace(p.ColorCode) ? "#3B82F6" : p.ColorCode,
                Level = p.Level.GetValueOrDefault(),
                Count = count,
                Percentage = pct
            };
        }).ToList();

        var now = DateTime.UtcNow.Date;
        TrendData = Enumerable.Range(0, 7)
            .Select(i => now.AddDays(-6 + i))
            .Select(date => new DailyTrendDto
            {
                Date = date,
                DayLabel = GetPersianDayName(date),
                Count = tickets.Count(t => t.CreatedAt.Date == date)
            })
            .ToList();

        var projGroups = tickets
            .Where(t => t.Project != null)
            .GroupBy(t => t.Project!.Name)
            .Select(g => new ProjectWorkloadDto
            {
                ProjectName = g.Key,
                TicketCount = g.Count(),
                Percentage = TotalTickets > 0 ? (int)Math.Round((double)g.Count() * 100 / TotalTickets) : 0
            })
            .OrderByDescending(p => p.TicketCount)
            .ToList();

        ProjectStats = projGroups;

        // Memoize SVG Paths for zero-allocation Blazor renders
        CachedTrendLinePath = BuildSvgLinePath(TrendData, 400, 120);
        CachedTrendAreaPath = BuildSvgAreaPath(TrendData, 400, 120);
        CachedModalTrendLinePath = BuildSvgLinePath(TrendData, 600, 200);
        CachedModalTrendAreaPath = BuildSvgAreaPath(TrendData, 600, 200);

        // Store in sliding cache for 30 seconds
        var summaryToCache = new DashboardSummaryDto
        {
            TotalTickets = TotalTickets,
            NewTicketsCount = NewTicketsCount,
            InProgressCount = InProgressCount,
            OverdueCount = OverdueCount,
            CriticalAndOverdueCount = CriticalAndOverdueCount,
            SlaOnTimePercentage = SlaOnTimePercentage,
            TrendData = TrendData,
            PriorityStats = PriorityStats,
            ProjectStats = ProjectStats,
            RecentTickets = RecentTickets.ToList()
        };

        await CacheService.SetAsync(cacheKey, summaryToCache, TimeSpan.FromSeconds(30));
    }

    protected async Task OpenCreateModal()
    {
        var authState = await AuthState;
        var user = authState.User;
        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value;
        int currentUserId = int.TryParse(userIdString, out var id) ? id : 0;

        NewTicket = new TicketDto
        {
            StatusId = 1,
            PriorityId = 2,
            UserId = currentUserId,
            ProjectId = 0
        };

        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        Dispatcher.Dispatch(new LoadTicketInitialDataAction(roles));
        Dispatcher.Dispatch(new ClearTicketMessagesAction());
        Dispatcher.Dispatch(new DynamicFieldsLoadedAction(Array.Empty<TicketFieldDto>()));
        IsCreateModalOpen = true;
    }

    protected void CloseCreateModal()
    {
        IsCreateModalOpen = false;
    }

    protected void HandleCreateTicket()
    {
        if (NewTicket.ProjectId == 0) return;
        Dispatcher.Dispatch(new SaveTicketAction(NewTicket));
    }

    protected void HandleCategoryChanged(int? categoryId)
    {
        NewTicket.CategoryId = categoryId;

        if (categoryId.HasValue)
        {
            Dispatcher.Dispatch(new LoadDynamicFieldsAction(categoryId.Value));
        }
        else
        {
            Dispatcher.Dispatch(new DynamicFieldsLoadedAction(Array.Empty<TicketFieldDto>()));
        }
    }

    protected void OpenChartModal(string type)
    {
        ActiveModalType = type;
        ActiveModalTitle = type switch
        {
            "trend" => "تحلیل جامع روند ورودی تیکت‌ها",
            "priority" => "توزیع تفکیکی اولویت‌های سیستم",
            "project" => "سهم پروژه‌ها از لود کاری سیستم",
            "sla" => "پایش دقیق شاخص زمان‌بندی (SLA)",
            _ => "آمار تفکیکی"
        };
        IsChartModalOpen = true;
    }

    protected void CloseChartModal() => IsChartModalOpen = false;

    protected void NavigateToDetails(int id) => Navigation.NavigateTo($"/tickets/{id}");

    protected string BuildSvgLinePath(List<DailyTrendDto>? data, double width, double height)
    {
        if (data == null || !data.Any()) return string.Empty;
        int max = Math.Max(data.Max(d => d.Count), 1);
        double stepX = width / Math.Max(data.Count - 1, 1);
        var points = data.Select((d, i) =>
        {
            double x = i * stepX;
            double y = height - (d.Count * (height - 20) / max) - 10;
            return $"{x:F1},{y:F1}";
        });
        return "M " + string.Join(" L ", points);
    }

    protected string BuildSvgAreaPath(List<DailyTrendDto>? data, double width, double height)
    {
        var linePath = BuildSvgLinePath(data, width, height);
        if (string.IsNullOrEmpty(linePath)) return string.Empty;
        return $"{linePath} L {width:F1},{height:F1} L 0,{height:F1} Z";
    }

    protected string ActiveQuestTab { get; set; } = "all";
    protected void SetQuestTab(string tab) => ActiveQuestTab = tab;

    protected IEnumerable<TicketDto> FilteredRecentTickets
    {
        get
        {
            var list = RecentTickets ?? Enumerable.Empty<TicketDto>();
            return ActiveQuestTab switch
            {
                "overdue" => list.Where(t => t.IsOverdue),
                "critical" => list.Where(t => t.Priority?.Level >= 4 || t.Priority?.Name == "بحرانی"),
                _ => list
            };
        }
    }

    protected string GetPriorityClass(string priorityName) => priorityName switch
    {
        "بحرانی" => "bg-rose-950/80 text-rose-300 border border-rose-500/40 shadow-xs",
        "بالا" => "bg-amber-950/80 text-amber-300 border border-amber-500/40 shadow-xs",
        "متوسط" => "bg-sky-950/80 text-sky-300 border border-sky-500/40 shadow-xs",
        _ => "bg-slate-900 text-slate-300 border border-slate-700 shadow-xs"
    };

    protected string GetRarityTag(TicketDto ticket)
    {
        if (ticket.IsOverdue) return "🔥 BOSS RAID [OVERDUE]";
        if (ticket.Priority != null && ticket.Priority.Level > 4) return "🔴 LEGENDARY";
        if (ticket.Priority != null && ticket.Priority.Level >= 3) return "⚡ EPIC";
        if (ticket.Priority != null && ticket.Priority.Level == 2) return "🛡️ RARE";
        return "⚔️ COMMON";
    }

    protected string GetRarityTagClass(TicketDto ticket)
    {
        if (ticket.IsOverdue) return "bg-rose-950/80 text-rose-300 border-rose-500/60 shadow-[0_0_12px_rgba(244,63,94,0.4)] animate-pulse";
        if (ticket.Priority != null && ticket.Priority.Level > 4) return "bg-orange-950/80 text-orange-300 border-orange-500/60 shadow-[0_0_10px_rgba(249,115,22,0.3)]";
        if (ticket.Priority != null && ticket.Priority.Level >= 3) return "bg-purple-950/80 text-purple-300 border-purple-500/60 shadow-[0_0_10px_rgba(168,85,247,0.3)]";
        if (ticket.Priority != null && ticket.Priority.Level == 2) return "bg-sky-950/80 text-sky-300 border-sky-500/60 shadow-[0_0_10px_rgba(56,189,248,0.3)]";
        return "bg-emerald-950/80 text-emerald-300 border-emerald-500/60 shadow-[0_0_10px_rgba(16,185,129,0.3)]";
    }

    protected string GetSlaHpClass()
    {
        if (SlaOnTimePercentage < 60) return "hp-critical";
        if (SlaOnTimePercentage < 85) return "hp-warning";
        return "";
    }

    private string GetPersianDate(DateTime date) => date.ToPersianDateString();

    private string GetPersianDayName(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Saturday => "شنبه",
        DayOfWeek.Sunday => "۱شنبه",
        DayOfWeek.Monday => "۲شنبه",
        DayOfWeek.Tuesday => "۳شنبه",
        DayOfWeek.Wednesday => "۴شنبه",
        DayOfWeek.Thursday => "۵شنبه",
        DayOfWeek.Friday => "جمعه",
        _ => ""
    };
}
