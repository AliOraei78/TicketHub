using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validators;

public class ExecuteTransitionDtoValidator : AbstractValidator<ExecuteTransitionDto>
{
    public ExecuteTransitionDtoValidator()
    {
        RuleFor(x => x.TicketId)
            .GreaterThan(0).WithMessage("شناسه تیکت نامعتبر است.");

        RuleFor(x => x.TransitionId)
            .GreaterThan(0).WithMessage("شناسه انتقال نامعتبر است.");

        RuleForEach(x => x.FieldValues).ChildRules(field =>
        {
            field.RuleFor(f => f.Value)
                 .NotEmpty()
                 .When(f => f.IsRequired)
                 .WithMessage(f => $"فیلد '{f.FieldName}' الزامی است.");
        });
    }
}
