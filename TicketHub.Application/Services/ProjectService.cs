// TicketSystem.Application/Services/ProjectService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IRepository<Project> _projectRepository;

        public ProjectService(IRepository<Project> projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            // Retrieve all projects from the database
            return await _projectRepository.GetAllAsync();
        }

        public async Task CreateProjectAsync(Project project)
        {
            // Add a new project to the database
            await _projectRepository.AddAsync(project);
        }
    }
}