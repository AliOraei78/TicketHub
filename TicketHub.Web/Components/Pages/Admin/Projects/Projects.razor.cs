using Microsoft.AspNetCore.Components;
using TicketHub.Application.Services;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Web.States;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TicketHub.Web.Components.Pages.Admin.Projects;

public partial class Projects : ComponentBase, IDisposable
{
    [Inject] public IProjectService ProjectService { get; set; } = default!;

    // اگر اینترفیس ساختید از IProjectState وگرنه از ProjectState استفاده کنید
    [Inject] public ProjectState State { get; set; } = default!;

    private Project projectModel = new Project();
    private bool isFormModalOpen = false;
    private int? deletingProjectId;

    private Project? projectToDelete;
    private bool isDeleteModalOpen = false;

    protected override async Task OnInitializedAsync()
    {
        State.OnChange += StateHasChanged;
        await State.InitializeAsync();
    }

    // پیاده‌سازی متد Dispose برای رفع ارور CS0535
    public void Dispose()
    {
        State.OnChange -= StateHasChanged;
    }

    private void FilterByStatus(bool? status) => State.SetFilter(status);

    private void HandleSearch(string term) => State.SetSearchTerm(term);

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
            // خواندن لیست پروژه‌ها از State
            var trackedProject = State.Projects.FirstOrDefault(p => p.Id == projectModel.Id);
            if (trackedProject != null)
            {
                trackedProject.Name = projectModel.Name;
                trackedProject.Description = projectModel.Description;
                trackedProject.IsActive = projectModel.IsActive;
                trackedProject.WorkflowId = projectModel.WorkflowId;

                await ProjectService.UpdateProjectAsync(trackedProject);
            }
        }

        // بارگذاری مجدد از طریق State
        await State.ReloadProjectsAsync();
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

            // بارگذاری مجدد از طریق State
            await State.ReloadProjectsAsync();

            deletingProjectId = null;
        }
        else
        {
            CloseDeleteModal();
        }
    }
}