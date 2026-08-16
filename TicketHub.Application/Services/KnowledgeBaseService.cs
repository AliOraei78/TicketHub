using System.Collections.Concurrent;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;

namespace TicketHub.Application.Services;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private static readonly ConcurrentDictionary<string, (int Helpful, int NotHelpful)> ArticleFeedback = new();

    private readonly List<KbCategoryDto> _categories = new()
    {
        new KbCategoryDto
        {
            Id = "cat-errors",
            Slug = "troubleshooting-errors",
            Title = "رفع خطاها و عیب‌یابی نرم‌افزاری",
            Description = "راهنمای گام‌به‌گام رفع خطاهای رایج سامانه، خطای اتصال و پیام‌های هشدار.",
            Icon = "bug",
            ColorClass = "text-rose-400 bg-rose-500/10 border-rose-500/30",
            ArticleCount = 3,
            DisplayOrder = 1
        },
        new KbCategoryDto
        {
            Id = "cat-auth",
            Slug = "account-and-security",
            Title = "حساب کاربری و امنیت",
            Description = "احراز هویت دو مرحله‌ای، بازیابی رمز عبور، مدیریت دسترسی‌ها و نشست‌ها.",
            Icon = "shield-check",
            ColorClass = "text-emerald-400 bg-emerald-500/10 border-emerald-500/30",
            ArticleCount = 2,
            DisplayOrder = 2
        },
        new KbCategoryDto
        {
            Id = "cat-tickets",
            Slug = "ticket-management",
            Title = "مدیریت تیکت‌ها و گردش‌کار",
            Description = "نحوه ثبت تیکت، پیگیری مهلت اقدام (SLA)، اولویت‌بندی و انتقال وضعیت‌ها.",
            Icon = "ticket",
            ColorClass = "text-cyan-400 bg-cyan-500/10 border-cyan-500/30",
            ArticleCount = 2,
            DisplayOrder = 3
        },
        new KbCategoryDto
        {
            Id = "cat-workflow",
            Slug = "automation-and-sla",
            Title = "اتوماسیون و مانیتورینگ SLA",
            Description = "قوانین محاسبه خودکار مهلت اقدام، فیلدهای پویا و طراحی بصری گردش‌کارها.",
            Icon = "cpu-chip",
            ColorClass = "text-indigo-400 bg-indigo-500/10 border-indigo-500/30",
            ArticleCount = 2,
            DisplayOrder = 4
        }
    };

    private readonly List<KbFaqItemDto> _faqs = new()
    {
        new KbFaqItemDto
        {
            Id = "faq-1",
            Question = "چرا پیام «قطع موقت اتصال // SIGNAL_LOST» نمایش داده می‌شود؟",
            Answer = "این پیام زمانی رخ می‌دهد که کانال WebSocket سرور به دلیل ناپایداری شبکه محلی یا فایروال به طور موقت قطع شود. سامانه به طور خودکار مکانیزم Reconnect را اجرا می‌کند یا می‌توانید کلید R را روی کیبورد فشار دهید.",
            CategorySlug = "troubleshooting-errors",
            CategoryTitle = "رفع خطاها و عیب‌یابی نرم‌افزاری"
        },
        new KbFaqItemDto
        {
            Id = "faq-2",
            Question = "چگونه می‌توانم مهلت اقدام (SLA) یک تیکت را تغییر دهم یا متوقف کنم؟",
            Answer = "مهلت اقدام بر اساس اولویت تیکت و ماتریس SLA تعریف‌شده در پروژه به صورت خودکار محاسبه می‌شود. تغییر اولویت توسط کارشناس ارشد یا انتقال تیکت به وضعیت‌های توقف‌کننده (مانند «در انتظار بازخورد کاربر») زمان‌سنج SLA را متوقف می‌کند.",
            CategorySlug = "automation-and-sla",
            CategoryTitle = "اتوماسیون و مانیتورینگ SLA"
        },
        new KbFaqItemDto
        {
            Id = "faq-3",
            Question = "کد تایید ایمیل (OTP) برای من ارسال نشده است؛ چه اقدامی انجام دهم؟",
            Answer = "ابتدا پوشه Spam یا Junk ایمیل خود را بررسی فرمایید. در صورت عدم دریافت پس از ۶۰ ثانیه، دکمه «ارسال مجدد کد» در صفحه تایید فعال می‌شود. همچنین اطمینان حاصل کنید که آدرس ایمیل خود را بدون غلط املایی وارد کرده‌اید.",
            CategorySlug = "account-and-security",
            CategoryTitle = "حساب کاربری و امنیت"
        },
        new KbFaqItemDto
        {
            Id = "faq-4",
            Question = "حداکثر حجم مجاز برای بارگذاری فایل‌های پیوست چقدر است؟",
            Answer = "به طور پیش‌فرض حداکثر حجم فایل پیوست ۱۰ مگابایت بوده و فرمت‌های مجاز شامل تصاویر (PNG, JPG, WEBP)، اسناد (PDF, DOCX) و فایل‌های فشرده (ZIP) می‌باشد.",
            CategorySlug = "ticket-management",
            CategoryTitle = "مدیریت تیکت‌ها و گردش‌کار"
        },
        new KbFaqItemDto
        {
            Id = "faq-5",
            Question = "خطای «محدودیت تعداد درخواست // Rate Limit 429» چیست و چگونه رفع می‌شود؟",
            Answer = "جهت جلوگیری از حملات Brute-force و اسپم، ارسال درخواست‌های ورود به ۵ بار در دقیقه محدود است. پس از گذشت ۶۰ ثانیه محدودیت به طور خودکار برطرف شده و دسترسی آزاد می‌شود.",
            CategorySlug = "troubleshooting-errors",
            CategoryTitle = "رفع خطاها و عیب‌یابی نرم‌افزاری"
        }
    };

    private readonly List<KbArticleDto> _articles = new()
    {
        new KbArticleDto
        {
            Id = "art-1",
            Slug = "fix-signalr-websocket-connection-lost",
            Title = "راهنمای جامع رفع خطای قطع اتصال و مدار SignalR (SIGNAL_LOST)",
            Summary = "علل قطعی وب‌سوکت در سامانه‌های بلادرنگ Blazor و روش‌های برقراری مجدد ارتباط با سرور بدون از دست رفتن اطلاعات فرم.",
            CategorySlug = "troubleshooting-errors",
            CategoryTitle = "رفع خطاها و عیب‌یابی نرم‌افزاری",
            CategoryColorClass = "text-rose-400 bg-rose-500/10 border-rose-500/30",
            Tags = new() { "SignalR", "WebSocket", "عیب‌یابی", "مدار Blazor", "خطای اتصال" },
            ViewCount = 1420,
            HelpfulCount = 98,
            NotHelpfulCount = 3,
            LastUpdatedPersian = "۱۴۰۴/۰۵/۲۶",
            LastUpdatedIso = "2026-08-16T12:00:00Z",
            ReadTimeMinutes = 4,
            IsPopular = true,
            ContentHtml = @"
                <div class='space-y-4 text-slate-200 leading-relaxed'>
                    <p class='text-base'>در معماری مدرن Blazor Interactive Server، وضعیت کامپوننت‌ها به صورت زنده از طریق کانال دوطرفه <code>WebSocket</code> با سرور هماهنگ می‌شود. در صورت نوسان شبکه یا فیلتر بودن پورت‌های WSS، مودال <strong>قطع موقت اتصال // SIGNAL_LOST</strong> فعال می‌گردد.</p>

                    <div class='p-4 rounded-2xl bg-amber-950/40 border border-amber-500/40 text-amber-200 text-sm'>
                        <strong>نکته مهم:</strong> هنگام نمایش این مودال، اطلاعات وارد شده در فرم‌های باز حفظ می‌شوند و نیازی به رفرش فوری صفحه نیست.
                    </div>

                    <h3 class='text-lg font-bold text-white pt-2'>گام‌های حل مشکل:</h3>
                    <ol class='list-decimal list-inside space-y-2 text-sm text-slate-300 pr-2'>
                        <li><strong>تلاش خودکار مدار:</strong> سیستم به مدت ۶۰ ثانیه در فواصل ۲ ثانیه‌ای تلاش مجدد انجام می‌دهد.</li>
                        <li><strong>کلید میانبر فوری:</strong> با فشردن کلید <code>R</code> یا کلیک روی دکمه <em>«تلاش فوری مجدد»</em>، ارتباط بلافاصله بازنشانی می‌شود.</li>
                        <li><strong>بررسی فیلترشکن و VPN:</strong> در صورت فعال بودن پروکسی سازمانی، اطمینان حاصل کنید که پورت‌های WebSocket مسدود نشده باشند.</li>
                        <li><strong>تازه‌سازی صفحه (F5):</strong> اگر ارتباط پس از چند تلاش برقرار نشد، با کلیک روی <em>«تازه‌سازی صفحه»</em> سوکت جدیدی ایجاد کنید.</li>
                    </ol>
                </div>"
        },
        new KbArticleDto
        {
            Id = "art-2",
            Slug = "resolve-rate-limiting-429-too-many-requests",
            Title = "رفع خطای محدودیت تعداد درخواست (Rate Limit 429)",
            Summary = "آشنایی با سیاست‌های امنیتی مقابله با اسپم و نحوه آزاد‌سازی نشست‌های کاربری پس از قفل موقت.",
            CategorySlug = "troubleshooting-errors",
            CategoryTitle = "رفع خطاها و عیب‌یابی نرم‌افزاری",
            CategoryColorClass = "text-rose-400 bg-rose-500/10 border-rose-500/30",
            Tags = new() { "Rate Limit", "خطای 429", "امنیت", "اسپم", "احراز هویت" },
            ViewCount = 890,
            HelpfulCount = 74,
            NotHelpfulCount = 1,
            LastUpdatedPersian = "۱۴۰۴/۰۵/۲۶",
            LastUpdatedIso = "2026-08-16T12:00:00Z",
            ReadTimeMinutes = 3,
            IsPopular = true,
            ContentHtml = @"
                <div class='space-y-4 text-slate-200 leading-relaxed'>
                    <p class='text-base'>جهت تضمین پایداری سیستم و جلوگیری از حملات Brute-force علیه صفحات ورود و فرم‌های ثبت تیکت، سیستم مدیریت نرخ درخواست (Rate Limiter) فعال است.</p>

                    <h3 class='text-lg font-bold text-white pt-2'>جدول محدودیت‌های نرخ مجاز:</h3>
                    <ul class='list-disc list-inside space-y-1.5 text-sm text-slate-300 pr-2'>
                        <li><strong>صفحه ورود و احراز هویت:</strong> ۵ درخواست در دقیقه به ازای هر آدرس IP</li>
                        <li><strong>تولید کد امنیتی (Captcha):</strong> ۱۰ درخواست در دقیقه با پنجره لغزان</li>
                        <li><strong>درخواست‌های عمومی سامانه:</strong> حداکثر ۲۰۰ درخواست در دقیقه</li>
                    </ul>

                    <div class='p-4 rounded-2xl bg-cyan-950/40 border border-cyan-500/40 text-cyan-200 text-sm'>
                        <strong>راهکار سریع:</strong> کافی است ۶۰ ثانیه منتظر بمانید تا باجه زمانی ریست شود و سپس مجدداً اطلاعات را وارد نمایید.
                    </div>
                </div>"
        },
        new KbArticleDto
        {
            Id = "art-3",
            Slug = "dynamic-fields-validation-and-file-upload-guide",
            Title = "راهنمای اعتبارسنجی فیلدهای پویا و بارگذاری فایل‌های پیوست",
            Summary = "بررسی ۹ نوع فیلد سفارشی پروژه‌ها و قوانین اعتبارسنجی تاریخ شمسی، چندانتخابی و رنگ‌ها.",
            CategorySlug = "troubleshooting-errors",
            CategoryTitle = "رفع خطاها و عیب‌یابی نرم‌افزاری",
            CategoryColorClass = "text-rose-400 bg-rose-500/10 border-rose-500/30",
            Tags = new() { "فیلدهای پویا", "آپلود فایل", "اعتبارسنجی", "تاریخ شمسی" },
            ViewCount = 670,
            HelpfulCount = 52,
            NotHelpfulCount = 2,
            LastUpdatedPersian = "۱۴۰۴/۰۵/۲۶",
            LastUpdatedIso = "2026-08-16T12:00:00Z",
            ReadTimeMinutes = 3,
            IsPopular = false,
            ContentHtml = @"
                <div class='space-y-4 text-slate-200 leading-relaxed'>
                    <p class='text-base'>در سامانه TicketHub، هر دسته‌بندی تیکت می‌تواند مجموعه‌ای از ۹ فیلد داینامیک را از کاربر دریافت کند. در صورت ثبت ناموفق فرم، نکات زیر را بررسی فرمایید:</p>

                    <ul class='list-disc list-inside space-y-2 text-sm text-slate-300 pr-2'>
                        <li><strong>فرمت تاریخ شمسی:</strong> از ابزار انتخاب‌گر تقویم جلالی استفاده کنید یا تاریخ را با ساختار <code>YYYY/MM/DD</code> وارد نمایید.</li>
                        <li><strong>فیلد چندانتخابی (MultiSelect):</strong> برای دسته‌هایی که دارای حداقل انتخاب هستند، باید حداقل یک برچسب فعال باشد.</li>
                        <li><strong>پسوند فایل‌های پیوست:</strong> فایل‌های اجرایی مانند <code>.exe</code> و <code>.bat</code> به دلایل امنیتی رد خواهند شد.</li>
                    </ul>
                </div>"
        },
        new KbArticleDto
        {
            Id = "art-4",
            Slug = "how-sla-countdown-and-breach-monitoring-works",
            Title = "نحوه عملکرد محاسبه مهلت اقدام (SLA) و مانیتورینگ بلادرنگ",
            Summary = "آشنایی با الگوریتم زمان‌بندی مهلت پاسخ‌گویی، زنگ هشدار نقض مهلت و اولویت‌بندی هوشمند.",
            CategorySlug = "automation-and-sla",
            CategoryTitle = "اتوماسیون و مانیتورینگ SLA",
            CategoryColorClass = "text-indigo-400 bg-indigo-500/10 border-indigo-500/30",
            Tags = new() { "SLA", "مهلت اقدام", "اولویت‌بندی", "گردش کار" },
            ViewCount = 1180,
            HelpfulCount = 110,
            NotHelpfulCount = 4,
            LastUpdatedPersian = "۱۴۰۴/۰۵/۲۶",
            LastUpdatedIso = "2026-08-16T12:00:00Z",
            ReadTimeMinutes = 5,
            IsPopular = true,
            ContentHtml = @"
                <div class='space-y-4 text-slate-200 leading-relaxed'>
                    <p class='text-base'>شاخص توافق سطح خدمات (SLA) تضمین‌کننده سرعت پاسخ‌گویی به درخواست‌های مشتریان است. در داشبورد TicketHub، مهلت اقدام به صورت تایمرهای پویا با سه وضعیت رنگی نمایش می‌یابد:</p>

                    <div class='grid grid-cols-1 sm:grid-cols-3 gap-3 my-3'>
                        <div class='p-3 rounded-xl bg-emerald-950/60 border border-emerald-500/40 text-emerald-300 text-xs'>
                            <strong>سبز (ایمن):</strong> بیش از ۵۰٪ مهلت باقی‌مانده است.
                        </div>
                        <div class='p-3 rounded-xl bg-amber-950/60 border border-amber-500/40 text-amber-300 text-xs'>
                            <strong>زرد (هشدار):</strong> کمتر از ۲۵٪ مهلت باقی‌مانده است.
                        </div>
                        <div class='p-3 rounded-xl bg-rose-950/60 border border-rose-500/40 text-rose-300 text-xs'>
                            <strong>قرمز (نقض شده):</strong> مهلت به اتمام رسیده و اولویت خودکار بالا می‌رود.
                        </div>
                    </div>
                </div>"
        },
        new KbArticleDto
        {
            Id = "art-5",
            Slug = "two-factor-authentication-and-password-security",
            Title = "راهنمای امنیت رمز عبور و احراز هویت دومرحله‌ای",
            Summary = "دستورالعمل تنظیم رمز عبور مستحکم و فعال‌سازی تایید دو مرحله‌ای مبتنی بر ایمیل.",
            CategorySlug = "account-and-security",
            CategoryTitle = "حساب کاربری و امنیت",
            CategoryColorClass = "text-emerald-400 bg-emerald-500/10 border-emerald-500/30",
            Tags = new() { "امنیت", "رمز عبور", "2FA", "احراز هویت" },
            ViewCount = 930,
            HelpfulCount = 85,
            NotHelpfulCount = 2,
            LastUpdatedPersian = "۱۴۰۴/۰۵/۲۶",
            LastUpdatedIso = "2026-08-16T12:00:00Z",
            ReadTimeMinutes = 3,
            IsPopular = false,
            ContentHtml = @"
                <div class='space-y-4 text-slate-200 leading-relaxed'>
                    <p class='text-base'>برای حفاظت از اطلاعات سازمانی، رمزهای عبور کاربران بر اساس الگوهای سخت‌گیرانه ارزیابی می‌شوند:</p>
                    <ul class='list-disc list-inside space-y-1 text-sm text-slate-300 pr-2'>
                        <li>حداقل ۸ کاراکتر و ترکیب حروف بزرگ، کوچک، اعداد و نمادهای خاص.</li>
                        <li>استفاده از ابزار خودکار <strong>«تولید رمز عبور تصادفی سایبری»</strong> در پنل پروفایل.</li>
                    </ul>
                </div>"
        }
    };

    public Task<List<KbCategoryDto>> GetCategoriesAsync()
    {
        return Task.FromResult(_categories.OrderBy(c => c.DisplayOrder).ToList());
    }

    public Task<List<KbArticleDto>> GetArticlesAsync(string? categorySlug = null, string? tag = null, string? searchTerm = null)
    {
        var query = _articles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(a => a.CategorySlug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(a => a.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(a => a.Title.ToLowerInvariant().Contains(term) ||
                                     a.Summary.ToLowerInvariant().Contains(term) ||
                                     a.Tags.Any(t => t.ToLowerInvariant().Contains(term)));
        }

        var list = query.OrderByDescending(a => a.ViewCount).ToList();
        HydrateFeedback(list);
        return Task.FromResult(list);
    }

    public Task<KbArticleDto?> GetArticleBySlugAsync(string slug)
    {
        var article = _articles.FirstOrDefault(a => a.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (article != null)
        {
            HydrateFeedback(new List<KbArticleDto> { article });
            article.RelatedFaqs = _faqs.Where(f => f.CategorySlug == article.CategorySlug).Take(3).ToList();
        }
        return Task.FromResult(article);
    }

    public Task<List<KbArticleDto>> GetPopularArticlesAsync(int count = 6)
    {
        var list = _articles.Where(a => a.IsPopular).OrderByDescending(a => a.ViewCount).Take(count).ToList();
        HydrateFeedback(list);
        return Task.FromResult(list);
    }

    public Task<List<KbArticleDto>> GetRelatedArticlesAsync(string slug, string categorySlug, int count = 3)
    {
        var list = _articles.Where(a => !a.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && a.CategorySlug == categorySlug)
                            .Take(count)
                            .ToList();
        HydrateFeedback(list);
        return Task.FromResult(list);
    }

    public Task<List<KbFaqItemDto>> GetFaqsAsync(string? categorySlug = null)
    {
        if (string.IsNullOrWhiteSpace(categorySlug))
        {
            return Task.FromResult(_faqs);
        }

        return Task.FromResult(_faqs.Where(f => f.CategorySlug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    public Task<bool> SubmitFeedbackAsync(string slug, bool isHelpful)
    {
        ArticleFeedback.AddOrUpdate(
            slug,
            isHelpful ? (1, 0) : (0, 1),
            (_, current) => isHelpful ? (current.Helpful + 1, current.NotHelpful) : (current.Helpful, current.NotHelpful + 1)
        );
        return Task.FromResult(true);
    }

    private static void HydrateFeedback(List<KbArticleDto> articles)
    {
        foreach (var art in articles)
        {
            if (ArticleFeedback.TryGetValue(art.Slug, out var counts))
            {
                art.HelpfulCount += counts.Helpful;
                art.NotHelpfulCount += counts.NotHelpful;
            }
        }
    }
}
