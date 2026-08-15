using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Web.Components.Pages.Admin.Settings.Permissions;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class PermissionFormTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_CreateMode_ShowsCreateTitleAndSubmitButton()
        {
            var model = new PermissionDto();

            var cut = Render<PermissionForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
                .Add(p => p.AvailableRoles, new List<RoleDto>())
            );

            cut.Markup.Should().Contain("ایجاد دسترسی جدید");
            cut.Markup.Should().Contain("ثبت دسترسی");
            cut.Find("input[placeholder='مثال: مدیریت کاربران']").Should().NotBeNull();
            cut.Find("input[placeholder='مثال: Users.Manage']").Should().NotBeNull();
        }

        [Fact]
        public void Render_EditMode_ShowsEditTitleSaveButtonAndCancelButton()
        {
            var model = new PermissionDto { Id = 1, Title = "EditUser", ResourceKey = "users.edit", Type = PermissionType.Full, IsActive = true };

            var cut = Render<PermissionForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
                .Add(p => p.AvailableRoles, new List<RoleDto>())
            );

            cut.Markup.Should().Contain("ویرایش دسترسی");
            cut.Markup.Should().Contain("ذخیره تغییرات");
            cut.Markup.Should().Contain("انصراف");
        }

        [Fact]
        public void CancelButton_Click_InvokesOnCancelCallback()
        {
            var model = new PermissionDto { Id = 1, Title = "EditUser", ResourceKey = "users.edit", Type = PermissionType.Full, IsActive = true };
            bool cancelInvoked = false;

            var cut = Render<PermissionForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
                .Add(p => p.AvailableRoles, new List<RoleDto>())
                .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true))
            );

            var cancelBtn = cut.Find("button:contains('انصراف')");
            cancelBtn.Click();

            cancelInvoked.Should().BeTrue();
        }

        [Fact]
        public void ValidSubmit_InvokesOnValidSubmitCallback()
        {
            var model = new PermissionDto { Title = "ManageProjects", ResourceKey = "/projects", Type = PermissionType.Full, IsActive = true };
            bool submitted = false;

            var cut = Render<PermissionForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
                .Add(p => p.AvailableRoles, new List<RoleDto>())
                .Add(p => p.OnValidSubmit, EventCallback.Factory.Create(this, () => submitted = true))
            );

            cut.Find("form").Submit();

            submitted.Should().BeTrue();
        }
    }
}
