using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using FluentValidation;
using Fluxor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Interfaces;
using TicketHub.Web.Components.Pages.Main;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit;

public class HomePageTests : BUnitComponentTestBase
{
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<IProjectService> _mockProjectService;
    private readonly Mock<IPriorityService> _mockPriorityService;
    private readonly Mock<IStatusService> _mockStatusService;
    private readonly Mock<IState<TicketState>> _mockTicketState;
    private readonly Mock<IDispatcher> _mockDispatcher;
    private readonly Mock<IActionSubscriber> _mockActionSubscriber;
    private readonly Mock<ITicketEventBroker> _mockEventBroker;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

    private Action<SaveTicketSuccessAction>? _saveTicketCallback;
    private Action<TicketsLoadedAction>? _ticketsLoadedCallback;

    public HomePageTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _mockProjectService = new Mock<IProjectService>();
        _mockPriorityService = new Mock<IPriorityService>();
        _mockStatusService = new Mock<IStatusService>();
        _mockTicketState = new Mock<IState<TicketState>>();
        _mockDispatcher = new Mock<IDispatcher>();
        _mockActionSubscriber = new Mock<IActionSubscriber>();
        _mockEventBroker = new Mock<ITicketEventBroker>();
        _mockCacheService = new Mock<ICacheService>();
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();

