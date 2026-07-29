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

        // جلوگیری از افتادن در حلقه بی‌نهایت برای Navigation Propertyهای دوطرفه
        config.Default.PreserveReference(true);
    }
}