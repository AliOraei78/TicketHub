using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class WorkflowDtoValidator : AbstractValidator<WorkflowDto>
{
    public WorkflowDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام جریان کاری الزامی است.")
            .MaximumLength(150).WithMessage("نام جریان کاری نمی‌تواند بیشتر از 150 کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("توضیحات نمی‌تواند بیشتر از 500 کاراکتر باشد.");

        RuleForEach(x => x.Transitions)
            .SetValidator(new TransitionDtoValidator());
    }
}