        // Setup Authenticated User State
        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "مدیر کل سیستم"),
            new Claim(ClaimTypes.Email, "admin@tickethub.io"),
            new Claim(ClaimTypes.Role, "ادمین")
        }, "TestAuth"));

        var authState = new AuthenticationState(userClaims);
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        // Setup TicketState
        var initialTicketState = new TicketState(
            false,
            new List<TicketDto>(),
            10,
            new List<ProjectDto> { new() { Id = 1, Name = "پروژه عمومی" } },
            new List<StatusDto> { new() { Id = 1, Name = "جدید" } },
            new List<PriorityDto> { new() { Id = 1, Name = "بحرانی", Level = 4, ColorCode = "#ef4444" } },
            new List<CategoryDto> { new() { Id = 1, Name = "عمومی" } },
            string.Empty,
            6,
            1,
            new List<int>(),
            new List<int>(),
            new List<int>(),
            new List<TicketFieldDto>(),
            new TicketTelemetrySummaryDto
            {
                TotalTickets = 10,
                NewTicketsCount = 5,
                InProgressCount = 3,
                CriticalCount = 2,
                OverdueCount = 1,
                ResolvedCount = 2,
                SlaOnTimePercentage = 90
            },
            "createdAt",
            false
        );
        _mockTicketState.Setup(s => s.Value).Returns(initialTicketState);

        // Setup Default TicketService mocks
        _mockTicketService.Setup(t => t.GetTicketTelemetrySummaryAsync(
            It.IsAny<string?>(),
            It.IsAny<List<int>?>(),
            It.IsAny<List<int>?>(),
            It.IsAny<List<int>?>(),
            It.IsAny<int?>(),
            It.IsAny<ClaimsPrincipal?>()))
            .ReturnsAsync(new TicketTelemetrySummaryDto
            {
                TotalTickets = 10,
                NewTicketsCount = 5,
                InProgressCount = 3,
                CriticalCount = 2,
                OverdueCount = 1,
                ResolvedCount = 2,
                SlaOnTimePercentage = 90
            });

        var sampleTickets = new List<TicketDto>
        {
            new() { Id = 1, Title = "تیکت ۱", ProjectId = 1, PriorityId = 1, CreatedAt = DateTime.UtcNow, Project = new ProjectDto { Name = "پروژه عمومی" }, Priority = new PriorityDto { Level = 4, Name = "بحرانی", ColorCode = "#ef4444" } },
            new() { Id = 2, Title = "تیکت ۲", ProjectId = 1, PriorityId = 2, CreatedAt = DateTime.UtcNow, Project = new ProjectDto { Name = "پروژه عمومی" }, Priority = new PriorityDto { Level = 2, Name = "متوسط", ColorCode = "#f59e0b" } }
        };

        _mockTicketService.Setup(t => t.GetFilteredTicketsAsync(
            It.IsAny<string?>(),
            It.IsAny<List<int>?>(),
            It.IsAny<List<int>?>(),
            It.IsAny<List<int>?>(),
            It.IsAny<int?>(),
            1,
            100,
            It.IsAny<ClaimsPrincipal?>(),
            It.IsAny<string?>(),
            It.IsAny<bool>()))
            .ReturnsAsync((sampleTickets, 10));

        // Setup Priorities
        _mockPriorityService.Setup(p => p.GetAllAsync())
            .ReturnsAsync(new List<PriorityDto>
            {
                new() { Id = 1, Name = "بحرانی", Level = 4, ColorCode = "#ef4444", IsActive = true },
                new() { Id = 2, Name = "متوسط", Level = 2, ColorCode = "#f59e0b", IsActive = true }
            });

        // Setup Action Subscriber Callbacks capture
        _mockActionSubscriber.Setup(s => s.SubscribeToAction<SaveTicketSuccessAction>(It.IsAny<object>(), It.IsAny<Action<SaveTicketSuccessAction>>()))
            .Callback<object, Action<SaveTicketSuccessAction>>((target, action) => _saveTicketCallback = action);

        _mockActionSubscriber.Setup(s => s.SubscribeToAction<TicketsLoadedAction>(It.IsAny<object>(), It.IsAny<Action<TicketsLoadedAction>>()))
            .Callback<object, Action<TicketsLoadedAction>>((target, action) => _ticketsLoadedCallback = action);

        // Register services in DI
        Services.AddSingleton(_mockTicketService.Object);
        Services.AddSingleton(_mockProjectService.Object);
        Services.AddSingleton(_mockPriorityService.Object);
        Services.AddSingleton(_mockStatusService.Object);
        Services.AddSingleton(_mockTicketState.Object);
        Services.AddSingleton(_mockDispatcher.Object);
        Services.AddSingleton(_mockActionSubscriber.Object);
        Services.AddSingleton(_mockEventBroker.Object);
        Services.AddSingleton(_mockCacheService.Object);
        Services.AddSingleton(_mockAuthStateProvider.Object);

        // Child component services
        Services.AddSingleton(Mock.Of<IWorkflowService>());
        Services.AddSingleton(Mock.Of<IRoleService>());
        Services.AddSingleton(Mock.Of<IToastService>());
        Services.AddSingleton(Mock.Of<IValidator<TicketDto>>());
        Services.AddSingleton(Mock.Of<IValidator<ExecuteTransitionDto>>());
    }

    [Fact]
    public void Render_Dashboard_DisplaysWelcomeBanner_AndUserName()
    {
        var cut = Render<Home>();

        Assert.Contains("خوش آمدید، مدیر کل سیستم", cut.Markup);
        Assert.Contains("مرکز پایش و حل مشکلات بازیکنان", cut.Markup);
        Assert.Contains("ثبت کوئست / تیکت جدید", cut.Markup);
    }

    [Fact]
    public void Render_Dashboard_Dispatches_LoadTicketInitialDataAction_WithUserRoles()
    {
        Render<Home>();

        _mockDispatcher.Verify(d => d.Dispatch(It.Is<LoadTicketInitialDataAction>(a =>
            a.UserRoles != null && a.UserRoles.Contains("ادمین"))), Times.Once);
    }

    [Fact]
    public void Render_Dashboard_DisplaysAll6ElementalCards_WithCorrectValues()
    {
        var cut = Render<Home>();

        // 1. Water (All tickets)
        Assert.Contains("کل تیکت‌ها", cut.Markup);
        Assert.Contains("🌊 ALL", cut.Markup);
        Assert.Contains("10", cut.Markup);

        // 2. Lightning (New tickets)
        Assert.Contains("اقدام نشده", cut.Markup);
        Assert.Contains("⚡ NEW", cut.Markup);
        Assert.Contains("5", cut.Markup);

        // 3. Toxic (In progress)
        Assert.Contains("در حال بررسی", cut.Markup);
        Assert.Contains("🧪 ACTIVE", cut.Markup);
        Assert.Contains("3", cut.Markup);

        // 4. Fire (Critical)
        Assert.Contains("بحرانی", cut.Markup);
        Assert.Contains("🔥 CRITICAL", cut.Markup);
        Assert.Contains("2", cut.Markup);

        // 5. Void (Overdue)
        Assert.Contains("منقضی شده", cut.Markup);
        Assert.Contains("🌀 OVERDUE", cut.Markup);
        Assert.Contains("1", cut.Markup);

        // 6. Smoke (Resolved)
        Assert.Contains("خاتمه یافته", cut.Markup);
        Assert.Contains("💨 RESOLVED", cut.Markup);
        Assert.Contains("2", cut.Markup);
    }

    [Fact]
    public void Render_Dashboard_DisplaysTrendChart_And_PriorityDistribution()
    {
        var cut = Render<Home>();

        // Trend chart
        Assert.Contains("روند ورودی تیکت‌ها (۷ روز گذشته)", cut.Markup);
        Assert.Contains("نمودار لیزری نوسانات ترافیک و لاگ هفتگی", cut.Markup);

        // Priority Distribution
        Assert.Contains("توزیع تیکت‌ها بر اساس اولویت و رنک", cut.Markup);
        Assert.Contains("بحرانی", cut.Markup);
        Assert.Contains("[LVL 4]", cut.Markup);
    }

    [Fact]
    public void Render_Dashboard_DisplaysProjectWorkload_And_SlaHealthBar()
    {
        var cut = Render<Home>();

        // Project Workload
        Assert.Contains("سهم بخش‌ها و قلمروها از کل تیکت‌ها", cut.Markup);
        Assert.Contains("پروژه عمومی", cut.Markup);

        // SLA Health Bar
        Assert.Contains("نوار سلامت و پاسخگویی به موقع (SLA HP)", cut.Markup);
        Assert.Contains("🛡️ OPTIMAL (ایمن)", cut.Markup);
        Assert.Contains("90% HP", cut.Markup);
    }

    [Fact]
    public void SlaHealthBar_DisplaysWarning_WhenSlaBetween60And84()
    {
        _mockTicketService.Setup(t => t.GetTicketTelemetrySummaryAsync(
            It.IsAny<string?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<int?>(), It.IsAny<ClaimsPrincipal?>()))
            .ReturnsAsync(new TicketTelemetrySummaryDto
            {
                TotalTickets = 10,
                OverdueCount = 3,
                SlaOnTimePercentage = 70
            });

        _mockTicketState.Setup(s => s.Value).Returns(new TicketState(
            false, new List<TicketDto>(), 10, new List<ProjectDto>(), new List<StatusDto>(), new List<PriorityDto>(), new List<CategoryDto>(),
            string.Empty, 6, 1, new List<int>(), new List<int>(), new List<int>(), new List<TicketFieldDto>(),
            new TicketTelemetrySummaryDto { TotalTickets = 10, OverdueCount = 3, SlaOnTimePercentage = 70 },
            "createdAt", false));

        var cut = Render<Home>();

        Assert.Contains("⚠️ WARNING (هشدار)", cut.Markup);
        Assert.Contains("70% HP", cut.Markup);
    }

    [Fact]
    public void SlaHealthBar_DisplaysCritical_WhenSlaBelow60()
    {
        _mockTicketService.Setup(t => t.GetTicketTelemetrySummaryAsync(
            It.IsAny<string?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<int?>(), It.IsAny<ClaimsPrincipal?>()))
            .ReturnsAsync(new TicketTelemetrySummaryDto
            {
                TotalTickets = 10,
                OverdueCount = 6,
                SlaOnTimePercentage = 40
            });

        _mockTicketState.Setup(s => s.Value).Returns(new TicketState(
            false, new List<TicketDto>(), 10, new List<ProjectDto>(), new List<StatusDto>(), new List<PriorityDto>(), new List<CategoryDto>(),
            string.Empty, 6, 1, new List<int>(), new List<int>(), new List<int>(), new List<TicketFieldDto>(),
            new TicketTelemetrySummaryDto { TotalTickets = 10, OverdueCount = 6, SlaOnTimePercentage = 40 },
            "createdAt", false));

        var cut = Render<Home>();

        Assert.Contains("🚨 CRITICAL (بحرانی)", cut.Markup);
        Assert.Contains("40% HP", cut.Markup);
    }

    [Fact]
    public void OpenCreateTicketModal_DispatchesInitialDataActions()
    {
        var cut = Render<Home>();

        var createButton = cut.Find("button:contains('ثبت کوئست / تیکت جدید')");
        createButton.Click();

        _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<LoadTicketInitialDataAction>()), Times.AtLeast(2));
        _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<ClearTicketMessagesAction>()), Times.Once);
        _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<DynamicFieldsLoadedAction>()), Times.Once);
    }

    [Fact]
    public void OpenChartModal_TrendCardClick_OpensTrendModal_AndClosesOnButtonClick()
    {
        var cut = Render<Home>();

        // Find and click the Trend Chart card
        var trendCard = cut.FindAll("div.element-card.gamer-card-3d").First(el => el.TextContent.Contains("روند ورودی"));
        trendCard.Click();

        // Verify Chart Zoom Modal renders in SectionOutlet
        Assert.NotNull(cut.Find("h3:contains('تحلیل جامع روند ورودی تیکت‌ها')"));
        Assert.NotNull(cut.Find("span:contains('SYS-TELEMETRY')"));

        // Click close button
        var closeButton = cut.Find("button:contains('بستن پنجره')");
        closeButton.Click();

        // Verify modal is closed
        Assert.Empty(cut.FindAll("h3:contains('تحلیل جامع روند ورودی تیکت‌ها')"));
    }

    [Fact]
    public void OpenChartModal_PriorityCardClick_OpensPriorityModal()
    {
        var cut = Render<Home>();

        var priorityCard = cut.FindAll("div.element-card.gamer-card-3d").First(el => el.TextContent.Contains("توزیع تیکت‌ها"));
        priorityCard.Click();

        Assert.NotNull(cut.Find("h3:contains('توزیع تفکیکی اولویت‌های سیستم')"));
        Assert.NotNull(cut.Find("span:contains('سطح اولویت: 4')"));
    }

    [Fact]
    public void OpenChartModal_ProjectCardClick_OpensProjectModal()
    {
        var cut = Render<Home>();

        var projectCard = cut.FindAll("div.element-card.gamer-card-3d").First(el => el.TextContent.Contains("سهم بخش‌ها"));
        projectCard.Click();

        Assert.NotNull(cut.Find("h3:contains('سهم پروژه‌ها از لود کاری سیستم')"));
        Assert.NotNull(cut.Find("span:contains('پروژه عمومی')"));
    }

    [Fact]
    public void OpenChartModal_SlaCardClick_OpensSlaModal()
    {
        var cut = Render<Home>();

        var slaCard = cut.FindAll("div.element-card.gamer-card-3d").First(el => el.TextContent.Contains("نوار سلامت"));
        slaCard.Click();

        Assert.NotNull(cut.Find("h3:contains('پایش دقیق شاخص زمان‌بندی (SLA)')"));
        Assert.NotNull(cut.Find("span:contains('میزان پایبندی به زمان‌بندی (SLA)')"));
    }

    [Fact]
    public void Fluxor_SaveTicketSuccessAction_ClosesModal_AndRefreshesDashboard()
    {
        var cut = Render<Home>();

        // Open modal first
        cut.Find("button:contains('ثبت کوئست / تیکت جدید')").Click();

        // Trigger action callback
        Assert.NotNull(_saveTicketCallback);
        cut.InvokeAsync(() => _saveTicketCallback!(new SaveTicketSuccessAction()));

        // Verify ticket service was refreshed
        _mockTicketService.Verify(t => t.GetTicketTelemetrySummaryAsync(
            It.IsAny<string?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<int?>(), It.IsAny<ClaimsPrincipal?>()),
            Times.AtLeast(2));
    }

    [Fact]
    public void Fluxor_TicketsLoadedAction_UpdatesTelemetryStats()
    {
        var cut = Render<Home>();

        var updatedTelemetry = new TicketTelemetrySummaryDto
        {
            TotalTickets = 45,
            NewTicketsCount = 12,
            InProgressCount = 18,
            ResolvedCount = 15,
            CriticalCount = 8,
            OverdueCount = 2,
            SlaOnTimePercentage = 95
        };

        Assert.NotNull(_ticketsLoadedCallback);
        cut.InvokeAsync(() => _ticketsLoadedCallback!(new TicketsLoadedAction(new List<TicketDto>(), 45, 1, updatedTelemetry)));

        Assert.Contains("45", cut.Markup);
    }

    [Fact]
    public async Task CacheService_LoadsFromCache_WhenCachedSummaryExists()
    {
        var cachedSummary = new DashboardSummaryDto
        {
            TotalTickets = 99,
            NewTicketsCount = 20,
            InProgressCount = 30,
            ResolvedCount = 49,
            CriticalCount = 5,
            OverdueCount = 1,
            SlaOnTimePercentage = 98,
            TrendData = new List<DailyTrendDto>
            {
                new() { Date = DateTime.UtcNow.Date, DayLabel = "امروز", Count = 15 }
            },
            PriorityStats = new List<PriorityStatDto>
            {
                new() { PriorityId = 1, Name = "بحرانی", Count = 5, Percentage = 5, ColorCode = "#ef4444" }
            },
            ProjectStats = new List<ProjectWorkloadDto>
            {
                new() { ProjectName = "پروژه کش شده", TicketCount = 99, Percentage = 100 }
            },
            RecentTickets = new List<TicketDto>()
        };

        _mockCacheService.Setup(c => c.GetAsync<DashboardSummaryDto>("dashboard_summary_1"))
            .ReturnsAsync(cachedSummary);

        var cut = Render<Home>();

        Assert.Contains("99", cut.Markup);
        Assert.Contains("پروژه کش شده", cut.Markup);
    }

    [Fact]
    public void HandleCategoryChanged_Dispatches_LoadDynamicFieldsAction_WhenCategoryIdProvided()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;

        home.HandleCategoryChanged(5);

        _mockDispatcher.Verify(d => d.Dispatch(It.Is<LoadDynamicFieldsAction>(a => a.CategoryId == 5)), Times.Once);
    }

    [Fact]
    public void HandleCategoryChanged_Dispatches_DynamicFieldsLoadedAction_WhenCategoryCleared()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;

        home.HandleCategoryChanged(null);

        _mockDispatcher.Verify(d => d.Dispatch(It.Is<DynamicFieldsLoadedAction>(a => !a.Fields.Any())), Times.Once);
    }

    [Fact]
    public void HandleCreateTicket_GuardsAgainstZeroProjectId()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;

        home.SetNewTicket(new TicketDto { ProjectId = 0 });
        home.HandleCreateTicket();

        _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<SaveTicketAction>()), Times.Never);
    }

    [Fact]
    public void HandleCreateTicket_DispatchesSaveTicketAction_WhenProjectIdIsValid()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;

        home.SetNewTicket(new TicketDto { ProjectId = 2, Title = "کوئست تست" });
        home.HandleCreateTicket();

        _mockDispatcher.Verify(d => d.Dispatch(It.Is<SaveTicketAction>(a => a.Ticket.ProjectId == 2 && a.Ticket.Title == "کوئست تست")), Times.Once);
    }

    [Fact]
    public void HelperMethods_RarityAndSlaFormatting_CalculatesCorrectOutputs()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;

        // Rarity Tag Tests
        var overdueTicket = new TicketDto { DueDate = DateTime.UtcNow.AddHours(-2) };
        Assert.Contains("BOSS RAID", home.GetRarityTag(overdueTicket));
        Assert.Contains("bg-rose-950", home.GetRarityTagClass(overdueTicket));

        var epicTicket = new TicketDto { Priority = new PriorityDto { Level = 4, Name = "بحرانی" } };
        Assert.Contains("EPIC", home.GetRarityTag(epicTicket));

        var rareTicket = new TicketDto { Priority = new PriorityDto { Level = 3, Name = "زیاد" } };
        Assert.Contains("RARE", home.GetRarityTag(rareTicket));

        var uncommonTicket = new TicketDto { Priority = new PriorityDto { Level = 2, Name = "متوسط" } };
        Assert.Contains("UNCOMMON", home.GetRarityTag(uncommonTicket));

        var commonTicket = new TicketDto { Priority = new PriorityDto { Level = 1, Name = "کم" } };
        Assert.Contains("COMMON", home.GetRarityTag(commonTicket));

        // Priority CSS Class Tests
        Assert.Contains("purple", home.GetPriorityClass("بحرانی"));
        Assert.Contains("rose", home.GetPriorityClass("زیاد"));
        Assert.Contains("amber", home.GetPriorityClass("متوسط"));
        Assert.Contains("emerald", home.GetPriorityClass("کم"));

        // SLA Remaining formatting
        var noDueDateTicket = new TicketDto { DueDate = null };
        Assert.Equal("بدون مهلت", home.GetSlaRemainingTimeText(noDueDateTicket));

        var pastTicket = new TicketDto { DueDate = DateTime.UtcNow.AddMinutes(-30) };
        Assert.Contains("گذشته", home.GetSlaRemainingTimeText(pastTicket));

        var futureTicket = new TicketDto { DueDate = DateTime.UtcNow.AddHours(2) };
        Assert.Contains("مانده", home.GetSlaRemainingTimeText(futureTicket));

        // SVG Path Math
        var sampleTrend = new List<DailyTrendDto>
        {
            new() { Date = DateTime.UtcNow.AddDays(-1), Count = 5 },
            new() { Date = DateTime.UtcNow, Count = 10 }
        };
        var linePath = home.BuildSvgLinePath(sampleTrend, 400, 120);
        var areaPath = home.BuildSvgAreaPath(sampleTrend, 400, 120);

        Assert.StartsWith("M ", linePath);
        Assert.Contains(" Z", areaPath);

        // Null and Empty SVG paths
        Assert.Equal(string.Empty, home.BuildSvgLinePath(null, 400, 120));
        Assert.Equal(string.Empty, home.BuildSvgLinePath(new List<DailyTrendDto>(), 400, 120));
        Assert.Equal(string.Empty, home.BuildSvgAreaPath(null, 400, 120));
    }

    [Fact]
    public void QuestTabs_FiltersRecentTicketsCorrectly()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;

        var tickets = new List<TicketDto>
        {
            new() { Id = 1, Title = "تیکت معوقه", DueDate = DateTime.UtcNow.AddDays(-1), Priority = new PriorityDto { Level = 1 } },
            new() { Id = 2, Title = "تیکت بحرانی", Priority = new PriorityDto { Level = 4, Name = "بحرانی" } },
            new() { Id = 3, Title = "تیکت عادی", Priority = new PriorityDto { Level = 2, Name = "متوسط" } }
        };

        home.SetRecentTickets(tickets);

        // Tab: all
        home.SetQuestTab("all");
        Assert.Equal(3, home.GetFilteredRecentTickets().Count());

        // Tab: overdue
        home.SetQuestTab("overdue");
        var overdueList = home.GetFilteredRecentTickets().ToList();
        Assert.Single(overdueList);
        Assert.Equal("تیکت معوقه", overdueList[0].Title);

        // Tab: critical
        home.SetQuestTab("critical");
        var criticalList = home.GetFilteredRecentTickets().ToList();
        Assert.Single(criticalList);
        Assert.Equal("تیکت بحرانی", criticalList[0].Title);
    }

    [Fact]
    public void ChartModalBeamClass_ReturnsCorrectStyling_ForEachModalType()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;

        home.SetActiveModalType("trend");
        Assert.Equal("modal-dual-beam-cyan-indigo", home.GetChartModalBeamClass());

        home.SetActiveModalType("priority");
        Assert.Equal("modal-dual-beam-purple-amber", home.GetChartModalBeamClass());

        home.SetActiveModalType("project");
        Assert.Equal("modal-dual-beam-emerald-purple", home.GetChartModalBeamClass());

        home.SetActiveModalType("sla");
        Assert.Equal("modal-dual-beam-emerald-rose", home.GetChartModalBeamClass());

        home.SetActiveModalType("unknown");
        Assert.Equal("modal-laser-ring", home.GetChartModalBeamClass());
    }

    [Fact]
    public void SlaHpClass_ReturnsStatusBasedOnHealthPercentage()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;

        home.SetSlaOnTimePercentage(50);
        Assert.Equal("hp-critical", home.GetSlaHpClass());

        home.SetSlaOnTimePercentage(75);
        Assert.Equal("hp-warning", home.GetSlaHpClass());

        home.SetSlaOnTimePercentage(95);
        Assert.Equal(string.Empty, home.GetSlaHpClass());
    }

    [Fact]
    public void CapsuleBorderGlowClass_ReturnsExpectedGlows()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;

        var overdue = new TicketDto { DueDate = DateTime.UtcNow.AddDays(-1) };
        Assert.Contains("border-rose-500", home.GetCapsuleBorderGlowClass(overdue));

        var bossLevel = new TicketDto { Priority = new PriorityDto { Level = 5 } };
        Assert.Contains("border-orange-500", home.GetCapsuleBorderGlowClass(bossLevel));

        var highPriority = new TicketDto { Priority = new PriorityDto { Level = 3 } };
        Assert.Contains("border-purple-500", home.GetCapsuleBorderGlowClass(highPriority));

        var mediumPriority = new TicketDto { Priority = new PriorityDto { Level = 2 } };
        Assert.Contains("border-sky-500", home.GetCapsuleBorderGlowClass(mediumPriority));

        var lowPriority = new TicketDto { Priority = new PriorityDto { Level = 1 } };
        Assert.Contains("border-emerald-500", home.GetCapsuleBorderGlowClass(lowPriority));
    }

    [Fact]
    public void RealTimeEventBroker_TriggersDashboardRefresh_OnTicketUpdated_And_OnTransitionOccurred()
    {
        var cut = Render<Home>();

        // Raise OnTicketUpdated
        _mockEventBroker.Raise(e => e.OnTicketUpdated += null, 1);
        _mockTicketService.Verify(t => t.GetTicketTelemetrySummaryAsync(
            It.IsAny<string?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<int?>(), It.IsAny<ClaimsPrincipal?>()),
            Times.AtLeast(2));

        // Raise OnTransitionOccurred
        _mockEventBroker.Raise(e => e.OnTransitionOccurred += null, 2);
        _mockTicketService.Verify(t => t.GetTicketTelemetrySummaryAsync(
            It.IsAny<string?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<int?>(), It.IsAny<ClaimsPrincipal?>()),
            Times.AtLeast(3));
    }

    [Fact]
    public void Component_Dispose_UnsubscribesFromActions_And_Events()
    {
        var cut = Render<Home>();

        // Act: Dispose the component
        cut.Instance.Dispose();

        // Assert: Unsubscribe from all actions was invoked
        _mockActionSubscriber.Verify(a => a.UnsubscribeFromAllActions(cut.Instance), Times.Once);
    }

    [Fact]
    public void NavigateToDetails_NavigatesToExpectedTicketUrl()
    {
        var cut = Render<TestableHome>();
        var home = cut.Instance;
        var nav = cut.Services.GetRequiredService<NavigationManager>();

        home.NavigateToDetails(42);

        Assert.EndsWith("/tickets/42", nav.Uri);
    }
}

