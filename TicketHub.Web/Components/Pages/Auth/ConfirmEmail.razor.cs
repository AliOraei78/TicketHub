using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Timers;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;
using Timer = System.Timers.Timer;

namespace TicketHub.Web.Components.Pages.Auth;

public partial class ConfirmEmail : ComponentBase, IDisposable
{
    [Inject] protected IUserRepository UserRepository { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected IEmailService EmailService { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "email")]
    public string? Email { get; set; }

    protected VerifyViewModel verifyModel { get; set; } = new();

    protected int redirectCountdown = 5;
    protected bool isProcessing = false;
    protected bool isSuccess = false;
    protected string errorMessage = string.Empty;
    protected string successMessage = string.Empty;
    protected int _remainingSeconds = 120;

    private Timer? _timer;

    protected override void OnInitialized()
    {
        if (string.IsNullOrEmpty(Email))
        {
            Navigation.NavigateTo("/login", forceLoad: true);
            return;
        }
        StartTimer();
    }

    private void StartTimer()
    {
        _remainingSeconds = 120;
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Dispose();
        }
        _timer = new Timer(1000);
        _timer.Elapsed += CountDownTimer;
        _timer.Start();
    }

    private void CountDownTimer(Object? source, ElapsedEventArgs e)
    {
        if (_remainingSeconds > 0)
        {
            _remainingSeconds--;
        }
        else
        {
            _timer?.Stop();
        }
        InvokeAsync(StateHasChanged);
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
                errorMessage = "کد وارد شده نامعتبر است.";
                isProcessing = false;
                return;
            }

            user.IsConfirmed = true;
            user.ConfirmationToken = null;
            user.TokenExpiration = null;
            await UserRepository.SaveChangesAsync();

            _timer?.Stop();
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
        catch (Exception)
        {
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
            await UserRepository.UpdateAsync(user);

            string emailBody = $@"
                <div style='font-family: Tahoma, Arial, sans-serif; direction: rtl; text-align: right;'>
                    <h2>کد تایید جدید</h2>
                    <p>کد تایید حساب کاربری شما:</p>
                    <h1 style='letter-spacing: 5px; color: #2563eb;'>{rawCode}</h1>
                    <p style='margin-top: 20px; font-size: 12px; color: #666;'>این کد تا ۲ دقیقه معتبر است.</p>
                </div>";

            await EmailService.SendEmailAsync(user.Email, "کد تایید جدید تیکت‌هاب", emailBody);

            verifyModel.Code = string.Empty;
            successMessage = "کد جدید با موفقیت به ایمیل شما ارسال شد.";
            StartTimer();
        }
        catch (NavigationException)
        {
            throw;
        }
        catch (Exception)
        {
            errorMessage = "در ارسال مجدد کد مشکلی رخ داد.";
        }
        finally
        {
            isProcessing = false;
        }
    }

    public void Dispose()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}

public class VerifyViewModel
{
    [Required(ErrorMessage = "وارد کردن کد الزامی است")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "کد باید ۶ رقم باشد")]
    public string Code { get; set; } = "";
}