using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Application.Validations;

public class TicketDtoValidator : AbstractValidator<TicketDto>
{
    public TicketDtoValidator(ITicketFieldService ticketFieldService)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("عنوان تیکت الزامی است.")
            .MaximumLength(200).WithMessage("عنوان تیکت نمی‌تواند بیشتر از 200 کاراکتر باشد.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("توضیحات تیکت الزامی است.");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("انتخاب پروژه الزامی است.");

        RuleFor(x => x.PriorityId)
            .GreaterThan(0).WithMessage("انتخاب اولویت الزامی است.");

        RuleFor(x => x.StatusId)
            .GreaterThan(0).WithMessage("وضعیت تیکت باید مشخص باشد.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("دسته‌بندی تیکت باید مشخص باشد.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("کاربر ثبت‌کننده تیکت باید مشخص باشد.");

        RuleFor(x => x.TicketFieldValues)
            .CustomAsync(async (fieldValues, context, cancellation) =>
            {
                var ticket = context.InstanceToValidate;
                if (!ticket.CategoryId.HasValue) return;

                var fields = await ticketFieldService.GetFieldsByCategoryIdAsync(ticket.CategoryId.Value);

                foreach (var field in fields.Where(f => f.IsRequired))
                {
                    // فرض بر این است که پراپرتی مقدار در TicketFieldValueDto با نام Value تعریف شده است
                    var value = fieldValues?.FirstOrDefault(v => v.TicketFieldId == field.Id)?.Value;

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        context.AddFailure($"DynamicField_{field.Id}", $"تکمیل فیلد «{field.Name}» الزامی است.");
                    }
                }
            });
    }
}