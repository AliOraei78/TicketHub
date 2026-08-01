using DNTCaptcha.Core;
using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;

namespace TicketHub.Web.Components.Pages.Auth;

public partial class Register : ComponentBase
{
    [Inject] protected IUserService UserService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected ILogger<Register> Logger { get; set; } = default!;
    [Inject] protected IDNTCaptchaValidatorService CaptchaValidator { get; set; } = default!;

    [CascadingParameter] public HttpContext? HttpContext { get; set; }

    [SupplyParameterFromForm]
    protected RegisterViewModel registerModel { get; set; } = new();

    protected bool isCaptchaValid = false;
    protected string? errorMessage;
    protected bool showSuccessMessage = false;
    protected bool isLoading = false;

    protected async Task HandleRegister()
    {
        // --- اعتبارسنجی کپچا ---
        bool isValidCaptcha = CaptchaValidator.HasRequestValidCaptchaEntry();
        if (!isValidCaptcha)
        {
            errorMessage = "کد امنیتی نامعتبر است یا منقضی شده است.";
            isLoading = false;
            return;
        }
        // -----------------------

        isLoading = true;
        StateHasChanged();

        try
        {
            var userDto = registerModel.Adapt<UserDto>();
            var result = await UserService.RegisterUserAsync(userDto, registerModel.Password);

            if (!result.Success)
            {
                Logger.LogWarning("تلاش ناموفق برای ثبت‌نام {Email} - علت: {ErrorMessage}", registerModel.Email, result.ErrorMessage);
                errorMessage = result.ErrorMessage;
                return;
            }

            Logger.LogInformation("ثبت‌نام کاربر جدید با موفقیت انجام شد: {Email}", registerModel.Email);
            showSuccessMessage = true;

            // ذخیره ایمیل در کوکی موقت 15 دقیقه‌ای برای استفاده در صفحه تایید ایمیل
            HttpContext?.Response.Cookies.Append("TempEmail", registerModel.Email, new CookieOptions { HttpOnly = true, Expires = DateTimeOffset.UtcNow.AddMinutes(15) });

            Navigation.NavigateTo("/confirm-email");
        }
        catch (NavigationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "خطای سیستمی در فرآیند ثبت‌نام کاربر {Email}", registerModel.Email);
            showSuccessMessage = false;
            errorMessage = "خطایی در پردازش اطلاعات رخ داد. مراتب در سیستم ثبت شد.";
        }
        finally
        {
            isLoading = false;
        }
    }
}