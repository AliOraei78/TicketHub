# TicketHub

سامانه جامع و بلادرنگ مدیریت تیکت‌ها و گردش‌کارهای سازمانی مبتنی بر .NET 10، Blazor Interactive Server و Clean Architecture.

## ساختار و لایه‌های پروژه (Architecture & Layers)

- **TicketHub.Core:** موجودیت‌های دامنه، ماشین حالت گردش‌کار (Workflow State Machine) و فیلدهای پویا.
- **TicketHub.Application:** منطق کسب‌وکار، اعتبارسنجی (FluentValidation)، مپینگ (Mapster) و اینترفیس‌های سرویس.
- **TicketHub.Infrastructure:** دسترسی به داده با EF Core 10 و PostgreSQL (Npgsql)، صف پیام MassTransit/RabbitMQ، کش Redis و Hangfire.
- **TicketHub.Web:** رابط کاربری بلادرنگ Blazor Web App (Interactive Server)، مدیریت وضعیت Fluxor و Tailwind CSS v4.

## الگوهای طراحی و بازآفرینی در Blazor (Design & Refactoring Patterns)

1. **Smart & Dumb Components:** تفکیک کامپوننت‌های هوشمند منطقی از کامپوننت‌های نمایشی خالص.
2. **RenderFragment (Templated Components):** طراحی کامپوننت‌های منعطف با قابلیت تزریق قالب و محتوا.
3. **Facade Pattern:** ساده‌سازی ارتباط لایه Presentation با سرویس‌ها و Storeهای متعدد.
4. **State Containers & Fluxor:** مدیریت پیش‌بینی‌پذیر و جریان تک‌مسیره داده در Stateهای برنامه.
5. **Generic Components (`@typeparam`):** ساخت کامپوننت‌های داده‌ای قابل‌استفاده مجدد با انواع تایپ جنریک.
6. **Code-Behind (`.razor.cs`):** تفکیک کامل کدهای منطقی C# از کدهای نشانه‌گذاری Razor.

## مستندات تکمیلی

- مشخصات و هویت محصول: [PRODUCT.md](PRODUCT.md)
- یادداشت‌های فنی و معماری: [Notes.md](Notes.md)
- لاگ‌های روند توسعه و رخدادها: [Journey.md](Journey.md)
