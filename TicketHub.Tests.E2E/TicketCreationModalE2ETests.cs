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

            var projects = await ctx.Projects.Where(p => p.Name == "عمومی").ToListAsync();
            if (!projects.Any())
            {
                var newProj = new Project { Name = "عمومی", Description = "پروژه عمومی", IsActive = true };
                await ctx.Projects.AddAsync(newProj);
                await ctx.SaveChangesAsync();
                projects.Add(newProj);
            }

            var categories = await ctx.Categories.Where(c => c.Name == "عمومی").ToListAsync();
            if (!categories.Any())
            {
                var newCat = new Category { Name = "عمومی", IsActive = true };
                await ctx.Categories.AddAsync(newCat);
                await ctx.SaveChangesAsync();
                categories.Add(newCat);
            }

            foreach (var p in projects)
            {
                foreach (var c in categories)
                {
                    if (!await ctx.CategoryProjects.AnyAsync(cp => cp.CategoryId == c.Id && cp.ProjectId == p.Id))
                    {
                        await ctx.CategoryProjects.AddAsync(new CategoryProject { CategoryId = c.Id, ProjectId = p.Id });
                    }
                }
            }
            await ctx.SaveChangesAsync();

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
            foreach (var c in categories)
            {
                foreach (var (name, typeName, placeholder, options, required) in definitions)
                {
                    var existingField = await ctx.TicketFields.FirstOrDefaultAsync(f => f.Name == name);
                    if (existingField == null)
                    {
                        existingField = new TicketField
                        {
                            Name        = name,
                            Placeholder = placeholder,
                            SortOrder   = order++,
                            FieldTypeId = fieldTypes[typeName],
                            Options     = options,
                            IsRequired  = required,
                            IsActive    = true
                        };
                        await ctx.TicketFields.AddAsync(existingField);
                        await ctx.SaveChangesAsync();
                    }

                    if (!await ctx.FieldCategories.AnyAsync(fc => fc.TicketFieldId == existingField.Id && fc.CategoryId == c.Id))
                    {
                        await ctx.FieldCategories.AddAsync(new FieldCategory
                        {
                            TicketFieldId = existingField.Id,
                            CategoryId    = c.Id
                        });
                        await ctx.SaveChangesAsync();
                    }
                }
            }
        }


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
            await WaitForBlazorAsync(Page, 800);

            // ── Fill Basic Info
            var ticketTitle = "تیکت جامع E2E " + Guid.NewGuid().ToString()[..5];
            await Page.FillAsync("input[placeholder='یک عنوان کوتاه بنویسید']", ticketTitle);
            await Page.FillAsync("textarea[placeholder='جزئیات مشکل یا درخواست خود را بنویسید...']", "تست تمامی فیلدها...");

            // ── Select Priority
            var priorityContainer = Page.Locator("div:has(> label:has-text('اولویت'))").First;
            await priorityContainer.Locator(".field-spark-wrap div.cursor-pointer").ClickAsync();
            await priorityContainer.Locator(".dropdown-menu-container.opacity-100").WaitForAsync(new() { Timeout = 5000 });
            await priorityContainer.Locator(".dropdown-menu-container div.cursor-pointer:has-text('متوسط')").First.ClickAsync();
            await WaitForBlazorAsync(Page, 400);

            // ── Select Project
            var projectContainer = Page.Locator("div:has(> label:has-text('پروژه مربوطه'))").First;
            await projectContainer.Locator(".field-spark-wrap div.cursor-pointer").ClickAsync();
            await projectContainer.Locator(".dropdown-menu-container.opacity-100").WaitForAsync(new() { Timeout = 5000 });
            await projectContainer.Locator(".dropdown-menu-container div.cursor-pointer:has-text('عمومی')").First.ClickAsync();
            await WaitForBlazorAsync(Page, 800);

            // ── Select Category
            var categoryContainer = Page.Locator("div:has(> label:has-text('دسته‌بندی'))").First;
            await categoryContainer.Locator(".field-spark-wrap div.cursor-pointer").ClickAsync();
            await categoryContainer.Locator(".dropdown-menu-container.opacity-100").WaitForAsync(new() { Timeout = 5000 });
            var categoryItem = categoryContainer.Locator(".dropdown-menu-container div.cursor-pointer:has-text('عمومی')").First;
            await categoryItem.ClickAsync();
            
            // Wait for Blazor to fetch fields
            await WaitForBlazorAsync(Page, 1500);
            await Page.Locator("text=اطلاعات و مقادیر پایه").First.WaitForAsync(new() { Timeout = 15_000 });

            // ── Dynamic Fields

            // 1. Text
            var textInput = Page.Locator("input[placeholder='تست متن']").First;
            if (await textInput.IsVisibleAsync()) await textInput.FillAsync("متن تستی");

            // 2. TextArea
            var textArea = Page.Locator("textarea[placeholder='تست متن طولانی']").First;
            if (await textArea.IsVisibleAsync()) await textArea.FillAsync("متن طولانی تستی");

            // 3. Number
            var numInput = Page.Locator("input[placeholder='123']").First;
            if (await numInput.IsVisibleAsync()) await numInput.FillAsync("987");

            // 4. Checkbox
            var checkboxInput = Page.Locator("label:has-text('فیلد چک باکس')").Locator("..").Locator("label.cyber-checkbox-container").First; 
            if (await checkboxInput.IsVisibleAsync())
            {
                await checkboxInput.ClickAsync(new() { Force = true });
            }

            // 5. ColorPicker
            var colorInput = Page.Locator("input[type='color']").First;
            if (await colorInput.IsVisibleAsync())
            {
                await colorInput.FillAsync("#0000ff");
            }

            // 6. Dropdown
            var dropdownTrigger = Page.Locator("div:has(> label:has-text('فیلد کشویی')) .field-spark-wrap div.cursor-pointer").First;
            if (await dropdownTrigger.IsVisibleAsync())
            {
                await dropdownTrigger.ClickAsync();
                await WaitForBlazorAsync(Page, 300);
                var opt = Page.Locator(".dropdown-menu-container div.cursor-pointer:has-text('گزینه 1')").First;
                if (await opt.IsVisibleAsync()) await opt.ClickAsync(new() { Force = true });
                await WaitForBlazorAsync(Page, 300);
            }

            // 7. MultipleDropdown
            var multiInput = Page.Locator("div:has(> label:has-text('فیلد چند انتخابی')) input").First;
            if (await multiInput.IsVisibleAsync())
            {
                await multiInput.ClickAsync();
                await WaitForBlazorAsync(Page, 300);
                var optA = Page.Locator(".dropdown-menu-container div.cursor-pointer:has-text('الف')").First;
                if (await optA.IsVisibleAsync()) await optA.ClickAsync(new() { Force = true });
                await WaitForBlazorAsync(Page, 300);
            }

            // 8. Date (Persian datepicker)
            var dateInput = Page.Locator("div:has(> label:has-text('فیلد تاریخ')) input").First;
            if (await dateInput.IsVisibleAsync())
            {
                await dateInput.ClickAsync();
                await WaitForBlazorAsync(Page, 300);
                var day = Page.Locator(".jdp-day:not(.not-in-month)").First;
                if (await day.IsVisibleAsync())
                {
                    await day.ClickAsync(new() { Force = true });
                    await WaitForBlazorAsync(Page, 300);
                }
            }

            // 9. File Upload
            var fileInput = Page.Locator("input[type='file']").Last;
            if (await fileInput.CountAsync() > 0)
            {
                await fileInput.SetInputFilesAsync(new FilePayload
                {
                    Name    = "test.txt",
                    MimeType = "text/plain",
                    Buffer  = System.Text.Encoding.UTF8.GetBytes("Hello World")
                });
                await WaitForBlazorAsync(Page, 500);
            }

            // ── Submit
            await Page.ClickAsync("button[type='submit']:has-text('ثبت تیکت')");

            // Wait up to 15s for modal to disappear
            await Page.WaitForSelectorAsync("h2:has-text('ایجاد تیکت پشتیبانی جدید')",
                new() { State = WaitForSelectorState.Hidden, Timeout = 15_000 });

            // Verify in list
            var card = Page.Locator("h3, h4", new PageLocatorOptions { HasTextString = ticketTitle }).First;
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
            var titleError = Page.Locator("text=عنوان تیکت الزامی است").First;
            await titleError.WaitForAsync(new() { Timeout = 5000 });
            (await titleError.IsVisibleAsync()).Should().BeTrue();

            var descError = Page.Locator("text=توضیحات تیکت الزامی است").First;
            await descError.WaitForAsync(new() { Timeout = 5000 });
            (await descError.IsVisibleAsync()).Should().BeTrue();
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
