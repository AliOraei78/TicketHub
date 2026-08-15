using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;
using System;
using Microsoft.Extensions.DependencyInjection;
using TicketHub.Infrastructure.Data;
using TicketHub.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using FluentAssertions;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class TicketCreationModalE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public TicketCreationModalE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private AppDbContext CreateDbContext()
        {
            var factory = Factory.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            return factory.CreateDbContext();
        }

        private async Task SeedTicketFieldsAsync()
        {
            await using var ctx = CreateDbContext();

            var category = await ctx.Categories.FirstOrDefaultAsync(c => c.Name == "عمومی");
            if (category == null) throw new Exception("دسته‌بندی 'عمومی' یافت نشد – DbInitializer اجرا نشده است.");

            var fieldTypes = await ctx.FieldTypes.ToDictionaryAsync(f => f.Type, f => f.Id);

            var definitions = new[]
            {
                ("فیلد متنی",        "متن کوتاه (Text)",                         "تست متن",          (string?)null,      false),
                ("فیلد طولانی",      "متن طولانی (TextArea)",                    "تست متن طولانی",   (string?)null,      false),
                ("فیلد عددی",        "عدد (Number)",                              "123",              (string?)null,      false),
                ("فیلد تاریخ",       "تاریخ (Date)",                              "تست تاریخ",        (string?)null,      false),
                ("فیلد کشویی",       "لیست کشویی (Dropdown)",                     "تست کشویی",        (string?)"گزینه 1,گزینه 2", false),
                ("فیلد چند انتخابی", "لیست کشویی چند گزینه‌ای (MultipleDropdown)","تست چند انتخابی", (string?)"الف,ب,ج", false),
                ("فیلد چک باکس",     "چک‌باکس (Checkbox)",                       "تست چک باکس",      (string?)null,      false),
                ("فیلد فایل",        "آپلود فایل (File)",                         "تست فایل",         (string?)null,      false),
                ("فیلد رنگ",         "انتخاب رنگ (ColorPicker)",                  "تست رنگ",          (string?)null,      false),
            };

            int order = 1;
            foreach (var (name, typeName, placeholder, options, required) in definitions)
            {
                if (!await ctx.TicketFields.AnyAsync(f => f.Name == name))
                {
                    var field = new TicketField
                    {
                        Name        = name,
                        Placeholder = placeholder,
                        SortOrder   = order++,
                        FieldTypeId = fieldTypes[typeName],
                        Options     = options,
                        IsRequired  = required
                    };
                    await ctx.TicketFields.AddAsync(field);
                    await ctx.SaveChangesAsync();

                    await ctx.FieldCategories.AddAsync(new FieldCategory
                    {
                        TicketFieldId = field.Id,
                        CategoryId    = category.Id
                    });
                    await ctx.SaveChangesAsync();
                }
            }
        }

        private static Task WaitForBlazorAsync(IPage page, int ms = 800)
            => Task.Delay(ms);

        [Fact]
        public async Task CreateTicket_WithAllFieldTypes_ShouldSucceed()
        {
            var serverUrl = Factory.ServerAddress;
            await SeedTicketFieldsAsync();

            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets");
            await Page.WaitForSelectorAsync("button:has-text('ثبت تیکت جدید')", new() { Timeout = 15_000 });

            // ── Open create modal
            await Page.ClickAsync("button:has-text('ثبت تیکت جدید')");
            await Page.WaitForSelectorAsync("h2:has-text('ایجاد تیکت پشتیبانی جدید')", new() { Timeout = 10_000 });
            await WaitForBlazorAsync(Page, 1000);

            // ── Fill Basic Info
            var ticketTitle = "تیکت جامع E2E " + Guid.NewGuid().ToString()[..5];
            await Page.FillAsync("input[placeholder='یک عنوان کوتاه بنویسید']", ticketTitle);
            await Page.FillAsync("textarea[placeholder='جزئیات مشکل یا درخواست خود را بنویسید...']", "تست تمامی فیلدها...");

            // ── Select Project
            var projectRoot = Page.Locator("div.relative:has(> label:has-text('پروژه مربوطه'))");
            await projectRoot.Locator("div.cursor-pointer").ClickAsync();
            await WaitForBlazorAsync(Page, 500);
            await projectRoot.Locator("li:has-text('عمومی')").First.ClickAsync(new() { Force = true });
            await WaitForBlazorAsync(Page, 500);

            // ── Select Priority
            var priorityRoot = Page.Locator("div.relative:has(> label:has-text('اولویت'))");
            await priorityRoot.Locator("div.cursor-pointer").ClickAsync();
            await WaitForBlazorAsync(Page, 500);
            await priorityRoot.Locator("li:has-text('متوسط')").First.ClickAsync(new() { Force = true });
            await WaitForBlazorAsync(Page, 500);

            // ── Select Category
            var categoryRoot = Page.Locator("div.relative:has(> label:has-text('دسته‌بندی'))");
            await categoryRoot.Locator("div.cursor-pointer").ClickAsync();
            await WaitForBlazorAsync(Page, 500);
            await categoryRoot.Locator("li:has-text('عمومی')").First.ClickAsync(new() { Force = true });
            
            // Wait for Blazor to fetch fields
            await WaitForBlazorAsync(Page, 1500);
            await Page.WaitForSelectorAsync("label:has-text('فیلد متنی')", new() { Timeout = 15_000 });

            // ── Dynamic Fields

            // 1. Text
            await Page.FillAsync("label:has-text('فیلد متنی') ~ input, label:has-text('فیلد متنی') + input", "متن تستی");

            // 2. TextArea
            await Page.FillAsync("label:has-text('فیلد طولانی') ~ textarea, label:has-text('فیلد طولانی') + textarea", "متن طولانی تستی");

            // 3. Number
            await Page.FillAsync("label:has-text('فیلد عددی') ~ input[type='number'], label:has-text('فیلد عددی') + input", "987");

            // 4. Checkbox
            var checkboxInput = Page.Locator("input[type='checkbox']").Last; 
            await checkboxInput.CheckAsync(new() { Force = true });

            // 5. ColorPicker
            var colorInput = Page.Locator("input[type='color']").Last;
            await colorInput.FillAsync("#0000ff");

            // 6. Dropdown
            var dropdownTrigger = Page.Locator("label:has-text('فیلد کشویی') ~ div").First;
            await dropdownTrigger.ClickAsync();
            await WaitForBlazorAsync(Page, 500);
            await Page.Locator("li").Filter(new() { HasText = "گزینه 1" }).And(Page.Locator(":visible")).First.ClickAsync(new() { Force = true });
            await WaitForBlazorAsync(Page, 500);

            // 7. MultipleDropdown
            var multiInput = Page.Locator("label:has-text('فیلد چند انتخابی') ~ div input").First;
            await multiInput.ClickAsync();
            await WaitForBlazorAsync(Page, 500);
            await Page.Locator("div.cursor-pointer:has(span:text-is('الف'))").First.ClickAsync(new() { Force = true });
            await Page.Locator("div.cursor-pointer:has(span:text-is('ب'))").First.ClickAsync(new() { Force = true });
            
            // close dropdown using the toggle icon
            var multiToggleIcon = Page.Locator("label:has-text('فیلد چند انتخابی') ~ div .absolute.inset-y-0").First;
            await multiToggleIcon.ClickAsync();
            await WaitForBlazorAsync(Page, 500);

            // 8. Date (Persian datepicker)
            var dateInput = Page.Locator("label:has-text('فیلد تاریخ') ~ div input, label:has-text('فیلد تاریخ') + div input").First;
            await dateInput.ClickAsync();
            await WaitForBlazorAsync(Page, 500);
            await Page.Locator(".jdp-day:not(.not-in-month)").First.ClickAsync(new() { Force = true });
            await WaitForBlazorAsync(Page, 500);

            // 9. File Upload
            var fileInput = Page.Locator("input[type='file']").Last;
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name    = "test.txt",
                MimeType = "text/plain",
                Buffer  = System.Text.Encoding.UTF8.GetBytes("Hello World")
            });
            await WaitForBlazorAsync(Page, 1000);

            // ── Submit
            await Page.ClickAsync("h2:has-text('ایجاد تیکت پشتیبانی جدید')"); // blur
            await WaitForBlazorAsync(Page, 500);
            await Page.ClickAsync("button[type='submit']:has-text('ثبت تیکت')");

            // Wait up to 10s for modal to disappear
            await Page.WaitForSelectorAsync("h2:has-text('ایجاد تیکت پشتیبانی جدید')",
                new() { State = WaitForSelectorState.Hidden, Timeout = 10_000 });

            // Verify in list
            var card = Page.Locator("h4", new PageLocatorOptions { HasTextString = ticketTitle });
            await card.WaitForAsync(new() { Timeout = 10_000 });
            (await card.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task CreateTicket_EmptyForm_ShouldShowValidationErrors()
        {
            var serverUrl = Factory.ServerAddress;
            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets");
            await Page.WaitForSelectorAsync("button:has-text('ثبت تیکت جدید')", new() { Timeout = 15_000 });

            await Page.ClickAsync("button:has-text('ثبت تیکت جدید')");
            await Page.WaitForSelectorAsync("h2:has-text('ایجاد تیکت پشتیبانی جدید')", new() { Timeout = 10_000 });
            await WaitForBlazorAsync(Page, 1000);

            // ── Submit empty form 
            await Page.ClickAsync("button[type='submit']:has-text('ثبت تیکت')");
            await WaitForBlazorAsync(Page, 1000);

            // ── Verify validation messages
            var bodyText = await Page.Locator("form.flex.flex-col").First.InnerTextAsync();
            bodyText.Should().Contain("عنوان تیکت الزامی است");
            bodyText.Should().Contain("توضیحات تیکت الزامی است");
        }

        [Fact]
        public async Task CreateTicket_CancelModal_ClosesWithoutSaving()
        {
            var serverUrl = Factory.ServerAddress;
            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets");
            await Page.WaitForSelectorAsync("button:has-text('ثبت تیکت جدید')", new() { Timeout = 15_000 });

            await Page.ClickAsync("button:has-text('ثبت تیکت جدید')");
            var header = Page.Locator("h2:has-text('ایجاد تیکت پشتیبانی جدید')").First;
            await header.WaitForAsync(new() { Timeout = 10_000 });
            await WaitForBlazorAsync(Page, 500);

            var cancelledTitle = "تیکت لغو شده " + Guid.NewGuid().ToString()[..5];
            await Page.FillAsync("input[placeholder='یک عنوان کوتاه بنویسید']", cancelledTitle);

            // Click Cancel button
            await Page.ClickAsync("button:has-text('انصراف')");
            await WaitForBlazorAsync(Page, 800);

            // Modal closes
            await header.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

            // Verify not in DB
            await using var ctx = CreateDbContext();
            var exists = await ctx.Tickets.AnyAsync(t => t.Title == cancelledTitle);
            exists.Should().BeFalse();
        }
    }
}
