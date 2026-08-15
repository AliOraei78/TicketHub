using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.TicketFields;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class TicketFieldFormTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_CreateMode_ShowsCreateTitleAndSubmitButton()
        {
            var model = new TicketFieldDto();

            var cut = Render<TicketFieldForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
                .Add(p => p.AvailableFieldTypes, new List<FieldTypeDto>())
                .Add(p => p.AvailableCategories, new List<CategoryDto>())
            );

            cut.Markup.Should().Contain("ایجاد فیلد جدید");
            cut.Markup.Should().Contain("ثبت فیلد");
            cut.Find("input[placeholder='مثال: شماره موبایل']").Should().NotBeNull();
        }

        [Fact]
        public void Render_EditMode_ShowsEditTitleSaveButtonAndCancelButton()
        {
            var model = new TicketFieldDto { Id = 1, Name = "DeviceSerial", SortOrder = 5, IsActive = true };

            var cut = Render<TicketFieldForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
                .Add(p => p.AvailableFieldTypes, new List<FieldTypeDto>())
                .Add(p => p.AvailableCategories, new List<CategoryDto>())
            );

            cut.Markup.Should().Contain("ویرایش فیلد داینامیک");
            cut.Markup.Should().Contain("ذخیره تغییرات");
            cut.Markup.Should().Contain("انصراف");
        }

        [Fact]
        public void CancelButton_Click_InvokesOnCancelCallback()
        {
            var model = new TicketFieldDto { Id = 1, Name = "DeviceSerial", SortOrder = 5, IsActive = true };
            bool cancelInvoked = false;

            var cut = Render<TicketFieldForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
                .Add(p => p.AvailableFieldTypes, new List<FieldTypeDto>())
                .Add(p => p.AvailableCategories, new List<CategoryDto>())
                .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true))
            );

            var cancelBtn = cut.Find("button:contains('انصراف')");
            cancelBtn.Click();

            cancelInvoked.Should().BeTrue();
        }

        [Fact]
        public void ValidSubmit_InvokesOnValidSubmitCallback()
        {
            var model = new TicketFieldDto { Name = "CustomerCode", FieldTypeId = 1, SortOrder = 1, IsActive = true };
            bool submitted = false;

            var cut = Render<TicketFieldForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
                .Add(p => p.AvailableFieldTypes, new List<FieldTypeDto>())
                .Add(p => p.AvailableCategories, new List<CategoryDto>())
                .Add(p => p.OnValidSubmit, EventCallback.Factory.Create(this, () => submitted = true))
            );

            cut.Find("form").Submit();

            submitted.Should().BeTrue();
        }
    }
}
