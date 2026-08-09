using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Application.Validations;

public class TicketDtoValidator : AbstractValidator<TicketDto>
{
    // این متد سازنده جایگزین قبلی می‌شود
    public TicketDtoValidator()
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

        // اعتبارسنجی همگام و سریع فیلدهای داینامیک
        RuleForEach(x => x.FieldValues).Custom((fieldValue, context) =>
        {
            if (fieldValue.IsRequired)
            {
                bool hasTextValue = !string.IsNullOrWhiteSpace(fieldValue.Value);
                bool hasFiles = fieldValue.PendingUploads?.Any() == true ||
                                fieldValue.Attachments?.Any() == true;

                if (!hasTextValue && !hasFiles)
                {
                    // نام‌گذاری کلید خطا به این شکل، برای ردیف ۳ کاربرد دارد
                    context.AddFailure(fieldValue.FieldName, $"تکمیل فیلد «{fieldValue.FieldName}» الزامی است.");
                }
            }
        });
    }
}