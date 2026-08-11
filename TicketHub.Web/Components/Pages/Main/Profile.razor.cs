using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Validations;

namespace TicketHub.Web.Components.Pages.Main;

public partial class Profile : ComponentBase
{
    [Inject] public IUserService UserService { get; set; } = default!;
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
                var userDto = await UserService.GetByIdAsync(CurrentUserId);
                if (userDto != null)
                {
                    ProfileModel.Email = userDto.Email;
                    ProfileModel.Name = userDto.Name;
                    ProfileModel.PhoneNumber = userDto.PhoneNumber ?? string.Empty;
                }
            }
        }
        IsLoading = false;
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
            StatusMessage = "پروفایل شما با موفقیت بروزرسانی شد.";

            ProfileModel.CurrentPassword = null;
            ProfileModel.NewPassword = null;
            ProfileModel.ConfirmNewPassword = null;
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
