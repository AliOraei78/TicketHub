using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class TransitionFieldDtoValidator : AbstractValidator<TransitionFieldDto>
{
    public TransitionFieldDtoValidator()
    {
        RuleFor(x => x.FieldName)
            .NotEmpty().WithMessage("نام فیلد الزامی است.");

        RuleFor(x => x.FieldTypeId)
            .NotEmpty().WithMessage("نوع فیلد الزامی است.")
            .GreaterThan(0).WithMessage("نوع فیلد نامعتبر است.");

        RuleFor(x => x.SortOrder)
            .NotNull().WithMessage("ترتیب نمایش الزامی است.")
            .GreaterThanOrEqualTo(0).WithMessage("ترتیب نمایش نباید کوچکتر از صفر باشد.");
    }
}