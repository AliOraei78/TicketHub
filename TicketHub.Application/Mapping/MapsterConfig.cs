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

        // جلوگیری از افتادن در حلقه بی‌نهایت برای Navigation Propertyهای دوطرفه
        config.Default.PreserveReference(true);
    }
}