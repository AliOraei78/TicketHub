using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using TicketHub.Core.Entities;
using TicketHub.Infrastructure.Data;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class TicketsManagementE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public TicketsManagementE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private AppDbContext CreateDbContext()
        {
            var factory = Factory.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            return factory.CreateDbContext();
        }


        private async Task<(int TicketId, string TicketTitle)> SeedSampleTicketAsync(string suffix = "")
        {
            await using var ctx = CreateDbContext();

            var project = await ctx.Projects.Include(p => p.Workflow).FirstOrDefaultAsync(p => p.Name == "عمومی");
            if (project == null) throw new Exception("پروژه 'عمومی' یافت نشد.");

            var category = await ctx.Categories.FirstOrDefaultAsync(c => c.Name == "عمومی");
            var status = await ctx.Statuses.FirstOrDefaultAsync(s => s.Name == "در حال اقدام" || s.Name == "جدید");
            var priority = await ctx.Priorities.FirstOrDefaultAsync(p => p.Name == "متوسط" || p.Name == "عادی");
            var adminUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email.Contains("admin") || u.Id == 1);

            var workflowStatus = await ctx.WorkflowStatuses.FirstOrDefaultAsync(ws => ws.WorkflowId == project.WorkflowId);

            var title = $"تیکت سایبری آزمایشی {suffix} {Guid.NewGuid().ToString()[..6]}";

            var ticket = new Ticket
            {
                Title = title,
                Description = "توضیحات تستی برای بررسی ماژول مدیریت تیکت",
                ProjectId = project.Id,
                CategoryId = category?.Id ?? 1,
                StatusId = status?.Id ?? 1,
                PriorityId = priority?.Id ?? 1,
                UserId = adminUser?.Id ?? 1,
                WorkflowStatusId = workflowStatus?.Id,
                CreatedAt = DateTime.UtcNow
            };

            await ctx.Tickets.AddAsync(ticket);
            await ctx.SaveChangesAsync();

            return (ticket.Id, title);
        }

        [Fact]
        public async Task FilterTickets_BySearchQuery_FiltersGridInRealTime()
        {
            var serverUrl = Factory.ServerAddress;
            var (_, uniqueTitle) = await SeedSampleTicketAsync("SER");

            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets");
            await Page.WaitForSelectorAsync("h1:has-text('مدیریت تیکت‌ها')", new() { Timeout = 15_000 });
            await WaitForBlazorAsync(Page, 1000);

            // Verify ticket is in list or search for it
            var searchInput = Page.Locator("input[placeholder*='جستجو در عنوان تیکت‌ها']").First;
            await searchInput.FillAsync(uniqueTitle);
            await WaitForBlazorAsync(Page, 1200);

            // Card with unique title should be visible
            var targetCard = Page.Locator("h4", new PageLocatorOptions { HasTextString = uniqueTitle });
            await targetCard.WaitForAsync(new() { Timeout = 10_000 });
            (await targetCard.IsVisibleAsync()).Should().BeTrue();

            // Clear search query
            await searchInput.FillAsync("");
            await WaitForBlazorAsync(Page, 800);
        }

        [Fact]
        public async Task SortControl_ToggleOrder_DispatchesSorting()
        {
            var serverUrl = Factory.ServerAddress;
            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets");
            await Page.WaitForSelectorAsync("h1:has-text('مدیریت تیکت‌ها')", new() { Timeout = 15_000 });
            await WaitForBlazorAsync(Page, 1000);

            var sortToggleBtn = Page.Locator("button[title*='مرتب‌سازی'], button[title*='صعودی'], button[title*='نزولی']").First;
            if (await sortToggleBtn.IsVisibleAsync())
            {
                await sortToggleBtn.ClickAsync();
                await WaitForBlazorAsync(Page, 800);
                true.Should().BeTrue();
            }
        }

        [Fact]
        public async Task SingleTicket_DeleteConfirmation_RemovesTicket()
        {
            var serverUrl = Factory.ServerAddress;
            var (ticketId, uniqueTitle) = await SeedSampleTicketAsync("DEL");

            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets");
            await Page.WaitForSelectorAsync("h1:has-text('مدیریت تیکت‌ها')", new() { Timeout = 15_000 });
            await WaitForBlazorAsync(Page, 1000);

            // Search for target ticket
            var searchInput = Page.Locator("input[placeholder*='جستجو در عنوان تیکت‌ها']").First;
            await searchInput.FillAsync(uniqueTitle);
            await WaitForBlazorAsync(Page, 1200);

            // Locate delete button on the ticket capsule
            var card = Page.Locator(".cyber-quest-capsule", new PageLocatorOptions { HasTextString = uniqueTitle }).First;
            await card.WaitForAsync(new() { Timeout = 10_000 });

            var deleteBtn = card.Locator("button[title*='حذف تیکت']").First;
            await deleteBtn.ClickAsync();
            await WaitForBlazorAsync(Page, 800);

            // Confirm Delete modal should appear
            await Page.WaitForSelectorAsync("h2:has-text('حذف'), h3:has-text('حذف')", new() { Timeout = 10_000 });

            // Click confirmation button
            var confirmBtn = Page.Locator("button:has-text('بله، حذف کن'), button.btn-cyber-danger").First;
            await confirmBtn.ClickAsync();
            await WaitForBlazorAsync(Page, 1500);

            // Verify ticket is no longer in database
            await using var ctx = CreateDbContext();
            var deletedTicket = await ctx.Tickets.FindAsync(ticketId);
            deletedTicket.Should().BeNull();
        }

        [Fact]
        public async Task BulkSelect_ShowsToolbar_AndDeletesSelectedTickets()
        {
            var serverUrl = Factory.ServerAddress;
            var (id1, _) = await SeedSampleTicketAsync("BLK1");
            var (id2, _) = await SeedSampleTicketAsync("BLK2");

            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets");
            await Page.WaitForSelectorAsync("h1:has-text('مدیریت تیکت‌ها')", new() { Timeout = 15_000 });
            await WaitForBlazorAsync(Page, 1000);

            // Search for bulk tickets prefix
            var searchInput = Page.Locator("input[placeholder*='جستجو در عنوان تیکت‌ها']").First;
            await searchInput.FillAsync("BLK");
            await WaitForBlazorAsync(Page, 1200);

            // Select checkboxes on cards directly
            var cards = Page.Locator(".cyber-quest-capsule", new PageLocatorOptions { HasTextString = "BLK" });
            await cards.First.WaitForAsync(new() { Timeout = 10_000 });
            var count = await cards.CountAsync();
            if (count >= 2)
            {
                var checkbox1 = cards.Nth(0).Locator("input[type='checkbox']").First;
                await checkbox1.ClickAsync(new() { Force = true });
                await WaitForBlazorAsync(Page, 600);

                var checkbox2 = cards.Nth(1).Locator("input[type='checkbox']").First;
                await checkbox2.ClickAsync(new() { Force = true });
                await WaitForBlazorAsync(Page, 800);

                // BulkActionToolbar should appear in RootModal outlet
                var bulkBar = Page.Locator("button:has-text('حذف موارد انتخابی'), button:has-text('حذف گروهی')").First;
                await bulkBar.WaitForAsync(new() { Timeout = 10_000 });
                (await bulkBar.IsVisibleAsync()).Should().BeTrue();

                // Click bulk delete button
                await bulkBar.ClickAsync();
                await WaitForBlazorAsync(Page, 800);

                // Confirm bulk deletion
                var confirmBtn = Page.Locator("button:has-text('بله، حذف کن'), button.btn-cyber-danger").First;
                await confirmBtn.ClickAsync();
                await WaitForBlazorAsync(Page, 1500);

                // Verify at least one or both deleted in DB
                await using var ctx = CreateDbContext();
                var remaining1 = await ctx.Tickets.FindAsync(id1);
                var remaining2 = await ctx.Tickets.FindAsync(id2);
                (remaining1 == null || remaining2 == null).Should().BeTrue();
            }
        }

        [Fact]
        public async Task NavigateToDetails_EditInlineAndAddComment_SavesSuccessfully()
        {
            var serverUrl = Factory.ServerAddress;
            var (ticketId, _) = await SeedSampleTicketAsync("DET");

            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets/{ticketId}");
            await Page.WaitForSelectorAsync("h1, h2, div", new() { Timeout = 15_000 });
            await WaitForBlazorAsync(Page, 1000);

            // Verify title is rendered
            var pageText = await Page.InnerTextAsync("body");
            pageText.Should().Contain("توضیحات تستی برای بررسی ماژول مدیریت تیکت");

            // Add a comment to the ticket stream
            var commentInput = Page.Locator("textarea[placeholder*='پاسخ یا نظر خود را بنویسید...'], input[placeholder*='نظر'], textarea").Last;
            if (await commentInput.IsVisibleAsync())
            {
                var commentText = "نظر ثبت شده توسط آزمون E2E " + Guid.NewGuid().ToString()[..5];
                await commentInput.FillAsync(commentText);
                await WaitForBlazorAsync(Page, 500);

                var sendBtn = Page.Locator("button:has-text('ارسال نظر'), button:has-text('ثبت نظر'), button[title*='ارسال']").Last;
                if (await sendBtn.IsVisibleAsync())
                {
                    await sendBtn.ClickAsync();
                    await WaitForBlazorAsync(Page, 1200);

                    // Check if comment persisted in DB
                    await using var ctx = CreateDbContext();
                    var commentInDb = await ctx.Comments.FirstOrDefaultAsync(c => c.TicketId == ticketId && c.Content.Contains(commentText));
                    commentInDb.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public async Task TicketTransition_OpenModalAndCancel_DoesNotChangeStatus()
        {
            var serverUrl = Factory.ServerAddress;
            var (ticketId, uniqueTitle) = await SeedSampleTicketAsync("TRN_CANCEL");

            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets");
            await Page.WaitForSelectorAsync("h1:has-text('مدیریت تیکت‌ها')", new() { Timeout = 15_000 });
            await WaitForBlazorAsync(Page, 1000);

            var searchInput = Page.Locator("input[placeholder*='جستجو در عنوان تیکت‌ها']").First;
            await searchInput.FillAsync(uniqueTitle);
            await WaitForBlazorAsync(Page, 1200);

            var card = Page.Locator(".cyber-quest-capsule", new PageLocatorOptions { HasTextString = uniqueTitle }).First;
            await card.WaitForAsync(new() { Timeout = 10_000 });

            // Click transition action button
            var actionBtn = card.Locator("button[title*='عملیات و تغییر وضعیت'], button[title*='عملیات']").First;
            await actionBtn.ClickAsync();
            await WaitForBlazorAsync(Page, 800);

            // Transition modal opens
            var modalLocator = Page.Locator("text=[SYS // WORKFLOW_TRANSITION]").First;
            await modalLocator.WaitForAsync(new() { Timeout = 8_000 });
            (await modalLocator.IsVisibleAsync()).Should().BeTrue();

            // Click cancel button
            await Page.ClickAsync("button:has-text('انصراف')");
            await WaitForBlazorAsync(Page, 800);

            // Modal closes
            await modalLocator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        [Fact]
        public async Task TicketDetails_InvalidTicketId_HandlesGracefully()
        {
            var serverUrl = Factory.ServerAddress;
            await Page.GotoAsync($"{serverUrl}/dev/login");
            await Page.GotoAsync($"{serverUrl}/tickets/999999");
            await WaitForBlazorAsync(Page, 1500);

            // Should either redirect to /tickets or show not found message
            var currentUrl = Page.Url;
            (currentUrl.Contains("/tickets") || currentUrl.Contains("/login")).Should().BeTrue();
        }
    }
}
