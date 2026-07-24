using System;
using System.Threading.Tasks;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Application.Interfaces;

namespace TicketHub.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepository;

        public UserService(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task RegisterUserAsync(User user, string plainPassword)
        {
            // لاجیک هش کردن رمز عبور (باید پیاده‌سازی شود)
            user.Password = plainPassword;

            // تولید توکن تایید و زمان انقضا
            user.ConfirmationToken = Guid.NewGuid().ToString();
            user.TokenExpiration = DateTime.UtcNow.AddMinutes(15);

            await _userRepository.AddAsync(user);
        }

        public async Task<bool> ConfirmUserAsync(int userId, string token)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null || user.ConfirmationToken != token || user.TokenExpiration < DateTime.UtcNow)
                return false;

            user.IsConfirmed = true;
            user.ConfirmationToken = null;
            user.TokenExpiration = null;

            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
}