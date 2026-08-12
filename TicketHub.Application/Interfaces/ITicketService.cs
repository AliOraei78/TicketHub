using System.Security.Claims;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface ITicketService
{
    Task<TicketDto?> GetByIdAsync(int id);
    Task<(List<TicketDto> Tickets, int TotalCount)> GetFilteredTicketsAsync(string? searchTerm = null, 
        List<int>? projectIds = null, List<int>? statusIds = null, List<int>? priorityIds = null,
        int? userId = null, int page = 1, int pageSize = 10);
    Task CreateAsync(TicketDto dto);
    Task UpdateAsync(TicketDto dto);
    Task DeleteAsync(int id);
    Task DeleteRangeAsync(IEnumerable<int> ids);
    Task ExecuteTransitionAsync(ExecuteTransitionDto dto, int currentUserId);
    Task<List<TicketHistoryDto>> GetTransitionsByTicketIdAsync(int ticketId);
    Task DeleteAttachmentAsync(int attachmentId, int currentUserId, bool hasFullAccess);
    Task<AttachmentDto> UploadTicketAttachmentAsync(int ticketId, int? ticketFieldValueId, Stream fileStream, string fileName, string contentType);
    Task<bool> CanEditTicketAsync(int ticketId, ClaimsPrincipal user);
}

