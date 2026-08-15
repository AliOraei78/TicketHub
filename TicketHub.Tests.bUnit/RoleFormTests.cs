using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.Roles;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class RoleFormTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_CreateMode_ShowsCreateTitleAndSubmitButton()
        {
            var model = new RoleDto();

            var cut = Render<RoleForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
            );

            cut.Markup.Should().Contain("ایجاد نقش جدید");
            cut.Markup.Should().Contain("ثبت نقش");
            cut.Find("input[placeholder='مثال: کارشناس پشتیبانی فنی']").Should().NotBeNull();
        }

        [Fact]
        public void Render_EditMode_ShowsEditTitleSaveButtonAndCancelButton()
        {
            var model = new RoleDto { Id = 5, Name = "Manager", IsActive = true };

            var cut = Render<RoleForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
            );

            cut.Markup.Should().Contain("ویرایش نقش");
            cut.Markup.Should().Contain("ذخیره تغییرات");
            cut.Markup.Should().Contain("انصراف");
        }

        [Fact]
        public void CancelButton_Click_InvokesOnCancelCallback()
        {
            var model = new RoleDto { Id = 5, Name = "Manager", IsActive = true };
            bool cancelInvoked = false;

            var cut = Render<RoleForm>(parameters => parameters
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
            var model = new RoleDto { Name = "TechLead", IsActive = true };
            bool submitted = false;

            var cut = Render<RoleForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
                .Add(p => p.OnValidSubmit, EventCallback.Factory.Create(this, () => submitted = true))
            );

            cut.Find("form").Submit();

            submitted.Should().BeTrue();
        }
    }
}
