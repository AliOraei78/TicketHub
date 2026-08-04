using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class ProjectDtoValidator : AbstractValidator<ProjectDto>
{
    public ProjectDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام پروژه الزامی است.")
            .MaximumLength(150).WithMessage("نام پروژه نمی‌تواند بیشتر از 150 کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("توضیحات پروژه نمی‌تواند بیشتر از 500 کاراکتر باشد.");
    }
}