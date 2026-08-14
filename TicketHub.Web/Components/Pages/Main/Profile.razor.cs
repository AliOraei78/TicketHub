using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Validations;

namespace TicketHub.Web.Components.Pages.Main;

public partial class Profile : ComponentBase
{
    [Inject] public IUserService UserService { get; set; } = default!;
    [Inject] public ITicketService TicketService { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    protected ProfileViewModel ProfileModel { get; set; } = new();
    protected UserDto? CurrentUserDto { get; set; }
    protected TicketTelemetrySummaryDto? UserTelemetry { get; set; }

    protected bool IsLoading { get; set; } = true;
    protected int CurrentUserId { get; set; }
    protected string? StatusMessage { get; set; }
    protected bool IsSuccess { get; set; }

    // Active Navigation Tab: 1 = Identity, 2 = Security, 3 = Cockpit HUD
    protected int ActiveTab { get; set; } = 1;

    // Password Visibility Toggles
    protected bool ShowCurrentPassword { get; set; } = false;
    protected bool ShowNewPassword { get; set; } = false;
    protected bool ShowConfirmPassword { get; set; } = false;

    // Cockpit Customizer State
    protected string CurrentAura { get; set; } = "water";
    protected bool MatrixRainEnabled { get; set; } = false;
    protected bool NebulaParallaxEnabled { get; set; } = true;
    protected bool SoundFxEnabled { get; set; } = true;

    // Password Strength Analysis
    protected bool HasMinLength => (ProfileModel.NewPassword?.Length ?? 0) >= 8;
    protected bool HasNumber => ProfileModel.NewPassword?.Any(char.IsDigit) == true;
    protected bool HasUpperLower => (ProfileModel.NewPassword?.Any(char.IsUpper) == true) && (ProfileModel.NewPassword?.Any(char.IsLower) == true);
    protected bool HasSpecialChar => ProfileModel.NewPassword?.Any(ch => !char.IsLetterOrDigit(ch)) == true;

    protected int PasswordScore
    {
        get
        {
            if (string.IsNullOrEmpty(ProfileModel.NewPassword)) return 0;
            int score = 0;
            if (HasMinLength) score++;
            if (HasNumber) score++;
            if (HasUpperLower) score++;
            if (HasSpecialChar) score++;
            return score;
        }
    }

    protected string PasswordStrengthLabel => PasswordScore switch
    {
        0 => "بدون رمز",
        1 => "بسیار ضعیف",
        2 => "متوسط",
        3 => "قوی",
        4 => "فوق‌العاده ایمن (Cyber-Shield)",
        _ => "ناشناخته"
    };

    protected string PasswordStrengthColor => PasswordScore switch
    {
        1 => "from-rose-600 to-rose-500 text-rose-400 shadow-[0_0_10px_rgba(244,63,94,0.5)]",
        2 => "from-amber-600 to-amber-400 text-amber-300 shadow-[0_0_10px_rgba(245,158,11,0.5)]",
        3 => "from-blue-600 to-cyan-400 text-cyan-300 shadow-[0_0_10px_rgba(56,189,248,0.5)]",
        4 => "from-emerald-500 via-teal-400 to-cyan-300 text-emerald-300 shadow-[0_0_15px_rgba(16,185,129,0.7)]",
        _ => "from-slate-700 to-slate-600 text-slate-500"
    };

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userClaims = authState.User;

        if (userClaims.Identity?.IsAuthenticated == true)
        {
            var userIdStr = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var id))
            {
                CurrentUserId = id;
                CurrentUserDto = await UserService.GetByIdAsync(CurrentUserId);
                if (CurrentUserDto != null)
                {
                    ProfileModel.Email = CurrentUserDto.Email;
                    ProfileModel.Name = CurrentUserDto.Name;
                    ProfileModel.PhoneNumber = CurrentUserDto.PhoneNumber ?? string.Empty;
                }

                // Load User Ticket Telemetry Statistics
                try
                {
                    UserTelemetry = await TicketService.GetTicketTelemetrySummaryAsync(userId: CurrentUserId, user: userClaims);
                }
                catch
                {
                    UserTelemetry = new TicketTelemetrySummaryDto();
                }
            }
        }
        IsLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                var aura = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "tickethub_cockpit_aura");
                if (!string.IsNullOrWhiteSpace(aura)) CurrentAura = aura;

                var matrix = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "tickethub_matrix_rain");
                MatrixRainEnabled = matrix == "true";

                var nebula = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "tickethub_ambient_nebula");
                NebulaParallaxEnabled = nebula != "false";

                var sound = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "tickethub_sound_fx");
                SoundFxEnabled = sound != "false";

                StateHasChanged();
            }
            catch { }
        }
    }

    protected void SetActiveTab(int tabIndex)
    {
        ActiveTab = tabIndex;
        StatusMessage = null;
        try
        {
            _ = JSRuntime.InvokeVoidAsync("playCyberSound", "menu");
        }
        catch { }
    }

    protected async Task SetCockpitAura(string auraName)
    {
        CurrentAura = auraName;
        try
        {
            await JSRuntime.InvokeVoidAsync("setCockpitAura", auraName);
            StatusMessage = $"پالت رنگی کاک‌پیت به {auraName.ToUpper()} تغییر یافت.";
            IsSuccess = true;
        }
        catch { }
    }

    protected async Task ToggleMatrixRainAsync()
    {
        MatrixRainEnabled = !MatrixRainEnabled;
        try
        {
            await JSRuntime.InvokeVoidAsync("toggleMatrixRain", MatrixRainEnabled);
        }
        catch { }
    }

    protected async Task ToggleNebulaAsync()
    {
        NebulaParallaxEnabled = !NebulaParallaxEnabled;
        try
        {
            await JSRuntime.InvokeVoidAsync("toggleAmbientNebula", NebulaParallaxEnabled);
        }
        catch { }
    }

    protected async Task ToggleSoundFxAsync()
    {
        SoundFxEnabled = !SoundFxEnabled;
        try
        {
            await JSRuntime.InvokeVoidAsync("toggleSoundFx", SoundFxEnabled);
        }
        catch { }
    }

    protected void GenerateStrongPassword()
    {
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string special = "!@#$%&*_+=";

        var random = new Random();
        var builder = new StringBuilder();

        builder.Append(lower[random.Next(lower.Length)]);
        builder.Append(upper[random.Next(upper.Length)]);
        builder.Append(digits[random.Next(digits.Length)]);
        builder.Append(special[random.Next(special.Length)]);

        string allChars = lower + upper + digits + special;
        for (int i = 4; i < 16; i++)
        {
            builder.Append(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle string
        var shuffled = new string(builder.ToString().ToCharArray().OrderBy(_ => random.Next()).ToArray());

        ProfileModel.NewPassword = shuffled;
        ProfileModel.ConfirmNewPassword = shuffled;
        ShowNewPassword = true;
        ShowConfirmPassword = true;

        try
        {
            _ = JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", shuffled);
            StatusMessage = "رمز عبور فوق‌العاده امن تولید شد و در کلیپ‌بورد کپی گردید!";
            IsSuccess = true;
        }
        catch
        {
            StatusMessage = "رمز عبور فوق‌العاده امن تولید شد.";
            IsSuccess = true;
        }
    }

    protected async Task UpdateProfile()
    {
        StatusMessage = null;

        try
        {
            var (success, errorMessage) = await UserService.UpdateProfileAsync(
                CurrentUserId,
                ProfileModel.Name,
                ProfileModel.PhoneNumber,
                ProfileModel.CurrentPassword,
                ProfileModel.NewPassword);

            if (!success)
            {
                IsSuccess = false;
                StatusMessage = errorMessage;
                return;
            }

            IsSuccess = true;
            StatusMessage = "اطلاعات حساب کاربری شما با موفقیت در پایگاه داده مرکزی بروزرسانی گردید.";

            ProfileModel.CurrentPassword = null;
            ProfileModel.NewPassword = null;
            ProfileModel.ConfirmNewPassword = null;

            // Refresh cached user data
            CurrentUserDto = await UserService.GetByIdAsync(CurrentUserId);
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = ex.Message;
        }
    }

    public class ProfileViewModel
    {
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "وارد کردن نام الزامی است.")]
        public string Name { get; set; } = "";

        [ValidPhoneNumber]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? CurrentPassword { get; set; }

        [StrongPassword]
        public string? NewPassword { get; set; }

        [Compare(nameof(NewPassword), ErrorMessage = "رمز عبور جدید و تکرار آن مطابقت ندارند.")]
        public string? ConfirmNewPassword { get; set; }
    }
}
