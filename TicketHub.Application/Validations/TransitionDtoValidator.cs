using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class TransitionDtoValidator : AbstractValidator<TransitionDto>
{
    public TransitionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام انتقال الزامی است.");

        RuleFor(x => x.SourcePort)
            .NotEmpty().WithMessage("پورت مبدا الزامی است.");

        RuleFor(x => x.TargetPort)
            .NotEmpty().WithMessage("پورت مقصد الزامی است.");

        RuleFor(x => x.FromState)
            .NotEmpty().WithMessage("وضعیت مبدا الزامی است.")
            .GreaterThan(0).WithMessage("وضعیت مبدا نامعتبر است.");

        RuleFor(x => x.ToState)
            .NotEmpty().WithMessage("وضعیت مقصد الزامی است.")
            .GreaterThan(0).WithMessage("وضعیت مقصد نامعتبر است.");

        RuleFor(x => x.FromNodeId)
            .NotEmpty().WithMessage("نود مبدا الزامی است.");

        RuleFor(x => x.ToNodeId)
            .NotEmpty().WithMessage("نود مقصد الزامی است.");

        // اتصال ولیدیتور فیلدهای داینامیک
        RuleForEach(x => x.TransitionFields)
            .SetValidator(new TransitionFieldDtoValidator());
    }
}