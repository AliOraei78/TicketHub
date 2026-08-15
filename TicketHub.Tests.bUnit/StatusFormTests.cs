using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.Statuses;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class StatusFormTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_CreateMode_ShowsCreateTitleAndSubmitButton()
        {
            var model = new StatusDto();

            var cut = Render<StatusForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
            );

            cut.Markup.Should().Contain("ایجاد وضعیت جدید");
            cut.Markup.Should().Contain("ثبت وضعیت");
            cut.Find("input[placeholder='مثال: In Progress']").Should().NotBeNull();
        }

        [Fact]
        public void Render_EditMode_ShowsEditTitleSaveButtonAndCancelButton()
        {
            var model = new StatusDto { Id = 3, Name = "Resolved", ColorCode = "#10B981", NeedApproval = true, IsActive = true };

            var cut = Render<StatusForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
            );

            cut.Markup.Should().Contain("ویرایش وضعیت");
            cut.Markup.Should().Contain("ذخیره تغییرات");
            cut.Markup.Should().Contain("انصراف");
        }

        [Fact]
        public void CancelButton_Click_InvokesOnCancelCallback()
        {
            var model = new StatusDto { Id = 3, Name = "Resolved", ColorCode = "#10B981", NeedApproval = true, IsActive = true };
            bool cancelInvoked = false;

            var cut = Render<StatusForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
                .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true))
            );

            var cancelBtn = cut.Find("button:contains('انصراف')");
            cancelBtn.Click();

            cancelInvoked.Should().BeTrue();
        }

        [Fact]
        public void ValidSubmit_InvokesOnSubmitCallback()
        {
            var model = new StatusDto { Name = "UnderReview", ColorCode = "#3B82F6", IsActive = true };
            bool submitted = false;

            var cut = Render<StatusForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
                .Add(p => p.OnSubmit, EventCallback.Factory.Create(this, () => submitted = true))
            );

            cut.Find("form").Submit();

            submitted.Should().BeTrue();
        }
    }
}
