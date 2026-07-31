using System.ComponentModel.DataAnnotations;
using TicketHub.Application.Validations;

namespace TicketHub.Application.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "وارد کردن ایمیل الزامی است")]
    [ValidEmail]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "وارد کردن رمز عبور الزامی است")]
    public string Password { get; set; } = "";
}
