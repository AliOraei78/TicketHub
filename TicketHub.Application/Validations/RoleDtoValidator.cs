using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class RoleDtoValidator : AbstractValidator<RoleDto>
{
    public RoleDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام نقش الزامی است.")
            .MaximumLength(100).WithMessage("نام نقش نمی‌تواند بیشتر از 100 کاراکتر باشد.");
    }
}