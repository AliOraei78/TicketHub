using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.Validations;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Web.Components.Pages.Main;

public partial class Profile : ComponentBase
{
    [Inject] public AppDbContext DbContext { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    protected ProfileViewModel ProfileModel { get; set; } = new();
    protected bool IsLoading { get; set; } = true;
    protected int CurrentUserId { get; set; }
    protected string? StatusMessage { get; set; }
    protected bool IsSuccess { get; set; }

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
                var dbUser = await DbContext.Users.FindAsync(CurrentUserId);
                if (dbUser != null)
                {
                    ProfileModel.Email = dbUser.Email;
                    ProfileModel.Name = dbUser.Name;
                    ProfileModel.PhoneNumber = dbUser.PhoneNumber ?? string.Empty;
                }
            }
        }
        IsLoading = false;
    }

    protected async Task UpdateProfile()
    {
        StatusMessage = null;

        var dbUser = await DbContext.Users.FindAsync(CurrentUserId);
        if (dbUser == null) return;

        dbUser.Name = ProfileModel.Name;
        dbUser.PhoneNumber = ProfileModel.PhoneNumber;

        if (!string.IsNullOrWhiteSpace(ProfileModel.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(ProfileModel.CurrentPassword))
            {
                IsSuccess = false;
                StatusMessage = "برای تغییر رمز، وارد کردن رمز عبور فعلی الزامی است.";
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(ProfileModel.CurrentPassword, dbUser.Password))
            {
                IsSuccess = false;
                StatusMessage = "رمز عبور فعلی اشتباه است.";
                return;
            }

            dbUser.Password = BCrypt.Net.BCrypt.HashPassword(ProfileModel.NewPassword);
        }

        await DbContext.SaveChangesAsync();

        IsSuccess = true;
        StatusMessage = "پروفایل شما با موفقیت بروزرسانی شد.";

        ProfileModel.CurrentPassword = null;
        ProfileModel.NewPassword = null;
        ProfileModel.ConfirmNewPassword = null;
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
