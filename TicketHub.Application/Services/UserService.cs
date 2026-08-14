using Mapster;
using Microsoft.Extensions.Logging;
using FluentValidation;
using System.Text.RegularExpressions;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

using Microsoft.AspNetCore.Http;
using TicketHub.Application.Enums;

namespace TicketHub.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IRepository<Role> _roleRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IValidator<UserDto> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;

    public UserService(
        IUserRepository userRepository,
        IEmailService emailService,
        IRepository<Role> roleRepository,
        ILogger<UserService> logger,
        IValidator<UserDto> validator,
        IHttpContextAccessor httpContextAccessor,
        IPermissionService permissionService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _roleRepository = roleRepository;
        _logger = logger;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
    }

    private async Task ValidateDtoAsync(UserDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            throw new ValidationException(errors);
        }
    }

    // متد جایگزین برای StrongPasswordAttribute
    private void ValidatePasswordStrict(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            !Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$"))
        {
            var errors = new Dictionary<string, string[]>
            {
                { "Password", new[] { "رمز عبور باید حداقل ۸ کاراکتر شامل حروف بزرگ و کوچک انگلیسی، عدد و کاراکتر ویژه (مانند !@#$) باشد." } }
            };
            throw new ValidationException(errors);
        }
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        _logger.LogInformation("شروع دریافت لیست تمامی کاربران.");

        var users = await _userRepository.GetAllAsync();
        return users.Adapt<List<UserDto>>();
    }

    public async Task<(List<UserDto> Users, int TotalCount)> GetFilteredUsersAsync(
        string searchTerm, List<int> roleIds, List<int> projectIds, bool? status, int page, int pageSize)
    {
        _logger.LogInformation("دریافت لیست کاربران با فیلتر. صفحه: {Page}، تعداد در صفحه: {PageSize}.", page, pageSize);

        var (users, totalCount) = await _userRepository.GetFilteredUsersAsync(
            searchTerm, roleIds, projectIds, status, page, pageSize);

        _logger.LogInformation("تعداد {TotalCount} کاربر منطبق با فیلترها یافت شد.", totalCount);
        return (users.Adapt<List<UserDto>>(), totalCount);
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("جستجوی کاربر با شناسه {Id}.", id);

        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            throw new NotFoundException("کاربر", id);

        return user.Adapt<UserDto>();
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        _logger.LogInformation("جستجوی کاربر با ایمیل {Email}.", email);
        var user = await _userRepository.GetByEmailAsync(email);
        return user?.Adapt<UserDto>();
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(
        int userId, string name, string phoneNumber, string? currentPassword, string? newPassword)
    {
        var userInDb = await _userRepository.GetByIdAsync(userId);
        if (userInDb == null)
            throw new NotFoundException("کاربر", userId);

        userInDb.Name = name;
        userInDb.PhoneNumber = phoneNumber;

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                return (false, "برای تغییر رمز، وارد کردن رمز عبور فعلی الزامی است.");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, userInDb.Password))
            {
                return (false, "رمز عبور فعلی اشتباه است.");
            }

            ValidatePasswordStrict(newPassword);
            userInDb.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        }

        await _userRepository.UpdateAsync(userInDb);
        _logger.LogInformation("پروفایل کاربر با شناسه {UserId} با موفقیت ویرایش شد.", userId);
        return (true, null);
    }


    private async Task EnsurePermissionAsync(PermissionType minType, string message)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user != null && user.Identity?.IsAuthenticated == true)
        {
            if (!await _permissionService.HasAccessAsync(user, "/users", minType))
            {
                throw new ForbiddenException(message);
            }
        }
    }

    public async Task CreateAsync(UserDto dto, string password, List<int> roleIds)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای ایجاد کاربر را ندارید.");
        await ValidateDtoAsync(dto);
        ValidatePasswordStrict(password);

        _logger.LogInformation("شروع ایجاد کاربر جدید با ایمیل {Email}.", dto.Email);

        var user = dto.Adapt<User>();
        user.Password = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;

        await _userRepository.AddAsync(user);
        await _userRepository.UpdateUserRolesAsync(user.Id, roleIds);

        _logger.LogInformation("کاربر جدید با شناسه {Id} با موفقیت ایجاد شد.", user.Id);
    }

    public async Task UpdateAsync(UserDto dto, string? password, List<int> roleIds)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای ویرایش کاربر را ندارید.");
        await ValidateDtoAsync(dto);

        _logger.LogInformation("ویرایش کاربر با شناسه {Id}.", dto.Id);

        var userInDb = await _userRepository.GetByIdAsync(dto.Id);
        if (userInDb == null)
            throw new NotFoundException("کاربر", dto.Id);

        dto.Adapt(userInDb);

        if (!string.IsNullOrWhiteSpace(password))
        {
            ValidatePasswordStrict(password); // چک کردن رمز عبور در صورت تغییر
            userInDb.Password = BCrypt.Net.BCrypt.HashPassword(password);
        }

        await _userRepository.UpdateAsync(userInDb);
        await _userRepository.UpdateUserRolesAsync(userInDb.Id, roleIds);

        _logger.LogInformation("کاربر با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
    }

    public async Task ExecuteBulkActionAsync(HashSet<int> userIds, string actionType, int? singleId = null)
    {
        _logger.LogInformation("اجرای عملیات گروهی {ActionType} روی کاربران.", actionType);

        switch (actionType)
        {
            case "Delete":
                await EnsurePermissionAsync(PermissionType.Full, "شما دسترسی لازم برای حذف گروهی کاربران را ندارید.");
                await _userRepository.BulkDeleteAsync(userIds);
                break;
            case "SingleDelete":
                await EnsurePermissionAsync(PermissionType.Full, "شما دسترسی لازم برای حذف کاربر را ندارید.");
                if (singleId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(singleId.Value);
                    if (user == null) throw new NotFoundException("کاربر", singleId.Value);

                    await _userRepository.DeleteAsync(singleId.Value);
                }
                break;
            case "Activate":
                await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای تغییر وضعیت کاربران را ندارید.");
                await _userRepository.BulkUpdateStatusAsync(userIds, true);
                break;
            case "Deactivate":
                await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای تغییر وضعیت کاربران را ندارید.");
                await _userRepository.BulkUpdateStatusAsync(userIds, false);
                break;
        }

        _logger.LogInformation("عملیات گروهی {ActionType} با موفقیت انجام شد.", actionType);
    }

    public async Task<(bool Success, string? ErrorMessage)> RegisterUserAsync(UserDto dto, string plainPassword)
    {
        await ValidateDtoAsync(dto);
        ValidatePasswordStrict(plainPassword);

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

    public async Task<bool> ConfirmUserAsync(int userId, string token)
    {
        _logger.LogInformation("درخواست تایید حساب کاربری برای کاربر شناسه {UserId}.", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || string.IsNullOrEmpty(user.ConfirmationToken))
        {
            _logger.LogWarning("تایید حساب ناموفق: کاربر یا توکن یافت نشد برای شناسه {UserId}.", userId);
            return false;
        }

        if (user.TokenExpiration.HasValue && user.TokenExpiration.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("تایید حساب ناموفق: توکن منقضی شده است برای کاربر {UserId}.", userId);
            return false;
        }

        bool isTokenValid = false;
        if (user.ConfirmationToken.StartsWith("$2") && user.ConfirmationToken.Length >= 28)
        {
            try
            {
                isTokenValid = BCrypt.Net.BCrypt.Verify(token, user.ConfirmationToken);
            }
            catch
            {
                isTokenValid = false;
            }
        }
        else
        {
            isTokenValid = user.ConfirmationToken == token;
        }

        if (!isTokenValid)
        {
            _logger.LogWarning("تایید حساب ناموفق: کد وارد شده نامعتبر است برای کاربر {UserId}.", userId);
            return false;
        }

        user.IsConfirmed = true;
        user.ConfirmationToken = null;
        user.TokenExpiration = null;

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("حساب کاربری با شناسه {UserId} با موفقیت تایید شد.", userId);
        return true;
    }

    public async Task<(bool Success, string? Message, int RemainingSeconds)> ResendConfirmationCodeAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.IsConfirmed)
        {
            return (false, "کاربر یافت نشد یا قبلاً تایید شده است.", 0);
        }

        // بررسی محدودیت ۲ دقیقه‌ای ارسال توکن (Anti-Spam Rate Limiter)
        if (user.TokenExpiration.HasValue && user.TokenExpiration.Value > DateTime.UtcNow)
        {
            var remaining = (int)Math.Ceiling((user.TokenExpiration.Value - DateTime.UtcNow).TotalSeconds);
            _logger.LogWarning("درخواست ارسال مجدد کد برای {Email} رد شد. توکن قبلی هنوز معتبر است ({Remaining} ثانیه باقی‌مانده).", email, remaining);
            return (false, $"کد تایید قبلی هنوز معتبر است. لطفاً {remaining} ثانیه دیگر مجدداً تلاش کنید.", remaining);
        }

        string rawCode = new Random().Next(100000, 999999).ToString();
        user.ConfirmationToken = BCrypt.Net.BCrypt.HashPassword(rawCode);
        user.TokenExpiration = DateTime.UtcNow.AddMinutes(2);

        await _userRepository.UpdateAsync(user);

        string emailBody = $@"
        <div style='font-family: Tahoma, Arial, sans-serif; direction: rtl; text-align: right;'>
            <h2>کد تایید جدید</h2>
            <p>کد تایید حساب کاربری شما:</p>
            <h1 style='letter-spacing: 5px; color: #2563eb;'>{rawCode}</h1>
            <p style='margin-top: 20px; font-size: 12px; color: #666;'>این کد تا ۲ دقیقه معتبر است.</p>
        </div>";

        await _emailService.SendEmailAsync(user.Email, "کد تایید جدید تیکت‌هاب", emailBody);
        _logger.LogInformation("کد تایید جدید برای {Email} با انقضای ۲ دقیقه‌ای با موفقیت ارسال شد.", email);

        return (true, "کد تایید جدید با موفقیت به ایمیل شما ارسال شد.", 120);
    }


    public async Task<AuthServiceResponse> LoginAsync(LoginViewModel model)
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

            // فقط در صورتی که توکن قبلاً منقضی شده باشد کد جدید ارسال می‌شود
            if (!user.TokenExpiration.HasValue || user.TokenExpiration.Value <= DateTime.UtcNow)
            {
                _logger.LogInformation("تولید و ارسال کد تایید جدید برای ایمیل {Email} (به علت انقضای کد قبلی).", model.Email);

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
}