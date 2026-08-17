using Microsoft.AspNetCore.Components;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Interfaces;
using Microsoft.AspNetCore.Components.Forms;

namespace TicketHub.Web.Components.Pages.Auth;

public partial class ConfirmEmail : ComponentBase
{
    [Inject] protected IUserService UserService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IEmailService EmailService { get; set; } = default!;
    [Inject] protected ILogger<ConfirmEmail> Logger { get; set; } = default!;
    [Inject] protected IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [CascadingParameter] public HttpContext? HttpContext { get; set; }

    public string? Email { get; set; }

#pragma warning disable BL0008
    [SupplyParameterFromForm(FormName = "verifyForm")]
    protected VerifyViewModel verifyModel { get; set; } = new();
#pragma warning restore BL0008

    [SupplyParameterFromForm(FormName = "verifyForm", Name = "Action")]
    public string? Action { get; set; }

    protected int redirectCountdown = 5;
    protected bool isProcessing = false;
    protected bool isSuccess = false;
    protected string errorMessage = string.Empty;
    protected string successMessage = string.Empty;
    protected int _remainingSeconds = 0;

    protected override async Task OnInitializedAsync()
    {
        verifyModel ??= new();
        var httpContext = HttpContext ?? HttpContextAccessor.HttpContext;
        Email = httpContext?.Request?.Cookies["TempEmail"];

        if (string.IsNullOrEmpty(Email))
        {
            Navigation.NavigateTo("/login", forceLoad: true);
            return;
        }

        var user = await UserService.GetByEmailAsync(Email);
        if (user == null || user.IsConfirmed)
        {
            Navigation.NavigateTo("/login", forceLoad: true);
            return;
        }

        // محاسبه دقیق زمان باقی‌مانده انقضای توکن بر اساس دیتابیس
        if (user.TokenExpiration.HasValue && user.TokenExpiration.Value > DateTime.UtcNow)
        {
            var remaining = (int)Math.Ceiling((user.TokenExpiration.Value - DateTime.UtcNow).TotalSeconds);
            _remainingSeconds = remaining;
        }
        else
        {
            _remainingSeconds = 0;
        }
    }

    protected async Task VerifyCode()
    {
        isProcessing = true;
        errorMessage = string.Empty;
        successMessage = string.Empty;

        try
        {
            var user = await UserService.GetByEmailAsync(Email!);

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
                errorMessage = "کد تایید منقضی شده است. لطفاً بر روی ارسال مجدد کد کلیک کنید.";
                isProcessing = false;
                _remainingSeconds = 0;
                return;
            }

            bool isConfirmed = await UserService.ConfirmUserAsync(user.Id, verifyModel.Code);
            if (!isConfirmed)
            {
                Logger.LogWarning("کد تایید نامعتبر برای ایمیل {Email} وارد شد.", Email);
                errorMessage = "کد وارد شده نامعتبر یا اشتباه است.";
                isProcessing = false;
                return;
            }

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
            var user = await UserService.GetByEmailAsync(Email!);
            if (user == null || user.IsConfirmed)
            {
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            var result = await UserService.ResendConfirmationCodeAsync(Email!);
            if (result.Success)
            {
                verifyModel.Code = string.Empty;
                successMessage = result.Message ?? "کد جدید با موفقیت به ایمیل شما ارسال شد.";
                _remainingSeconds = result.RemainingSeconds;
            }
            else
            {
                errorMessage = result.Message ?? "امکان ارسال مجدد کد در حال حاضر وجود ندارد.";
                _remainingSeconds = result.RemainingSeconds;
            }
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