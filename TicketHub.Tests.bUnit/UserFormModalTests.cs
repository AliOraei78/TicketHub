using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Users;
using TicketHub.Web.Components.Shared;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class UserFormModalTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_WhenIsOpenIsTrue_RendersModalInRootOutlet()
        {
            var cut = Render<UserFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new UserDto())
                .Add(p => p.AvailableRoles, new List<RoleDto>())
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper.Should().NotBeNull();
            outletRef.Wrapper!.Markup.Should().Contain("ایجاد کاربر جدید");
            outletRef.Wrapper.Markup.Should().Contain("[SYS // NEW_USER]");
        }

        [Fact]
        public void Render_WhenIsOpenIsFalse_DoesNotRenderModal()
        {
            var cut = Render<UserFormModal>(parameters => parameters
                .Add(p => p.IsOpen, false)
                .Add(p => p.Model, new UserDto())
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper!.Markup.Should().BeEmpty();
        }

        [Fact]
        public void SubmitEmptyForm_ShowsValidationMessages()
        {
            var cut = Render<UserFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new UserDto())
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper!.Find("form").Submit();

            var validationMessages = outletRef.Wrapper!.FindAll(".validation-message, .text-rose-400");
            validationMessages.Should().NotBeEmpty();
        }

        [Fact]
        public void CancelButton_Click_InvokesOnCancel()
        {
            bool cancelled = false;
            var cut = Render<UserFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new UserDto())
                .Add(p => p.OnCancel, () => { cancelled = true; })
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            var cancelBtn = outletRef.Wrapper!.Find("button:contains('انصراف')");
            cancelBtn.Click();

            cancelled.Should().BeTrue();
        }

        [Fact]
        public void EditMode_RendersEditTitleAndOptionalPasswordLabel()
        {
            var existingUser = new UserDto
            {
                Id = 15,
                Name = "کاربر ویرایش",
                Email = "edit@example.com",
                PhoneNumber = "09121234567",
                IsActive = true
            };

            var cut = Render<UserFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, existingUser)
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper!.Markup.Should().Contain("ویرایش اطلاعات کاربر");
            outletRef.Wrapper.Markup.Should().Contain("[SYS // EDIT_USER]");
            outletRef.Wrapper.Markup.Should().Contain("رمز عبور جدید (اختیاری)");
        }
    }
}
