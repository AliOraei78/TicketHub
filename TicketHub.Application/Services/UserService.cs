using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IRepository<Role> _roleRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IEmailService emailService,
        IRepository<Role> roleRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت لیست تمامی کاربران.");

            var users = await _userRepository.GetAllAsync();
            return users.Adapt<List<UserDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست تمامی کاربران.");
            throw;
        }
    }

    public async Task<(List<UserDto> Users, int TotalCount)> GetFilteredUsersAsync(
        string searchTerm, List<int> roleIds, List<int> projectIds, bool? status, int page, int pageSize)
    {
        try
        {
            _logger.LogInformation("دریافت لیست کاربران با فیلتر. صفحه: {Page}، تعداد در صفحه: {PageSize}.", page, pageSize);

            var (users, totalCount) = await _userRepository.GetFilteredUsersAsync(
                searchTerm, roleIds, projectIds, status, page, pageSize);

            _logger.LogInformation("تعداد {TotalCount} کاربر منطبق با فیلترها یافت شد.", totalCount);
            return (users.Adapt<List<UserDto>>(), totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست فیلتر شده کاربران.");
            throw;
        }
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("جستجوی کاربر با شناسه {Id}.", id);

            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
                _logger.LogWarning("کاربر با شناسه {Id} یافت نشد.", id);

            return user?.Adapt<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت کاربر با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task CreateAsync(UserDto dto, string password, List<int> roleIds)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد کاربر جدید با ایمیل {Email}.", dto.Email);

            var user = dto.Adapt<User>();
            user.Password = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(user);
            await _userRepository.UpdateUserRolesAsync(user.Id, roleIds);

            _logger.LogInformation("کاربر جدید با شناسه {Id} با موفقیت ایجاد شد.", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد کاربر جدید با ایمیل {Email}.", dto.Email);
            throw;
        }
    }

    public async Task UpdateAsync(UserDto dto, string? password, List<int> roleIds)
    {
        try
        {
            _logger.LogInformation("ویرایش کاربر با شناسه {Id}.", dto.Id);

            var userInDb = await _userRepository.GetByIdAsync(dto.Id);
            if (userInDb == null)
            {
                _logger.LogWarning("کاربر با شناسه {Id} جهت ویرایش یافت نشد.", dto.Id);
                return;
            }

            dto.Adapt(userInDb);

            if (!string.IsNullOrWhiteSpace(password))
                userInDb.Password = BCrypt.Net.BCrypt.HashPassword(password);

            await _userRepository.UpdateAsync(userInDb);
            await _userRepository.UpdateUserRolesAsync(userInDb.Id, roleIds);

            _logger.LogInformation("کاربر با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش کاربر با شناسه {Id}.", dto.Id);
            throw;
        }
    }

    public async Task ExecuteBulkActionAsync(HashSet<int> userIds, string actionType, int? singleId = null)
    {
        try
        {
            _logger.LogInformation("اجرای عملیات گروهی {ActionType} روی کاربران.", actionType);

            switch (actionType)
            {
                case "Delete":
                    await _userRepository.BulkDeleteAsync(userIds);
                    break;
                case "SingleDelete":
                    if (singleId.HasValue)
                        await _userRepository.DeleteAsync(singleId.Value);
                    break;
                case "Activate":
                    await _userRepository.BulkUpdateStatusAsync(userIds, true);
                    break;
                case "Deactivate":
                    await _userRepository.BulkUpdateStatusAsync(userIds, false);
                    break;
            }

            _logger.LogInformation("عملیات گروهی {ActionType} با موفقیت انجام شد.", actionType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در اجرای عملیات گروهی {ActionType} روی کاربران.", actionType);
            throw;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RegisterUserAsync(UserDto dto, string plainPassword)
    {
        try
        {
            _logger.LogInformation("درخواست ثبت نام برای ایمیل {Email}.", dto.Email);

            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("ثبت نام ناموفق: ایمیل {Email} قبلاً ثبت شده است.", dto.Email);
                return (false, "این ایمیل قبلاً ثبت شده است.");
            }

            var user = dto.Adapt<User>();
            user.Password = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            string rawCode = new Random().Next(100000, 999999).ToString();
            user.ConfirmationToken = BCrypt.Net.BCrypt.HashPassword(rawCode);
            user.TokenExpiration = DateTime.UtcNow.AddMinutes(2);
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;
            user.IsConfirmed = false;

            await _userRepository.AddAsync(user);

            var roles = await _roleRepository.GetAllAsync();
            var defaultRole = roles.FirstOrDefault(r => r.Name == "کاربر");

            if (defaultRole != null)
            {
                await _userRepository.UpdateUserRolesAsync(user.Id, new List<int> { defaultRole.Id });
            }

            string emailBody = $@"
        <div style='font-family: Tahoma, Arial, sans-serif; direction: rtl; text-align: right;'>
            <h2>خوش آمدید، {user.Name}!</h2>
            <p>کد تایید حساب کاربری شما:</p>
            <h1 style='letter-spacing: 5px; color: #2563eb;'>{rawCode}</h1>
            <p style='margin-top: 20px; font-size: 12px; color: #666;'>این کد تا ۲ دقیقه معتبر است.</p>
        </div>";

            await _emailService.SendEmailAsync(user.Email, "کد تایید حساب کاربری در تیکت‌هاب", emailBody);

            _logger.LogInformation("ثبت نام با موفقیت انجام شد و ایمیل تایید برای {Email} ارسال گردید.", user.Email);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ثبت نام کاربر با ایمیل {Email}.", dto.Email);
            throw;
        }
    }

    public async Task<bool> ConfirmUserAsync(int userId, string token)
    {
        try
        {
            _logger.LogInformation("درخواست تایید حساب کاربری برای کاربر شناسه {UserId}.", userId);

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null || user.ConfirmationToken != token || user.TokenExpiration < DateTime.UtcNow)
            {
                _logger.LogWarning("تایید حساب ناموفق: کاربر یافت نشد یا توکن نامعتبر/منقضی برای کاربر {UserId}.", userId);
                return false;
            }

            user.IsConfirmed = true;
            user.ConfirmationToken = null;
            user.TokenExpiration = null;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("حساب کاربری با شناسه {UserId} با موفقیت تایید شد.", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تایید حساب کاربری شناسه {UserId}.", userId);
            throw;
        }
    }

    public async Task<AuthServiceResponse> LoginAsync(LoginViewModel model)
    {
        try
        {
            _logger.LogInformation("درخواست ورود برای ایمیل {Email}.", model.Email);

            var user = await _userRepository.GetByEmailAsync(model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                _logger.LogWarning("ورود ناموفق: ایمیل یا رمز عبور اشتباه برای {Email}.", model.Email);
                return new AuthServiceResponse { Success = false, ErrorMessage = "ایمیل یا رمز عبور اشتباه است." };
            }

            if (!user.IsConfirmed)
            {
                _logger.LogWarning("ورود متوقف شد: ایمیل {Email} تایید نشده است.", model.Email);

                // بررسی وجود کد معتبر: فقط در صورتی ایمیل ارسال شود که کدی نباشد یا منقضی شده باشد
                if (!user.TokenExpiration.HasValue || user.TokenExpiration.Value <= DateTime.UtcNow)
                {
                    _logger.LogInformation("تولید و ارسال مجدد کد تایید برای ایمیل {Email}.", model.Email);

                    string rawCode = new Random().Next(100000, 999999).ToString();
                    user.ConfirmationToken = BCrypt.Net.BCrypt.HashPassword(rawCode);
                    user.TokenExpiration = DateTime.UtcNow.AddMinutes(2);

                    await _userRepository.UpdateAsync(user);

                    string emailBody = $@"
                <div style='font-family: Tahoma, Arial, sans-serif; direction: rtl; text-align: right;'>
                    <h2>کد تایید حساب کاربری</h2>
                    <h1 style='letter-spacing: 5px; color: #2563eb;'>{rawCode}</h1>
                    <p>این کد تا ۲ دقیقه معتبر است.</p>
                </div>";

                    await _emailService.SendEmailAsync(user.Email, "کد تایید جدید", emailBody);
                }

                // چه کد جدید ارسال شده باشد و چه کد قبلی زمان داشته باشد، باید به تایید ارجاع شود
                return new AuthServiceResponse { Success = false, RequiresConfirmation = true, Email = user.Email };
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("ورود ناموفق: حساب کاربری {Email} غیرفعال می‌باشد.", model.Email);
                return new AuthServiceResponse { Success = false, ErrorMessage = "حساب کاربری شما غیرفعال می‌باشد." };
            }

            _logger.LogInformation("کاربر {Email} با موفقیت وارد سیستم شد.", model.Email);

            return new AuthServiceResponse
            {
                Success = true,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Roles = user.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role.Name).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در فرآیند ورود برای ایمیل {Email}.", model.Email);
            throw;
        }
    }
}