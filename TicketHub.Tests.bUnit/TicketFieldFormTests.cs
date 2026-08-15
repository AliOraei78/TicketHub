using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Application.Validations;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Web.Components.Pages.Admin.Settings.TicketFields;
using Xunit;
using System.Collections.Generic;

namespace TicketHub.Tests.bUnit
{
    public class TicketFieldFormTests : BUnitComponentTestBase
    {
        public TicketFieldFormTests()
        {
            // Register Validator so FluentValidationValidator can resolve it
            var mockRepo = new Mock<IRepository<TicketField>>();
            mockRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TicketField, object?>>[]>()))
                    .ReturnsAsync(new List<TicketField>());

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(sp => sp.GetService(typeof(IRepository<TicketField>))).Returns(mockRepo.Object);

            var mockServiceScope = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScope>();
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

            var mockScopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
            mockScopeFactory.Setup(s => s.CreateScope()).Returns(mockServiceScope.Object);

            Services.AddScoped<IValidator<TicketFieldDto>>(sp => new TicketFieldDtoValidator(mockScopeFactory.Object));
        }

        [Fact]
        public void Render_Form_Successfully()
        {
            var cut = Render<TicketFieldForm>();
            
            // Verify base elements rendered
            cut.Find("input[placeholder='مثال: شماره موبایل']").Should().NotBeNull();
            cut.Find("input[type='number']").Should().NotBeNull(); // Sort order
        }

        [Fact]
        public void Submit_EmptyForm_ShowsValidationErrors()
        {
            var cut = Render<TicketFieldForm>();
            
            // Try to submit
            cut.Find("form").Submit();

            // Verify validation messages render
            var validationMessages = cut.FindAll(".validation-message, .text-rose-500");
            validationMessages.Should().NotBeEmpty();
            validationMessages.Should().Contain(m => m.TextContent.Contains("نام فیلد الزامی است"));
        }

        [Fact]
        public void FieldType_Dropdown_ShowsOptionsInput()
        {
            var model = new TicketFieldDto { FieldTypeId = (int)FieldTypeEnum.Dropdown };
            var cut = Render<TicketFieldForm>(parameters => parameters
                .Add(p => p.Model, model)
            );

            // Using standard placeholder text to find the options input
            var optionsInput = cut.Find("input[placeholder='گزینه ۱، گزینه ۲، گزینه ۳']");
            optionsInput.Should().NotBeNull();
        }

        [Fact]
        public void ValidSubmit_TriggersEventCallback()
        {
            bool submitted = false;
            var model = new TicketFieldDto 
            { 
                Name = "Test Field", 
                FieldTypeId = (int)FieldTypeEnum.Text,
                SortOrder = 1
            };

            var cut = Render<TicketFieldForm>(parameters => parameters
                .Add(p => p.Model, model)
                .Add(p => p.OnValidSubmit, () => { submitted = true; })
            );

            // Type values (not strictly necessary since we passed the model, but good for E2E-like Component testing)
            cut.Find("form").Submit();

            // Verify callback triggered
            submitted.Should().BeTrue();
        }
    }
}
