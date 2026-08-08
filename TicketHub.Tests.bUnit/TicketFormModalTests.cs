using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TicketHub.Application.DTOs;
using TicketHub.Application.Validations;
using TicketHub.Web.Components.Pages.Main.Tickets;
using Xunit;
using System.Collections.Generic;

namespace TicketHub.Tests.bUnit
{
    public class TicketFormModalTests : BUnitComponentTestBase
    {
        public TicketFormModalTests()
        {
            // Register Validator so FluentValidationValidator can resolve it
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

            // Verify base elements rendered
            Assert.NotNull(cut.Find("input[placeholder='یک عنوان کوتاه بنویسید']"));
            Assert.NotNull(cut.Find("textarea[placeholder='جزئیات مشکل یا درخواست خود را بنویسید...']"));
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

            // Try to submit
            cut.Find("form").Submit();

            // Verify validation messages render
            var validationMessages = cut.FindAll(".validation-message, .text-red-500");
            Assert.NotEmpty(validationMessages);
            Assert.Contains("عنوان تیکت الزامی است.", cut.Markup);
            Assert.Contains("توضیحات تیکت الزامی است.", cut.Markup);
        }

        [Fact]
        public void Render_DynamicFields_Successfully()
        {
            var dynamicFields = new List<TicketFieldDto>
            {
                new TicketFieldDto { Id = 1, Name = "تست فیلد متنی", FieldTypeId = 1, IsRequired = true },
                new TicketFieldDto { Id = 2, Name = "تست فیلد کشویی", FieldTypeId = 5, Options = "گزینه 1,گزینه 2", IsRequired = false }
            };

            var cut = Render<TicketFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, new TicketDto())
                .Add(p => p.Projects, new List<ProjectDto>())
                .Add(p => p.Priorities, new List<PriorityDto>())
                .Add(p => p.Categories, new List<CategoryDto>())
                .Add(p => p.DynamicFields, dynamicFields)
            );

            // Verify dynamic fields label rendered
            // Uses text content checking since labels wrap or precede the input
            var labels = cut.FindAll("label");
            Assert.Contains(labels, l => l.TextContent.Contains("تست فیلد متنی"));
            Assert.Contains(labels, l => l.TextContent.Contains("تست فیلد کشویی"));
        }

        [Fact]
        public void ValidSubmit_TriggersEventCallback()
        {
            bool submitted = false;
            var model = new TicketDto 
            { 
                Title = "Test Title",
                Description = "Test Description",
                ProjectId = 1,
                PriorityId = 1,
                CategoryId = 1,
                StatusId = 1,
                UserId = 1
            };

            var cut = Render<TicketFormModal>(parameters => parameters
                .Add(p => p.IsOpen, true)
                .Add(p => p.Model, model)
                .Add(p => p.Projects, new List<ProjectDto>())
                .Add(p => p.Priorities, new List<PriorityDto>())
                .Add(p => p.Categories, new List<CategoryDto>())
                .Add(p => p.DynamicFields, new List<TicketFieldDto>())
                .Add(p => p.OnSubmit, () => { submitted = true; })
            );

            cut.Find("form").Submit();

            // Verify callback triggered
            Assert.True(submitted);
        }
    }
}
