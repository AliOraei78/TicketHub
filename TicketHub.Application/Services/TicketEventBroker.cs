namespace TicketHub.Application.Services;

using System;
using System.Threading.Tasks;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

public class TicketEventBroker : ITicketEventBroker
{
    public event Func<int, CommentDto, Task>? OnCommentAdded;
    public event Func<int, int, Task>? OnCommentDeleted;
    public event Func<int, Task>? OnTicketUpdated;
    public event Func<int, Task>? OnTransitionOccurred;
    public event Func<int, NotificationDto, Task>? OnNotificationReceived;

    public async Task PublishCommentAddedAsync(int ticketId, CommentDto comment)
    {
        if (OnCommentAdded != null)
        {
            foreach (Func<int, CommentDto, Task> handler in OnCommentAdded.GetInvocationList())
            {
                try
                {
                    await handler(ticketId, comment);
                }
                catch
                {
                    // Ignore disconnected circuit or disposed component errors safely
                }
            }
        }
    }

    public async Task PublishCommentDeletedAsync(int ticketId, int commentId)
    {
        if (OnCommentDeleted != null)
        {
            foreach (Func<int, int, Task> handler in OnCommentDeleted.GetInvocationList())
            {
                try
                {
                    await handler(ticketId, commentId);
                }
                catch
                {
                    // Ignore disconnected circuit or disposed component errors safely
                }
            }
        }
    }

    public async Task PublishTicketUpdatedAsync(int ticketId)
    {
        if (OnTicketUpdated != null)
        {
            foreach (Func<int, Task> handler in OnTicketUpdated.GetInvocationList())
            {
                try
                {
                    await handler(ticketId);
                }
                catch
                {
                    // Ignore disconnected circuit or disposed component errors safely
                }
            }
        }
    }

    public async Task PublishTransitionOccurredAsync(int ticketId)
    {
        if (OnTransitionOccurred != null)
        {
            foreach (Func<int, Task> handler in OnTransitionOccurred.GetInvocationList())
            {
                try
                {
                    await handler(ticketId);
                }
                catch
                {
                    // Ignore disconnected circuit or disposed component errors safely
                }
            }
        }
    }

    public async Task PublishNotificationAsync(int targetUserId, NotificationDto notification)
    {
        if (OnNotificationReceived != null)
        {
            foreach (Func<int, NotificationDto, Task> handler in OnNotificationReceived.GetInvocationList())
            {
                try
                {
                    await handler(targetUserId, notification);
                }
                catch
                {
                    // Ignore disconnected circuit or disposed component errors safely
                }
            }
        }
    }
}
