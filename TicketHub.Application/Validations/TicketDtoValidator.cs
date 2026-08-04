using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class TicketDtoValidator : AbstractValidator<TicketDto>
{
    public TicketDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("عنوان تیکت الزامی است.")
            .MaximumLength(200).WithMessage("عنوان تیکت نمی‌تواند بیشتر از 200 کاراکتر باشد.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("توضیحات تیکت الزامی است.");

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("انتخاب پروژه الزامی است.");

        RuleFor(x => x.StatusId)
            .GreaterThan(0).WithMessage("وضعیت تیکت باید مشخص باشد.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("کاربر ثبت‌کننده تیکت باید مشخص باشد.");
    }
}