using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TicketHub.Application.Validations;

public class ValidEmailAttribute : ValidationAttribute
{
    // الگوی استاندارد برای بررسی صحت ایمیل
    public const string Pattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";

    public ValidEmailAttribute()
    {
        ErrorMessage = "فرمت ایمیل وارد شده معتبر نیست.";
    }

    public override bool IsValid(object? value)
    {
        if (string.IsNullOrWhiteSpace(value?.ToString())) return true;
        return Regex.IsMatch(value.ToString()!, Pattern);
    }
}