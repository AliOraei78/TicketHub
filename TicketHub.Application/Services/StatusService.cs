// TicketSystem.Application/Services/StatusService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Application.Services
{
    public class StatusService : IStatusService
    {
        private readonly IRepository<Status> _StatusRepository;

        public StatusService(IRepository<Status> StatusRepository)
        {
            _StatusRepository = StatusRepository;
        }

        public async Task<IEnumerable<Status>> GetAllStatusesAsync()
        {
            // Retrieve all Statuss from the database
            return await _StatusRepository.GetAllAsync();
        }

        public async Task CreateStatusAsync(Status Status)
        {
            // Add a new Status to the database
            await _StatusRepository.AddAsync(Status);
        }

        public async Task UpdateStatusAsync(Status Status)
        {
            await _StatusRepository.UpdateAsync(Status);
        }

        public async Task DeleteStatusAsync(int id)
        {
            await _StatusRepository.DeleteAsync(id);
        }
    }
}