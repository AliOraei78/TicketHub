using FluentValidation;
using TicketHub.Core.Common;

namespace TicketHub.Web.Validations;

public class CanvasTransitionFieldValidator : AbstractValidator<CanvasTransitionField>
{
    public CanvasTransitionFieldValidator()
    {
        RuleFor(x => x.FieldName)
            .NotEmpty().WithMessage("نام فیلد الزامی است.");

        RuleFor(x => x.FieldTypeId)
            .NotEmpty().WithMessage("نوع فیلد الزامی است.")
            .GreaterThan(0).WithMessage("انتخاب نوع فیلد الزامی است.");

        RuleFor(x => x.SortOrder)
            .NotNull().WithMessage("ترتیب نمایش الزامی است.")
            .GreaterThanOrEqualTo(0).WithMessage("ترتیب نمایش نباید کوچکتر از صفر باشد.");
    }
}

public class CanvasConnectionValidator : AbstractValidator<CanvasConnection>
{
    public CanvasConnectionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام انتقال الزامی است.");

        RuleForEach(x => x.CustomFields)
            .SetValidator(new CanvasTransitionFieldValidator());
    }
}