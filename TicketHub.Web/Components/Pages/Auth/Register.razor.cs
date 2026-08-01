using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace TicketHub.Web.Components.Pages.Auth;

public partial class Register : ComponentBase
{
    [Inject] protected IUserService UserService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected ProtectedSessionStorage ProtectedSessionStore { get; set; } = default!;

    [SupplyParameterFromForm]
    protected RegisterViewModel registerModel { get; set; } = new();

    protected string? errorMessage;
    protected bool showSuccessMessage = false;
    protected bool isLoading = false;

    protected async Task HandleRegister()
    {
        isLoading = true;
        StateHasChanged();
        await Task.Delay(10);

        try
        {
            var userDto = registerModel.Adapt<UserDto>();
            var result = await UserService.RegisterUserAsync(userDto, registerModel.Password);

            if (!result.Success)
            {
                errorMessage = result.ErrorMessage;
                return;
            }

            showSuccessMessage = true;
            await ProtectedSessionStore.SetAsync("TempEmail", registerModel.Email);
            Navigation.NavigateTo("/confirm-email");
        }
        catch (NavigationException)
        {
            throw;
        }
        catch (Exception)
        {
            errorMessage = "خطایی در پردازش اطلاعات رخ داد.";
        }
        finally
        {
            isLoading = false;
        }
    }
}