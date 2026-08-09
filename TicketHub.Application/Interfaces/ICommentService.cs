using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface ICommentService
{
    Task<List<CommentDto>> GetCommentsByTicketIdAsync(int ticketId);
    Task<CommentDto> AddCommentAsync(CommentDto dto, int currentUserId);
    Task DeleteCommentAsync(int id, int currentUserId, bool hasFullAccess);
    event Action<int, CommentDto>? OnCommentAdded;
}
