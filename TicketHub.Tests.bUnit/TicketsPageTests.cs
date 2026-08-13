using Bunit;
using FluentValidation;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Web.Components.Pages.Main.Tickets;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit;

public class TicketsPageTests : BUnitComponentTestBase
{
    private readonly Mock<IState<TicketState>> _mockTicketState;
    private readonly Mock<IDispatcher> _mockDispatcher;
    private readonly Mock<ITicketService> _mockTicketService;

    public TicketsPageTests()
    {
        _mockTicketState = new Mock<IState<TicketState>>();
        _mockTicketState.Setup(s => s.Value).Returns(new TicketState(
            false,
            new List<TicketDto>
            {
                new() { Id = 1, Title = "تیکت تستی ۱", ProjectId = 1, StatusId = 1, PriorityId = 1, CreatedAt = DateTime.UtcNow, Project = new ProjectDto { Name = "پروژه ۱" }, Status = new StatusDto { Name = "جدید" } },
                new() { Id = 2, Title = "تیکت تستی ۲", ProjectId = 1, StatusId = 1, PriorityId = 1, CreatedAt = DateTime.UtcNow, Project = new ProjectDto { Name = "پروژه ۱" }, Status = new StatusDto { Name = "جدید" } }
            },
            18,
            new List<ProjectDto> { new() { Id = 1, Name = "پروژه ۱" } },
            new List<StatusDto> { new() { Id = 1, Name = "جدید" } },
            new List<PriorityDto> { new() { Id = 1, Name = "عادی" } },
            new List<CategoryDto> { new() { Id = 1, Name = "عمومی" } },
            string.Empty,
            6,
            1,
            new List<int>(),
            new List<int>(),
            new List<int>(),
            new List<TicketFieldDto>(),
            new TicketTelemetrySummaryDto { TotalTickets = 18 },
            "createdAt",
            false
        ));
        Services.AddSingleton(_mockTicketState.Object);

        _mockDispatcher = new Mock<IDispatcher>();
        Services.AddSingleton(_mockDispatcher.Object);

        var mockActionSubscriber = new Mock<IActionSubscriber>();
        Services.AddSingleton(mockActionSubscriber.Object);

        _mockTicketService = new Mock<ITicketService>();
        Services.AddSingleton(_mockTicketService.Object);

        var mockWorkflowService = new Mock<IWorkflowService>();
        Services.AddSingleton(mockWorkflowService.Object);

        var mockRoleService = new Mock<IRoleService>();
        Services.AddSingleton(mockRoleService.Object);

        var mockToastService = new Mock<IToastService>();
        Services.AddSingleton(mockToastService.Object);

        var mockTransitionValidator = new Mock<IValidator<ExecuteTransitionDto>>();
        Services.AddSingleton(mockTransitionValidator.Object);

        var mockTicketValidator = new Mock<IValidator<TicketDto>>();
        Services.AddSingleton(mockTicketValidator.Object);
    }

    [Fact]
    public void Render_TicketsPage_RendersHeader_AndPagination()
    {
        var cut = Render<Tickets>();

        // Assert page header
        Assert.NotNull(cut.Find("h1:contains('مدیریت تیکت‌ها')"));

        // Assert pagination is rendered and displays TotalItems 18
        var paginationEl = cut.Find("span:contains('18')");
        Assert.NotNull(paginationEl);

        // Assert default page size in dropdown button contains 6
        var pageSizeButton = cut.Find("button:contains('6 تیکت در صفحه')");
        Assert.NotNull(pageSizeButton);
    }

    [Fact]
    public void Pagination_NextPageButton_DispatchesFilterAction()
    {
        var cut = Render<Tickets>();

        var nextButton = cut.Find("button:contains('بعدی')");
        nextButton.Click();

        _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetTicketFiltersAction>(a => a.CurrentPage == 2)), Times.Once);
    }

    [Fact]
    public void ChangeFilter_DispatchesFilterAction_WithPageResetToOne()
    {
        var cut = Render<Tickets>();

        var searchInput = cut.Find("input[placeholder='جستجو در عنوان تیکت‌ها...']");
        searchInput.Input("خطا");

        _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetTicketFiltersAction>(a => a.SearchTerm == "خطا" && a.CurrentPage == 1)), Times.Once);
    }

    [Fact]
    public async Task HandleLoadTickets_AutoCorrectsPage_WhenCurrentPageExceedsMaxPage()
    {
        // Setup state where user is on Page 3, PageSize is 6, but total count drops to 6 (maxPage = 1)
        var stateMock = new Mock<IState<TicketState>>();
        stateMock.Setup(s => s.Value).Returns(new TicketState(
            false,
            new List<TicketDto>(),
            6,
            new List<ProjectDto>(),
            new List<StatusDto>(),
            new List<PriorityDto>(),
            new List<CategoryDto>(),
            string.Empty,
            6,
            3, // CurrentPage is 3, but only 6 items exist (1 page)
            new List<int>(),
            new List<int>(),
            new List<int>(),
            new List<TicketFieldDto>(),
            new TicketTelemetrySummaryDto { TotalTickets = 6 },
            "createdAt",
            false
        ));

        var returnTickets = new List<TicketDto> { new() { Id = 1, Title = "تیکت ۱" } };
        _mockTicketService.Setup(s => s.GetFilteredTicketsAsync(It.IsAny<string?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<int?>(), 3, 6, It.IsAny<System.Security.Claims.ClaimsPrincipal?>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync((new List<TicketDto>(), 6));
        _mockTicketService.Setup(s => s.GetFilteredTicketsAsync(It.IsAny<string?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<int?>(), 1, 6, It.IsAny<System.Security.Claims.ClaimsPrincipal?>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync((returnTickets, 6));
        _mockTicketService.Setup(s => s.GetTicketTelemetrySummaryAsync(It.IsAny<string?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<List<int>?>(), It.IsAny<int?>()))
            .ReturnsAsync(new TicketTelemetrySummaryDto { TotalTickets = 6 });

        var effects = new TicketEffects(
            _mockTicketService.Object,
            Mock.Of<IProjectService>(),
            Mock.Of<IStatusService>(),
            Mock.Of<IPriorityService>(),
            Mock.Of<ICategoryService>(),
            stateMock.Object,
            Mock.Of<ITicketFieldService>(),
            Mock.Of<ILogger<TicketEffects>>(),
            Mock.Of<IToastService>()
        );

        var dispatcherMock = new Mock<IDispatcher>();
        await effects.HandleLoadTickets(dispatcherMock.Object);

        // Verify TicketsLoadedAction was dispatched with finalPage = 1
        dispatcherMock.Verify(d => d.Dispatch(It.Is<TicketsLoadedAction>(a => a.ValidatedPage == 1 && a.TotalCount == 6)), Times.Once);
    }
}
