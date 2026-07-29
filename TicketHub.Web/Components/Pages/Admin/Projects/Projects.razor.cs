using Microsoft.AspNetCore.Components;
using TicketHub.Application.Services;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TicketHub.Web.Components.Pages.Admin.Projects;

public partial class Projects : ComponentBase
{
    [Inject] public IProjectService ProjectService { get; set; } = default!;

    private IEnumerable<Project> projects = new List<Project>();
    private IEnumerable<Workflow> workflows = new List<Workflow>();

    private Project projectModel = new Project();
    private bool isFormModalOpen = false;
    private int? deletingProjectId;

    private Project? projectToDelete;
    private bool isDeleteModalOpen = false;

    private string searchTerm = string.Empty;
    private bool? selectedFilterStatus = null;

    private IEnumerable<Project> FilteredProjects =>
        projects
        .Where(p => string.IsNullOrWhiteSpace(searchTerm) || p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(p => selectedFilterStatus == null || p.IsActive == selectedFilterStatus);

    private void FilterByStatus(bool? status) => selectedFilterStatus = status;
    private void HandleSearch(string term) => searchTerm = term;

    protected override async Task OnInitializedAsync()
    {
        workflows = await ProjectService.GetWorkflowsAsync();
        await LoadProjects();
    }

    private async Task LoadProjects() => projects = await ProjectService.GetProjectsAsync();

    private void OpenCreateModal()
    {
        projectModel = new Project();
        isFormModalOpen = true;
    }

    private void OpenEditModal(Project project)
    {
        projectModel = new Project
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            IsActive = project.IsActive,
            WorkflowId = project.WorkflowId
        };
        isFormModalOpen = true;
    }

    private void CloseFormModal() => isFormModalOpen = false;

    private async Task HandleSaveProject()
    {
        if (projectModel.Id == 0)
        {
            await ProjectService.AddProjectAsync(projectModel);
        }
        else
        {
            var trackedProject = projects.FirstOrDefault(p => p.Id == projectModel.Id);
            if (trackedProject != null)
            {
                trackedProject.Name = projectModel.Name;
                trackedProject.Description = projectModel.Description;
                trackedProject.IsActive = projectModel.IsActive;
                trackedProject.WorkflowId = projectModel.WorkflowId;

                await ProjectService.UpdateProjectAsync(trackedProject);
            }
        }

        await LoadProjects();
        isFormModalOpen = false;
    }

    private void OpenDeleteModal(Project project)
    {
        projectToDelete = project;
        isDeleteModalOpen = true;
    }

    private void CloseDeleteModal()
    {
        isDeleteModalOpen = false;
        projectToDelete = null;
    }

    private async Task ConfirmDelete()
    {
        if (projectToDelete != null)
        {
            var idToDelete = projectToDelete.Id;
            deletingProjectId = idToDelete;
            CloseDeleteModal();

            StateHasChanged();
            await Task.Delay(400);

            await ProjectService.DeleteProjectAsync(idToDelete);
            await LoadProjects();

            deletingProjectId = null;
        }
        else
        {
            CloseDeleteModal();
        }
    }
}
