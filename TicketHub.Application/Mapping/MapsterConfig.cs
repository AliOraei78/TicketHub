using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;

namespace TicketHub.Application.Mapping;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // تنظیمات صریح برای مواردی که نام‌گذاری تطابق دقیق ندارد
        config.NewConfig<Ticket, TicketListDto>()
              .Map(dest => dest.StatusColorCode, src => src.Status.ColorCode);

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

        // جلوگیری از افتادن در حلقه بی‌نهایت برای Navigation Propertyهای دوطرفه
        config.Default.PreserveReference(true);
    }
}