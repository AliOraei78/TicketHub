using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TicketHub.Application.Validations;

public class ValidPhoneNumberAttribute : ValidationAttribute
{
    public const string Pattern = @"^09\d{9}$";

    public ValidPhoneNumberAttribute()
    {
        ErrorMessage = "شماره تماس باید ۱۱ رقم باشد و با 09 شروع شود.";
    }

    public override bool IsValid(object? value)
    {
        if (string.IsNullOrWhiteSpace(value?.ToString())) return true;
        return Regex.IsMatch(value.ToString()!, Pattern);
    }
}