using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Core.Common;
using TicketHub.Core.Entities;

namespace TicketHub.Application.Mapping;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // مپ کردن شناسه‌ی نقش‌ها از موجودیت واسط به لیست اعداد در DTO
        config.NewConfig<Project, ProjectDto>()
              .Map(dest => dest.RoleIds, src => src.RoleProjects != null
                                                ? src.RoleProjects.Select(rp => rp.RoleId).ToList()
                                                : new List<int>());
        config.NewConfig<ProjectDto, Project>()
              .Ignore(dest => dest.Workflow);

        // اضافه کردن مپینگ کاربر به DTO
        config.NewConfig<User, UserDto>()
              .Map(dest => dest.RoleNames, src => src.UserRoles != null
                                                ? src.UserRoles.Select(ur => ur.Role.Name).ToList()
                                                : new List<string>());

        // Transition Mapping
        config.NewConfig<Transition, TransitionDto>()
              .Map(dest => dest.AllowedRoleIds, src => src.AllowedRoles != null
                                                        ? src.AllowedRoles.Select(r => r.RoleId).ToList()
                                                        : new List<int>());

        config.NewConfig<TransitionDto, Transition>()
              .Map(dest => dest.AllowedRoles, src => src.AllowedRoleIds != null
                                                    ? src.AllowedRoleIds.Select(id => new TransitionRole { RoleId = id, TransitionId = src.Id }).ToList()
                                                    : new List<TransitionRole>())
              .Ignore(dest => dest.Workflow)
              .Ignore(dest => dest.FromStatus)
              .Ignore(dest => dest.ToStatus);

        // Workflow Mapping
        config.NewConfig<Workflow, WorkflowDto>()
                      .Map(dest => dest.WorkflowStatuses, src => src.WorkflowStatuses);

        config.NewConfig<FieldTypeDto, FieldType>()
              .Ignore(dest => dest.TransitionFields);

        config.NewConfig<CanvasNode, CanvasNodeDto>();
        config.NewConfig<CanvasNodeDto, CanvasNode>()
              .Ignore(dest => dest.Status); // جلوگیری از خطای EF Core هنگام آپدیت نود

        // مپینگ برای WorkflowStatus (که روی بوم رسم می‌شود)
        config.NewConfig<WorkflowStatus, WorkflowStatusDto>();
        config.NewConfig<WorkflowStatusDto, WorkflowStatus>()
              .Ignore(dest => dest.Status)
              .Ignore(dest => dest.Workflow);

        // مپینگ برای فیلدهای کاستوم (TransitionField)
        config.NewConfig<TransitionField, TransitionFieldDto>();
        config.NewConfig<TransitionFieldDto, TransitionField>()
              .Ignore(dest => dest.Transition)
              .Ignore(dest => dest.FieldType)
              .Ignore(dest => dest.Attachments);

        config.NewConfig<Category, CategoryDto>()
              .Map(dest => dest.ProjectIds, src => src.CategoryProjects != null
                                                ? src.CategoryProjects.Select(cp => cp.ProjectId).ToList()
                                                : new List<int>())
              .Map(dest => dest.RoleIds, src => src.CategoryRoles != null
                                                ? src.CategoryRoles.Select(cr => cr.RoleId).ToList()
                                                : new List<int>());

        config.NewConfig<TicketField, TicketFieldDto>()
              .Map(dest => dest.CategoryIds, src => src.FieldCategories != null
                                                ? src.FieldCategories.Select(rp => rp.CategoryId).ToList()
                                                : new List<int>());
        config.NewConfig<TicketFieldDto, TicketField>()
              .Ignore(dest => dest.FieldType);

        config.NewConfig<Attachment, AttachmentDto>();
        config.NewConfig<AttachmentDto, Attachment>()
              .Ignore(dest => dest.Ticket)
              .Ignore(dest => dest.TicketHistory)
              .Ignore(dest => dest.TransitionField)
              .Ignore(dest => dest.TicketFieldValue);

        config.NewConfig<TicketFieldValue, TicketFieldValueDto>();
        config.NewConfig<TicketFieldValueDto, TicketFieldValue>()
              .Ignore(dest => dest.Ticket)
              .Ignore(dest => dest.TicketField);

        config.NewConfig<Comment, CommentDto>();
        config.NewConfig<CommentDto, Comment>()
              .Ignore(dest => dest.Ticket)
              .Ignore(dest => dest.User);

        config.NewConfig<TicketHistory, TicketHistoryDto>();
        config.NewConfig<TicketHistoryDto, TicketHistory>()
              .Ignore(dest => dest.Ticket)
              .Ignore(dest => dest.User)
              .Ignore(dest => dest.Transition)
              .Ignore(dest => dest.FromStatus)
              .Ignore(dest => dest.ToStatus);


        config.NewConfig<Ticket, TicketDto>()
              .Map(dest => dest.AttachmentIds, src => src.Attachments != null
                                                ? src.Attachments.Select(a => a.Id).ToList()
                                                : new List<int>())
              .Map(dest => dest.TicketHistoryIds, src => src.TicketHistories != null
                                                ? src.TicketHistories.Select(th => th.Id).ToList()
                                                : new List<int>())
              .Map(dest => dest.CommentIds, src => src.Comments != null
                                                ? src.Comments.Select(c => c.Id).ToList()
                                                : new List<int>())
              .Map(dest => dest.FieldValueIds, src => src.FieldValues != null
                                                ? src.FieldValues.Select(fv => fv.Id).ToList()
                                                : new List<int>());

        config.NewConfig<TicketDto, Ticket>()
              .Ignore(dest => dest.User)
              .Ignore(dest => dest.Project)
              .Ignore(dest => dest.Status)
              .Ignore(dest => dest.Category)
              .Ignore(dest => dest.Priority)
              .Ignore(dest => dest.WorkflowStatus);

        // Permission Mapping
        config.NewConfig<Permission, PermissionDto>()
              .Map(dest => dest.RoleIds, src => src.RolePermissions != null
                                                ? src.RolePermissions.Select(rp => rp.RoleId).ToList()
                                                : new List<int>());

        config.NewConfig<PermissionDto, Permission>()
              .Ignore(dest => dest.RolePermissions);

        // جلوگیری از افتادن در حلقه بی‌نهایت برای Navigation Propertyهای دوطرفه
        config.Default.PreserveReference(true);
    }
}