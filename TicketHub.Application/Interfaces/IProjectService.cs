// TicketSystem.Application/Interfaces/IProjectService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Core.Entities;

namespace TicketSystem.Application.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllProjectsAsync();
        Task CreateProjectAsync(Project project);
    }
}