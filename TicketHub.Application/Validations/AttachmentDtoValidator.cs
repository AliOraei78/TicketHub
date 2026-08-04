using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class AttachmentDtoValidator : AbstractValidator<AttachmentDto>
{
    public AttachmentDtoValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("نام فایل الزامی است.")
            .MaximumLength(255).WithMessage("نام فایل نمی‌تواند بیشتر از 255 کاراکتر باشد.");

        RuleFor(x => x.FilePath)
            .NotEmpty().WithMessage("مسیر ذخیره‌سازی فایل الزامی است.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("نوع محتوای فایل (ContentType) الزامی است.");
    }
}