using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class StrongPasswordAttribute : ValidationAttribute
{
    public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

    public StrongPasswordAttribute()
    {
        ErrorMessage = "رمز عبور باید حداقل ۸ کاراکتر شامل حروف بزرگ و کوچک انگلیسی، عدد و کاراکتر ویژه (مانند !@#$) باشد.";
    }

    public override bool IsValid(object? value)
    {
        if (string.IsNullOrWhiteSpace(value?.ToString())) return true;
        return Regex.IsMatch(value.ToString()!, Pattern);
    }
}