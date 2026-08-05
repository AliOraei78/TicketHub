using FluentValidation;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Validations;

public class UserDtoValidator : AbstractValidator<UserDto>
{
    public UserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("نام کاربر الزامی است.")
            .MaximumLength(100).WithMessage("نام کاربر نمی‌تواند بیشتر از 100 کاراکتر باشد.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("ایمیل الزامی است.")
            .Matches(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$").WithMessage("فرمت ایمیل وارد شده معتبر نیست.");

        // --- تغییرات مربوط به شماره تماس ---
        RuleFor(x => x.PhoneNumber)
            .Cascade(CascadeMode.Stop) // در صورت خالی بودن، بقیه شرط‌ها را چک نمی‌کند
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("شماره تماس الزامی است.")
            .Matches(@"^09\d{9}$").WithMessage("شماره تماس باید ۱۱ رقم باشد و با 09 شروع شود.");
    }
}