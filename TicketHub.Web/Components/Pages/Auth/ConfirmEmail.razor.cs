using Microsoft.AspNetCore.Components;
using TicketHub.Application.Models;
using TicketHub.Core.Interfaces;
using Microsoft.AspNetCore.Components.Forms;

namespace TicketHub.Web.Components.Pages.Auth;

public partial class ConfirmEmail : ComponentBase
{
    [Inject] protected IUserRepository UserRepository { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IEmailService EmailService { get; set; } = default!;
    [Inject] protected ILogger<ConfirmEmail> Logger { get; set; } = default!;

    [CascadingParameter] public HttpContext? HttpContext { get; set; }

    public string? Email { get; set; }

    [SupplyParameterFromForm(FormName = "verifyForm")]
    protected VerifyViewModel verifyModel { get; set; } = new();

    [SupplyParameterFromForm(FormName = "verifyForm", Name = "Action")]
    public string? Action { get; set; }

    protected int redirectCountdown = 5;
    protected bool isProcessing = false;
    protected bool isSuccess = false;
    protected string errorMessage = string.Empty;
    protected string successMessage = string.Empty;
    protected int _remainingSeconds = 120;

    protected override async Task OnInitializedAsync()
    {
        Email = HttpContext?.Request.Cookies["TempEmail"];

        if (string.IsNullOrEmpty(Email))
        {
            Navigation.NavigateTo("/login", forceLoad: true);
            return;
        }

        var user = await UserRepository.GetByEmailAsync(Email);
        if (user == null || user.IsConfirmed)
        {
            Navigation.NavigateTo("/login", forceLoad: true);
            return;
        }

        // جلوگیری از اجرای منطق لود اولیه در زمان سابمیت فرم
        if (HttpContext?.Request.Method == "POST")
        {
            if (user.TokenExpiration.HasValue && user.TokenExpiration.Value > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((user.TokenExpiration.Value - DateTime.UtcNow).TotalSeconds);
                StartTimer(remaining);
            }
            else
            {
                StartTimer(0);
            }
            return;
        }

        if (!user.TokenExpiration.HasValue || user.TokenExpiration.Value <= DateTime.UtcNow)
        {
            await ResendCode();
        }
        else
        {
            var remaining = (int)Math.Ceiling((user.TokenExpiration.Value - DateTime.UtcNow).TotalSeconds);
            StartTimer(remaining);
        }
    }

    private void StartTimer(int seconds = 120)
    {
        _remainingSeconds = seconds;
    }

    protected async Task VerifyCode()
    {
        isProcessing = true;
        errorMessage = string.Empty;
        successMessage = string.Empty;

        try
        {
            var user = await UserRepository.GetByEmailAsync(Email!);

            if (user == null)
            {
                errorMessage = "کاربری با این ایمیل یافت نشد.";
                isProcessing = false;
                return;
            }

            if (user.IsConfirmed)
            {
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            if (user.TokenExpiration.HasValue && user.TokenExpiration.Value < DateTime.UtcNow)
            {
                errorMessage = "کد تایید منقضی شده است. لطفا مجددا درخواست کنید.";
                isProcessing = false;
                return;
            }

            if (string.IsNullOrEmpty(user.ConfirmationToken) ||
                !BCrypt.Net.BCrypt.Verify(verifyModel.Code, user.ConfirmationToken))
            {
                Logger.LogWarning("کد تایید نامعتبر برای ایمیل {Email} وارد شد.", Email);
                errorMessage = "کد وارد شده نامعتبر است.";
                isProcessing = false;
                return;
            }

            user.IsConfirmed = true;
            user.ConfirmationToken = null;
            user.TokenExpiration = null;
            await UserRepository.SaveChangesAsync();

            Logger.LogInformation("حساب کاربری {Email} با موفقیت تایید و فعال شد.", Email);

            HttpContext?.Response.Cookies.Delete("TempEmail");

            isSuccess = true;
            isProcessing = false;
            StateHasChanged();

            while (redirectCountdown > 0)
            {
                await Task.Delay(1000);
                redirectCountdown--;
                StateHasChanged();
            }
            Navigation.NavigateTo("/login", forceLoad: true);
        }
        catch (NavigationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "خطا در اعتبارسنجی کد تایید برای ایمیل {Email}", Email);
            errorMessage = "در پردازش اطلاعات مشکلی رخ داد. لطفاً مجدداً تلاش کنید.";
            isProcessing = false;
        }
    }

    protected async Task ResendCode()
    {
        isProcessing = true;
        errorMessage = string.Empty;
        successMessage = string.Empty;

        try
        {
            var user = await UserRepository.GetByEmailAsync(Email!);
            if (user == null || user.IsConfirmed)
            {
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            string rawCode = new Random().Next(100000, 999999).ToString();
            string hashedCode = BCrypt.Net.BCrypt.HashPassword(rawCode);

            user.ConfirmationToken = hashedCode;
            user.TokenExpiration = DateTime.UtcNow.AddMinutes(2);
            await UserRepository.SaveChangesAsync();

            string emailBody = $@"
                <div style='font-family: Tahoma, Arial, sans-serif; direction: rtl; text-align: right;'>
                    <h2>کد تایید جدید</h2>
                    <p>کد تایید حساب کاربری شما:</p>
                    <h1 style='letter-spacing: 5px; color: #2563eb;'>{rawCode}</h1>
                    <p style='margin-top: 20px; font-size: 12px; color: #666;'>این کد تا ۲ دقیقه معتبر است.</p>
                </div>";

            await EmailService.SendEmailAsync(user.Email, "کد تایید جدید تیکت‌هاب", emailBody);
            Logger.LogInformation("کد تایید جدید برای ایمیل {Email} ارسال شد.", Email);

            verifyModel.Code = string.Empty;
            successMessage = "کد جدید با موفقیت به ایمیل شما ارسال شد.";

            // شروع دقیق از 120 ثانیه
            StartTimer(120);
        }
        catch (NavigationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "خطا در ارسال مجدد کد تایید برای ایمیل {Email}", Email);
            errorMessage = "در ارسال مجدد کد مشکلی رخ داد.";
        }
        finally
        {
            isProcessing = false;
        }
    }

    protected async Task HandleFormSubmit(EditContext context)
    {
        if (Action == "Resend")
        {
            await ResendCode();
        }
        else
        {
            if (context.Validate())
            {
                await VerifyCode();
            }
        }
    }
}