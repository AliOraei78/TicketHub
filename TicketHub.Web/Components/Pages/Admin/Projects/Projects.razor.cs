using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Facades;
using TicketHub.Web.States;

namespace TicketHub.Web.Components.Pages.Admin.Projects;

public partial class Projects : ComponentBase, IDisposable
{
    [Inject] public ProjectFacade ProjectFacade { get; set; } = default!;
    [Inject] public ProjectState State { get; set; } = default!;

    private ProjectDto projectModel = new();
    private bool isFormModalOpen;
    private int? deletingProjectId;

    private ProjectDto? projectToDelete;
    private bool isDeleteModalOpen;

    protected override async Task OnInitializedAsync()
    {
        State.OnChange += StateHasChanged;
        await ProjectFacade.InitializeAsync();
    }

    public void Dispose()
    {
        State.OnChange -= StateHasChanged;
    }

    private void FilterByStatus(bool? status) => ProjectFacade.SetFilter(status);

    private void HandleSearch(string term) => ProjectFacade.SetSearchTerm(term);

    private void OpenCreateModal()
    {
        projectModel = new ProjectDto();
        isFormModalOpen = true;
    }

    private void OpenEditModal(ProjectDto project)
    {
        projectModel = project.Adapt<ProjectDto>();
        isFormModalOpen = true;
    }

    private void CloseFormModal() => isFormModalOpen = false;

    private async Task HandleSaveProject()
    {
        await ProjectFacade.SaveProjectAsync(projectModel);
        isFormModalOpen = false;
    }

    private void OpenDeleteModal(ProjectDto project)
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

            await ProjectFacade.DeleteProjectAsync(idToDelete);
            deletingProjectId = null;
        }
        else
        {
            CloseDeleteModal();
        }
    }
}