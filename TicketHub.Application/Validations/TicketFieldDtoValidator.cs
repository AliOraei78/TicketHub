using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;

namespace TicketHub.Application.Validations;

public class TicketFieldDtoValidator : AbstractValidator<TicketFieldDto>
{
    public TicketFieldDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام فیلد الزامی است.")
            .MaximumLength(150).WithMessage("نام فیلد نمی‌تواند بیشتر از 150 کاراکتر باشد.");

        RuleFor(x => x.FieldTypeId)
            .GreaterThan(0).WithMessage("انتخاب نوع فیلد الزامی است.");

        RuleFor(x => x.Placeholder)
            .MaximumLength(200).WithMessage("متن نگهدارنده (Placeholder) نمی‌تواند بیشتر از 200 کاراکتر باشد.");

        RuleFor(x => x.Options)
            .NotEmpty().WithMessage("وارد کردن گزینه‌ها برای لیست‌های کشویی الزامی است.")
            .When(x => x.FieldTypeId == (int)FieldTypeEnum.Dropdown || x.FieldTypeId == (int)FieldTypeEnum.MultipleDropdown);
    }
}