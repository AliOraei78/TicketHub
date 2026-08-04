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

        // استفاده از الگوی Regex سفارشی شما برای ایمیل
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("ایمیل الزامی است.")
            .Matches(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$").WithMessage("فرمت ایمیل وارد شده معتبر نیست.");

        // استفاده از الگوی Regex سفارشی شما برای شماره موبایل (در صورت وارد شدن مقدار)
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^09\d{9}$").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("شماره تماس باید ۱۱ رقم باشد و با 09 شروع شود.");
    }
}