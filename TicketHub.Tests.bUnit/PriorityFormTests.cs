using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.Priorities;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class PriorityFormTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_CreateMode_ShowsCreateTitleAndSubmitButton()
        {
            var model = new PriorityDto();

            var cut = Render<PriorityForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
            );

            cut.Markup.Should().Contain("ایجاد اولویت جدید");
            cut.Markup.Should().Contain("ثبت اولویت");
            cut.Find("input[placeholder='مثال: بحرانی / فوری']").Should().NotBeNull();
        }

        [Fact]
        public void Render_EditMode_ShowsEditTitleSaveButtonAndCancelButton()
        {
            var model = new PriorityDto { Id = 3, Name = "High", Level = 10, ColorCode = "#EF4444", IsActive = true };

            var cut = Render<PriorityForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
            );

            cut.Markup.Should().Contain("ویرایش اولویت");
            cut.Markup.Should().Contain("ذخیره تغییرات");
            cut.Markup.Should().Contain("انصراف");
        }

        [Fact]
        public void CancelButton_Click_InvokesOnCancelCallback()
        {
            var model = new PriorityDto { Id = 3, Name = "High", Level = 10, ColorCode = "#EF4444", IsActive = true };
            bool cancelInvoked = false;

            var cut = Render<PriorityForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
                .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true))
            );

            var cancelBtn = cut.Find("button:contains('انصراف')");
            cancelBtn.Click();

            cancelInvoked.Should().BeTrue();
        }

        [Fact]
        public void ValidSubmit_InvokesOnValidSubmitCallback()
        {
            var model = new PriorityDto { Name = "Urgent", Level = 99, ColorCode = "#DC2626", IsActive = true };
            bool submitted = false;

            var cut = Render<PriorityForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
                .Add(p => p.OnValidSubmit, EventCallback.Factory.Create(this, () => submitted = true))
            );

            cut.Find("form").Submit();

            submitted.Should().BeTrue();
        }
    }
}
