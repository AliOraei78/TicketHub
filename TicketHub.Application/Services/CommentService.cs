namespace TicketHub.Application.Services;

using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

using TicketHub.Core.Enums;

public class CommentService : ICommentService
{
    private readonly IRepository<Comment> _commentRepo;
    private readonly ITicketEventBroker _eventBroker;
    private readonly ILogger<CommentService> _logger;
    private readonly INotificationService? _notificationService;
    private readonly ITicketRepository? _ticketRepository;

    public event Action<int, CommentDto>? OnCommentAdded;

    public CommentService(
        IRepository<Comment> commentRepo,
        ITicketEventBroker eventBroker,
        ILogger<CommentService> logger,
        INotificationService? notificationService = null,
        ITicketRepository? ticketRepository = null)
    {
        _commentRepo = commentRepo;
        _eventBroker = eventBroker;
        _logger = logger;
        _notificationService = notificationService;
        _ticketRepository = ticketRepository;
    }

    public async Task<List<CommentDto>> GetCommentsByTicketIdAsync(int ticketId)
    {
        _logger.LogInformation("دریافت نظرات تیکت با شناسه {TicketId}.", ticketId);

        var comments = (await _commentRepo.GetAllWithIncludesAsync(c => c.User))
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .ToList();

        return comments.Adapt<List<CommentDto>>();
    }

    public async Task<CommentDto> AddCommentAsync(CommentDto dto, int currentUserId)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            throw new ValidationException("متن نظر نمی‌تواند خالی باشد.");

        if (!dto.TicketId.HasValue)
            throw new ValidationException("شناسه تیکت نامعتبر است.");

        _logger.LogInformation("ثبت نظر جدید روی تیکت {TicketId} توسط کاربر {UserId}.", dto.TicketId, currentUserId);

        var comment = new Comment
        {
            Content = dto.Content.Trim(),
            TicketId = dto.TicketId.Value,
            UserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepo.AddAsync(comment);

        var savedComments = await _commentRepo.GetAllWithIncludesAsync(c => c.User);
        var savedComment = savedComments.FirstOrDefault(c => c.Id == comment.Id);

        var resultDto = (savedComment ?? comment).Adapt<CommentDto>();

        // انتشار ایونت محلی
        OnCommentAdded?.Invoke(dto.TicketId.Value, resultDto);

        // انتشار ایونت سراسری روی تمام مدارها و کاربران آنلاین
        await _eventBroker.PublishCommentAddedAsync(dto.TicketId.Value, resultDto);

        if (_notificationService != null && _ticketRepository != null)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(dto.TicketId.Value);
                if (ticket != null)
                {
                    var commentExcerpt = resultDto.Content.Length > 70
                        ? resultDto.Content.Substring(0, 70) + "..."
                        : resultDto.Content;

                    var userName = resultDto.User?.Name ?? "کاربر";
                    int targetUserId = ticket.UserId != currentUserId ? ticket.UserId : currentUserId;

                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = targetUserId,
                        Title = $"پاسخ جدید روی تیکت #{ticket.Id}",
                        Message = $"{userName} روی تیکت «{ticket.Title}» پاسخ جدیدی ثبت کرد: {commentExcerpt}",
                        Type = NotificationType.TicketComment,
                        Severity = NotificationSeverity.Info,
                        ReferenceId = ticket.Id,
                        ActionUrl = $"/tickets/{ticket.Id}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "خطا در ارسال اعلان ثبت نظر.");
            }
        }

        return resultDto;
    }

    public async Task DeleteCommentAsync(int id, int currentUserId, bool hasFullAccess)
    {
        _logger.LogWarning("درخواست حذف نظر با شناسه {CommentId}.", id);

        var comment = await _commentRepo.GetByIdAsync(id);

        if (comment == null)
            throw new NotFoundException("نظر", id);

        if (comment.UserId != currentUserId && !hasFullAccess)
            throw new ForbiddenException("شما دسترسی لازم برای حذف این نظر را ندارید.");

        int? ticketId = comment.TicketId;
        await _commentRepo.DeleteAsync(id);

        _logger.LogInformation("نظر با شناسه {CommentId} با موفقیت حذف شد.", id);

        if (ticketId.HasValue)
        {
            // انتشار ایونت حذف روی تمام مدارها و کاربران آنلاین
            await _eventBroker.PublishCommentDeletedAsync(ticketId.Value, id);
        }
    }
}
