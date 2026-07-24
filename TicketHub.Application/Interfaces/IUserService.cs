using System.Threading.Tasks;
using TicketHub.Core.Entities;

namespace TicketHub.Application.Interfaces
{
    public interface IUserService
    {
        Task RegisterUserAsync(User user, string plainPassword);
        Task<bool> ConfirmUserAsync(int userId, string token);
    }
}