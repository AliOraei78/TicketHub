using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TicketHub.Application.DTOs;
using TicketHub.Application.Validations;
using TicketHub.Web.Components.Pages.Main.Tickets;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class TicketFormModalTests : BUnitComponentTestBase
    {
        public TicketFormModalTests()
        {
            Services.AddScoped<IValidator<TicketDto>, TicketDtoValidator>();
        }

        [Fact]
        public void Render_Form_Successfully()
        {
            var cut = Render<TicketFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new TicketDto())
                .Add(p => p.Projects, new List<ProjectDto>())
                .Add(p => p.Priorities, new List<PriorityDto>())
                .Add(p => p.Categories, new List<CategoryDto>())
                .Add(p => p.DynamicFields, new List<TicketFieldDto>())
            );

            Assert.Contains("ایجاد تیکت پشتیبانی جدید", ModalMarkup);
            Assert.Contains("عنوان تیکت", ModalMarkup);
            Assert.Contains("توضیحات تیکت", ModalMarkup);
        }

        [Fact]
        public void Submit_EmptyForm_ShowsValidationErrors()
        {
            var cut = Render<TicketFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new TicketDto())
                .Add(p => p.Projects, new List<ProjectDto>())
                .Add(p => p.Priorities, new List<PriorityDto>())
                .Add(p => p.Categories, new List<CategoryDto>())
                .Add(p => p.DynamicFields, new List<TicketFieldDto>())
            );

            cut.Find("form").Submit();

            Assert.Contains("عنوان تیکت الزامی است.", ModalMarkup);
            Assert.Contains("توضیحات تیکت الزامی است.", ModalMarkup);
        }

        [Fact]
        public void Render_DynamicFields_Successfully()
        {
            var dynamicFields = new List<TicketFieldDto>
            {
                new TicketFieldDto { Id = 1, Name = "تست فیلد متنی", FieldTypeId = 1, IsActive = true, IsRequired = true },
                new TicketFieldDto { Id = 2, Name = "تست فیلد کشویی", FieldTypeId = 5, Options = "گزینه 1,گزینه 2", IsActive = true, IsRequired = false }
            };

            var cut = Render<TicketFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new TicketDto())
                .Add(p => p.Projects, new List<ProjectDto>())
                .Add(p => p.Priorities, new List<PriorityDto>())
                .Add(p => p.Categories, new List<CategoryDto>())
                .Add(p => p.DynamicFields, dynamicFields)
            );

            Assert.Contains("تست فیلد متنی", ModalMarkup);
            Assert.Contains("تست فیلد کشویی", ModalMarkup);
        }

        [Fact]
        public void ValidSubmit_TriggersEventCallback()
        {
            bool submitted = false;
            var model = new TicketDto 
            { 
                Title = "تیکت تستی",
                Description = "شرح تیکت برای ارسال",
                ProjectId = 1,
                PriorityId = 1,
                CategoryId = 1,
                StatusId = 1,
                UserId = 1
            };

            var cut = Render<TicketFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, model)
                .Add(p => p.Projects, new List<ProjectDto> { new() { Id = 1, Name = "پروژه ۱" } })
                .Add(p => p.Priorities, new List<PriorityDto> { new() { Id = 1, Name = "بالا" } })
                .Add(p => p.Categories, new List<CategoryDto> { new() { Id = 1, Name = "دسته ۱", ProjectIds = new List<int> { 1 } } })
                .Add(p => p.DynamicFields, new List<TicketFieldDto>())
                .Add(p => p.OnSubmit, () => { submitted = true; })
            );

            cut.Find("form").Submit();

            Assert.True(submitted);
        }

        [Fact]
        public void DynamicField_RequiredValidation_PreventsSubmitWhenEmpty()
        {
            bool submitted = false;
            var model = new TicketDto 
            { 
                Title = "تیکت با فیلد اجباری",
                Description = "شرح تیکت",
                ProjectId = 1,
                PriorityId = 1,
                CategoryId = 1,
                StatusId = 1,
                UserId = 1
            };

            var dynamicFields = new List<TicketFieldDto>
            {
                new TicketFieldDto { Id = 10, Name = "کد رهگیری الزامی", FieldTypeId = 1, IsActive = true, IsRequired = true }
            };

            var cut = Render<TicketFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, model)
                .Add(p => p.Projects, new List<ProjectDto> { new() { Id = 1, Name = "پروژه ۱" } })
                .Add(p => p.Priorities, new List<PriorityDto> { new() { Id = 1, Name = "عادی" } })
                .Add(p => p.Categories, new List<CategoryDto> { new() { Id = 1, Name = "دسته ۱", ProjectIds = new List<int> { 1 } } })
                .Add(p => p.DynamicFields, dynamicFields)
                .Add(p => p.OnSubmit, () => { submitted = true; })
            );

            cut.Find("form").Submit();

            // Submit should NOT be called because dynamic field is required and empty
            Assert.False(submitted);
            Assert.Contains("تکمیل فیلد «کد رهگیری الزامی» الزامی است.", ModalMarkup);
        }

        [Fact]
        public void CategoryChanged_RendersProjectCategories()
        {
            var model = new TicketDto { ProjectId = 1 };
            var categories = new List<CategoryDto>
            {
                new() { Id = 101, Name = "پشتیبانی فنی", ProjectIds = new List<int> { 1 } }
            };

            var cut = Render<TicketFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, model)
                .Add(p => p.Projects, new List<ProjectDto> { new() { Id = 1, Name = "پروژه ۱" } })
                .Add(p => p.Priorities, new List<PriorityDto>())
                .Add(p => p.Categories, categories)
            );

            // Assert categories are rendered in modal
            Assert.Contains("پشتیبانی فنی", ModalMarkup);
        }

        [Fact]
        public void Cancel_Button_TriggersOnCancelCallback()
        {
            bool cancelled = false;

            var cut = Render<TicketFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new TicketDto())
                .Add(p => p.Projects, new List<ProjectDto>())
                .Add(p => p.Priorities, new List<PriorityDto>())
                .Add(p => p.Categories, new List<CategoryDto>())
                .Add(p => p.OnCancel, () => { cancelled = true; })
            );

            var cancelButton = cut.Find("button.btn-cyber-ghost");
            cancelButton.Click();

            Assert.True(cancelled);
        }
    }
}
