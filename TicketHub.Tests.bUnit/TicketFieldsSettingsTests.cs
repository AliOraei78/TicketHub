using Bunit;
using FluentAssertions;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Generic;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.TicketFields;
using TicketHub.Web.Components.Shared;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class TicketFieldsSettingsTests : BUnitComponentTestBase
    {
        private readonly Mock<IState<TicketFieldState>> _mockState;
        private readonly Mock<IState<CategoryState>> _mockCatState;
        private readonly Mock<IState<FieldTypeState>> _mockTypeState;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IActionSubscriber> _mockActionSubscriber;

        public TicketFieldsSettingsTests()
        {
            _mockState = new Mock<IState<TicketFieldState>>();
            _mockState.Setup(s => s.Value).Returns(new TicketFieldState(
                false,
                new List<TicketFieldDto>(),
                string.Empty,
                null,
                new List<int>(),
                new List<int>()
            ));

            _mockCatState = new Mock<IState<CategoryState>>();
            _mockCatState.Setup(s => s.Value).Returns(new CategoryState(
                false,
                new List<CategoryDto>(),
                string.Empty,
                null,
                new List<int>()
            ));

            _mockTypeState = new Mock<IState<FieldTypeState>>();
            _mockTypeState.Setup(s => s.Value).Returns(new FieldTypeState(
                false,
                new List<FieldTypeDto>
                {
                    new FieldTypeDto { Id = 1, Type = "Text", IsActive = true },
                    new FieldTypeDto { Id = 2, Type = "Number", IsActive = true }
                }
            ));

            _mockDispatcher = new Mock<IDispatcher>();
            _mockActionSubscriber = new Mock<IActionSubscriber>();

            Services.AddSingleton(_mockState.Object);
            Services.AddSingleton(_mockCatState.Object);
            Services.AddSingleton(_mockTypeState.Object);
            Services.AddSingleton(_mockDispatcher.Object);
            Services.AddSingleton(_mockActionSubscriber.Object);
        }

        [Fact]
        public void Render_SettingsPage_ShowsHeaderAndTelemetryCards()
        {
            var fields = new List<TicketFieldDto>
            {
                new TicketFieldDto { Id = 1, Name = "Phone", IsActive = true },
                new TicketFieldDto { Id = 2, Name = "Serial", IsActive = false }
            };

            _mockState.Setup(s => s.Value).Returns(new TicketFieldState(false, fields, string.Empty, null, new(), new()));

            var cut = Render<TicketFieldsSettings>();

            cut.Markup.Should().Contain("مدیریت فیلدهای پویای تیکت");
            cut.Markup.Should().Contain("کل فیلدهای تیکت");
            cut.Markup.Should().Contain("فیلدهای فعال");
            cut.Markup.Should().Contain("فیلدهای غیرفعال");
        }

        [Fact]
        public void Render_TicketFields_InGrid()
        {
            var fields = new List<TicketFieldDto>
            {
                new TicketFieldDto { Id = 1, Name = "CustomerNationalCode", IsActive = true, SortOrder = 1 }
            };

            _mockState.Setup(s => s.Value).Returns(new TicketFieldState(false, fields, string.Empty, null, new(), new()));

            var cut = Render<TicketFieldsSettings>();

            cut.FindAll("td").Should().Contain(td => td.InnerHtml.Contains("CustomerNationalCode"));
        }

        [Fact]
        public void SubmittingForm_DispatchesSaveTicketFieldAction()
        {
            var cut = Render<TicketFieldsSettings>();

            // Enter Name and select FieldType via SlideSelect
            cut.Find("input[placeholder='مثال: شماره موبایل']").Change("DeviceIMEI");

            var slideSelect = cut.FindComponent<SlideSelect<FieldTypeDto, int>>();
            slideSelect.InvokeAsync(() => slideSelect.Instance.ValueChanged.InvokeAsync(1));

            var form = cut.Find("form");
            form.Submit();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SaveTicketFieldAction>(a => a.TicketField.Name == "DeviceIMEI" && a.IsEditing == false)), Times.Once);
        }

        [Fact]
        public void EditTicketField_PopulatesForm_And_DispatchesSaveActionWithIsEditingTrue()
        {
            var field = new TicketFieldDto { Id = 1, Name = "OldField", FieldTypeId = 1, IsActive = true, SortOrder = 1 };
            _mockState.Setup(s => s.Value).Returns(new TicketFieldState(false, new List<TicketFieldDto> { field }, string.Empty, null, new(), new()));

            var cut = Render<TicketFieldsSettings>();

            var editBtn = cut.Find("button[title='ویرایش']");
            editBtn.Click();

            var saveBtn = cut.Find("button[type='submit']");
            saveBtn.TextContent.Should().Contain("ذخیره تغییرات");

            cut.Find("input[placeholder='مثال: شماره موبایل']").Change("UpdatedFieldName");

            var form = cut.Find("form");
            form.Submit();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SaveTicketFieldAction>(a => a.TicketField.Name == "UpdatedFieldName" && a.IsEditing == true)), Times.Once);
        }

        [Fact]
        public void DeleteTicketField_OpensModal_And_DispatchesDeleteActionOnConfirm()
        {
            var field = new TicketFieldDto { Id = 7, Name = "ToDeleteField", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new TicketFieldState(false, new List<TicketFieldDto> { field }, string.Empty, null, new(), new()));

            var cut = Render<TicketFieldsSettings>();

            var deleteBtn = cut.Find("button[title='حذف']");
            deleteBtn.Click();

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            confirmModal.Find("button.bg-rose-600").Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<DeleteTicketFieldAction>(a => a.Id == 7)), Times.Once);
        }

        [Fact]
        public void DeleteTicketField_CancelModal_ClosesWithoutDispatchingDelete()
        {
            var field = new TicketFieldDto { Id = 7, Name = "ToDeleteField", IsActive = true };
            _mockState.Setup(s => s.Value).Returns(new TicketFieldState(false, new List<TicketFieldDto> { field }, string.Empty, null, new(), new()));

            var cut = Render<TicketFieldsSettings>();

            var deleteBtn = cut.Find("button[title='حذف']");
            deleteBtn.Click();

            var confirmModal = cut.FindComponent<ConfirmDeleteModal>();
            confirmModal.Instance.IsOpen.Should().BeTrue();

            confirmModal.InvokeAsync(() => confirmModal.Instance.OnCancel.InvokeAsync());

            _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<DeleteTicketFieldAction>()), Times.Never);
        }

        [Fact]
        public void HandleSearch_DispatchesSetTicketFieldSearchAction()
        {
            var cut = Render<TicketFieldsSettings>();

            var searchBox = cut.FindComponent<SearchBox>();
            searchBox.InvokeAsync(() => searchBox.Instance.OnSearchChanged.InvokeAsync("NationalCode"));

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetTicketFieldSearchAction>(a => a.Term == "NationalCode")), Times.Once);
        }

        [Fact]
        public void FilterByStatus_DispatchesSetTicketFieldFilterStatusAction()
        {
            var cut = Render<TicketFieldsSettings>();

            var activeFilterBtn = cut.Find("button:contains('فعال')");
            activeFilterBtn.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SetTicketFieldFilterStatusAction>(a => a.Status == true)), Times.Once);
        }
    }
}
