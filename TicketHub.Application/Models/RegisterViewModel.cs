using System.ComponentModel.DataAnnotations;
using TicketHub.Application.Validations;

namespace TicketHub.Application.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "وارد کردن نام الزامی است.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "وارد کردن ایمیل الزامی است.")]
    [ValidEmail] // این خط جایگزین صفت پیش‌فرض EmailAddress می‌شود
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "وارد کردن شماره تماس الزامی است.")]
    [ValidPhoneNumber]
    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "وارد کردن رمز عبور الزامی است.")]
    [StrongPassword]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
    [Compare(nameof(Password), ErrorMessage = "رمز عبور و تکرار آن مطابقت ندارند.")]
    public string ConfirmPassword { get; set; } = "";
}
