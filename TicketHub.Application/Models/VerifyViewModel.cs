using System.ComponentModel.DataAnnotations;

namespace TicketHub.Application.Models;

public class VerifyViewModel
{
    [Required(ErrorMessage = "وارد کردن کد الزامی است")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "کد باید ۶ رقم باشد")]
    public string Code { get; set; } = "";
}
