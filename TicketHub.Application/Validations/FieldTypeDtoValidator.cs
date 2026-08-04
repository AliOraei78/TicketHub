using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class FieldTypeDtoValidator : AbstractValidator<FieldTypeDto>
{
    public FieldTypeDtoValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("نوع فیلد الزامی است.")
            .MaximumLength(100).WithMessage("نوع فیلد نمی‌تواند بیشتر از 100 کاراکتر باشد.");
    }
}