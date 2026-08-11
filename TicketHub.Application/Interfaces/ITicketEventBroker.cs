namespace TicketHub.Application.Interfaces;

using System;
using System.Threading.Tasks;
using TicketHub.Application.DTOs;

public interface ITicketEventBroker
{
    event Func<int, CommentDto, Task>? OnCommentAdded;
    event Func<int, int, Task>? OnCommentDeleted;
    event Func<int, Task>? OnTicketUpdated;
    event Func<int, Task>? OnTransitionOccurred;
    event Func<int, NotificationDto, Task>? OnNotificationReceived;

    Task PublishCommentAddedAsync(int ticketId, CommentDto comment);
    Task PublishCommentDeletedAsync(int ticketId, int commentId);
    Task PublishTicketUpdatedAsync(int ticketId);
    Task PublishTransitionOccurredAsync(int ticketId);
    Task PublishNotificationAsync(int targetUserId, NotificationDto notification);
}
