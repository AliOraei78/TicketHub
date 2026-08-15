using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Projects;
using TicketHub.Web.Components.Shared;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class ProjectFormTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_WhenIsOpenIsTrue_RendersModalInRootOutlet()
        {
            var cut = Render<ProjectForm>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Title, "ایجاد پروژه جدید")
                .Add(p => p.Model, new ProjectDto())
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper.Should().NotBeNull();
            outletRef.Wrapper!.Markup.Should().Contain("ایجاد پروژه جدید");
            outletRef.Wrapper.Markup.Should().Contain("[SYS // NEW_PROJECT]");
        }

        [Fact]
        public void Render_WhenIsOpenIsFalse_DoesNotRenderModal()
        {
            var cut = Render<ProjectForm>(parameters => parameters
                .Add(p => p.IsOpen, false)
                .Add(p => p.Title, "ایجاد پروژه جدید")
                .Add(p => p.Model, new ProjectDto())
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper!.Markup.Should().BeEmpty();
        }

        [Fact]
        public void Submit_EmptyForm_ShowsValidationErrors()
        {
            var cut = Render<ProjectForm>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new ProjectDto())
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper!.Find("form").Submit();

            var validationMessages = outletRef.Wrapper!.FindAll(".validation-message, .text-rose-400");
            validationMessages.Should().NotBeEmpty();
        }

        [Fact]
        public void ValidSubmit_InvokesOnSaveCallback()
        {
            bool saved = false;
            var model = new ProjectDto
            {
                Name = "پروژه تستی معتبر",
                Description = "توضیحات تست",
                IsActive = true
            };

            var cut = Render<ProjectForm>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, model)
                .Add(p => p.OnSave, () => { saved = true; })
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper!.Find("form").Submit();

            saved.Should().BeTrue();
        }

        [Fact]
        public void CancelButton_Click_InvokesOnCancelCallback()
        {
            bool cancelled = false;
            var cut = Render<ProjectForm>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new ProjectDto())
                .Add(p => p.OnCancel, () => { cancelled = true; })
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            var cancelButton = outletRef.Wrapper!.Find("button:contains('انصراف')");
            cancelButton.Click();

            cancelled.Should().BeTrue();
        }

        [Fact]
        public void EditMode_RendersEditTitleAndBadge()
        {
            var model = new ProjectDto
            {
                Id = 12,
                Name = "پروژه در حال ویرایش",
                Description = "توضیح",
                IsActive = true
            };

            var cut = Render<ProjectForm>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Title, "ویرایش پروژه")
                .Add(p => p.Model, model)
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper!.Markup.Should().Contain("ویرایش پروژه");
            outletRef.Wrapper.Markup.Should().Contain("[SYS // EDIT_PROJECT]");
            outletRef.Wrapper.Markup.Should().Contain("ذخیره تغییرات");
        }

        [Fact]
        public void WorkflowAndRoles_RenderInForm()
        {
            var workflows = new List<WorkflowDto>
            {
                new WorkflowDto { Id = 1, Name = "جریان پشتیبانی" },
                new WorkflowDto { Id = 2, Name = "جریان توسعه" }
            };

            var roles = new List<RoleDto>
            {
                new RoleDto { Id = 10, Name = "مدیر سیستم" },
                new RoleDto { Id = 20, Name = "کارشناس فنی" }
            };

            var model = new ProjectDto { Name = "پروژه تست" };

            var cut = Render<ProjectForm>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, model)
                .Add(p => p.Workflows, workflows)
                .Add(p => p.Roles, roles)
            );

            var outletRef = cut.Services.GetRequiredService<SectionOutletRef>();
            outletRef.Wrapper!.FindComponents<SlideSelect<WorkflowDto, int?>>().Should().NotBeEmpty();
            outletRef.Wrapper!.FindComponents<MultiSelectDropdown<RoleDto, int>>().Should().NotBeEmpty();
        }
    }
}