public class TestableHome : Home
{
    public new string GetPriorityClass(string name) => base.GetPriorityClass(name);
    public new string GetRarityTag(TicketDto ticket) => base.GetRarityTag(ticket);
    public new string GetRarityTagClass(TicketDto ticket) => base.GetRarityTagClass(ticket);
    public new string GetSlaRemainingTimeText(TicketDto ticket) => base.GetSlaRemainingTimeText(ticket);
    public new string GetSlaRemainingClass(TicketDto ticket) => base.GetSlaRemainingClass(ticket);
    public new string GetCapsuleBorderGlowClass(TicketDto ticket) => base.GetCapsuleBorderGlowClass(ticket);
    public new string BuildSvgLinePath(List<DailyTrendDto>? data, double width, double height) => base.BuildSvgLinePath(data, width, height);
    public new string BuildSvgAreaPath(List<DailyTrendDto>? data, double width, double height) => base.BuildSvgAreaPath(data, width, height);
    public new void HandleCategoryChanged(int? categoryId) => base.HandleCategoryChanged(categoryId);
    public new void HandleCreateTicket() => base.HandleCreateTicket();
    public TicketDto GetNewTicket() => base.NewTicket;
    public void SetNewTicket(TicketDto t) => base.NewTicket = t;
    public new string GetChartModalBeamClass() => base.GetChartModalBeamClass();
    public new string GetSlaHpClass() => base.GetSlaHpClass();
    public void SetActiveModalType(string type) => base.ActiveModalType = type;
    public void SetSlaOnTimePercentage(int pct) => base.SlaOnTimePercentage = pct;
    public new void SetQuestTab(string tab) => base.SetQuestTab(tab);
    public IEnumerable<TicketDto> GetFilteredRecentTickets() => base.FilteredRecentTickets;
    public void SetRecentTickets(IEnumerable<TicketDto> tickets) => base.RecentTickets = tickets;
    public new void NavigateToDetails(int id) => base.NavigateToDetails(id);
}
