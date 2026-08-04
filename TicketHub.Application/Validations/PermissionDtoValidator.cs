using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class PermissionDtoValidator : AbstractValidator<PermissionDto>
{
    public PermissionDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("عنوان دسترسی الزامی است.")
            .MaximumLength(150).WithMessage("عنوان دسترسی نمی‌تواند بیشتر از 150 کاراکتر باشد.");

        RuleFor(x => x.ResourceKey)
            .NotEmpty().WithMessage("کلید منبع (ResourceKey) الزامی است.")
            .MaximumLength(100).WithMessage("کلید منبع نمی‌تواند بیشتر از 100 کاراکتر باشد.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("نوع دسترسی نامعتبر است.");
    }
}