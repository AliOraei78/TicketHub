// TicketSystem.Application/Interfaces/IStatusService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Core.Entities;

namespace TicketSystem.Application.Interfaces
{
    public interface IStatusService
    {
        Task<IEnumerable<Status>> GetAllStatusesAsync();
        Task CreateStatusAsync(Status status);
        Task UpdateStatusAsync(Status status);
        Task DeleteStatusAsync(int id);
    }
}