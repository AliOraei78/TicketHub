using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class PriorityDtoValidator : AbstractValidator<PriorityDto>
{
    public PriorityDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام اولویت الزامی است.")
            .MaximumLength(100).WithMessage("نام اولویت نمی‌تواند بیشتر از 100 کاراکتر باشد.");

        RuleFor(x => x.ColorCode)
            .NotEmpty().WithMessage("کد رنگ الزامی است.")
            .MaximumLength(20).WithMessage("فرمت کد رنگ نامعتبر است.");

        RuleFor(x => x.Level)
            .NotEmpty().WithMessage("تعیین سطح الزامی است.")
            .GreaterThan(0).WithMessage("سطح باید از صفر بزرگتر باشد.")
            .InclusiveBetween(0, 99).WithMessage("فرمت کد رنگ نامعتبر است.");
    }
}