using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class StatusDtoValidator : AbstractValidator<StatusDto>
{
    public StatusDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام وضعیت الزامی است.")
            .MaximumLength(100).WithMessage("نام وضعیت نمی‌تواند بیشتر از 100 کاراکتر باشد.");

        RuleFor(x => x.ColorCode)
            .NotEmpty().WithMessage("کد رنگ الزامی است.")
            .MaximumLength(20).WithMessage("فرمت کد رنگ نامعتبر است.");
    }
}