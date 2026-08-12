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

        RuleFor(x => x.WorkflowStatuses)
            .Must(statuses => statuses.Count(s => s.IsInitial) == 1)
            .WithMessage("دقیقاً یک وضعیت باید به عنوان وضعیت اولیه جریان کاری انتخاب شود.")
            .Must(statuses => statuses == null || !statuses.Any(s => s.IsInitial && s.IsFinal))
            .WithMessage("یک وضعیت نمی‌تواند همزمان به عنوان وضعیت اولیه و نهایی انتخاب شود.");

        RuleFor(x => x.Transitions)
            .Must(transitions =>
            {
                if (transitions == null || !transitions.Any()) return true;
                var automatedFromNodes = transitions
                    .Where(t => t.IsAutomated == 1 && t.IsActive)
                    .GroupBy(t => t.FromNodeId != Guid.Empty ? (object)t.FromNodeId : t.FromState);
                return automatedFromNodes.All(g => g.Count() <= 1);
            })
            .WithMessage("به ازای هر وضعیت در جریان کاری، حداکثر یک انتقال خودکار خروجی مجاز است.");

        RuleForEach(x => x.Transitions)
            .SetValidator(new TransitionDtoValidator());
    }
}