using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.Settings.Categories;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class CategoryFormTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_CreateMode_ShowsCreateTitleAndSubmitButton()
        {
            var model = new CategoryDto();
            var projects = new List<ProjectDto> { new ProjectDto { Id = 1, Name = "Core Engine" } };

            var cut = Render<CategoryForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
                .Add(p => p.AvailableProjects, projects)
            );

            cut.Markup.Should().Contain("ایجاد نوع تیکت جدید");
            cut.Markup.Should().Contain("ثبت نوع تیکت");
            cut.Find("input[placeholder='مثال: پشتیبانی فنی']").Should().NotBeNull();
        }

        [Fact]
        public void Render_EditMode_ShowsEditTitleSaveButtonAndCancelButton()
        {
            var model = new CategoryDto { Id = 2, Name = "Bug Report", IsActive = true };
            var projects = new List<ProjectDto>();

            var cut = Render<CategoryForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
                .Add(p => p.AvailableProjects, projects)
            );

            cut.Markup.Should().Contain("ویرایش نوع تیکت");
            cut.Markup.Should().Contain("ذخیره تغییرات");
            cut.Markup.Should().Contain("انصراف");
        }

        [Fact]
        public void CancelButton_Click_InvokesOnCancelCallback()
        {
            var model = new CategoryDto { Id = 2, Name = "Bug Report", IsActive = true };
            bool cancelInvoked = false;

            var cut = Render<CategoryForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, true)
                .Add(p => p.AvailableProjects, new List<ProjectDto>())
                .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true))
            );

            var cancelBtn = cut.Find("button:contains('انصراف')");
            cancelBtn.Click();

            cancelInvoked.Should().BeTrue();
        }

        [Fact]
        public void ValidSubmit_InvokesOnValidSubmitCallback()
        {
            var model = new CategoryDto { Name = "FeatureRequest", IsActive = true };
            bool submitted = false;

            var cut = Render<CategoryForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.IsEditing, false)
                .Add(p => p.AvailableProjects, new List<ProjectDto>())
                .Add(p => p.OnValidSubmit, EventCallback.Factory.Create(this, () => submitted = true))
            );

            cut.Find("form").Submit();

            submitted.Should().BeTrue();
        }
    }
}
