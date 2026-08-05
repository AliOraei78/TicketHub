using FluentValidation;
using TicketHub.Application.Models;

namespace TicketHub.Application.Validations;

public class UserFormSubmissionResultValidator : AbstractValidator<UserFormSubmissionResult>
{
    public UserFormSubmissionResultValidator()
    {
        // اعمال اعتبارسنجی‌های UserDto روی Property مربوطه
        RuleFor(x => x.User).SetValidator(new UserDtoValidator());

        // اگر کاربر جدید است (Id == 0)، رمز عبور اجباری است
        RuleFor(x => x.Password)
            .NotEmpty().When(x => x.User.Id == 0)
            .WithMessage("رمز عبور الزامی است.");

        // اگر فیلد رمز عبور پر شده بود (چه جدید چه ویرایش)، قوانین قدرت رمز چک شود
        RuleFor(x => x.Password)
            .Matches(StrongPasswordAttribute.Pattern).When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage(new StrongPasswordAttribute().ErrorMessage);
    }
}