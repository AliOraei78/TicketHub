using Fluxor;
using Mapster;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Store; // فرض بر این است که فایل State جدید در این مسیر است

namespace TicketHub.Web.Components.Pages.Admin.Projects;

public partial class Projects
{
    [Inject] public IState<ProjectState> State { get; set; } = default!;
    [Inject] public IDispatcher Dispatcher { get; set; } = default!;

    private ProjectDto projectModel = new();
    private bool isFormModalOpen;
    private int? deletingProjectId;
    private ProjectDto? projectToDelete;
    private bool isDeleteModalOpen;

    // اعمال فیلتر به صورت داینامیک در UI بدون نیاز به ذخیره لیست جداگانه در State
    private IEnumerable<ProjectDto> FilteredProjects =>
        State.Value.Projects
        .Where(p => string.IsNullOrWhiteSpace(State.Value.SearchTerm) || p.Name.Contains(State.Value.SearchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(p => State.Value.SelectedFilterStatus == null || p.IsActive == State.Value.SelectedFilterStatus);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Dispatcher.Dispatch(new LoadProjectsAction());
    }

    private void FilterByStatus(bool? status) => Dispatcher.Dispatch(new SetProjectFilterAction(status));
    private void HandleSearch(string term) => Dispatcher.Dispatch(new SetProjectSearchAction(term));

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

    private void HandleSaveProject()
    {
        Dispatcher.Dispatch(new SaveProjectAction(projectModel));
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

            await Task.Delay(400); // تاخیر برای انیمیشن

            Dispatcher.Dispatch(new DeleteProjectAction(idToDelete));
            deletingProjectId = null;
        }
        else
        {
            CloseDeleteModal();
        }
    }
}