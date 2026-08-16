using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class StatusesE2ETests : PlaywrightTestBase
    {
        public StatusesE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Create_Status_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var newStatusName = "وضعیت تست " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: In Progress']", newStatusName);
            await Page.FillAsync("input[type='color']", "#ff0000");

            // Submit form
            await Page.ClickAsync("button:has-text('ثبت وضعیت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newStatusName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Edit_Status_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var newStatusName = "وضعیت ویرایشی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: In Progress']", newStatusName);
            await Page.ClickAsync("button:has-text('ثبت وضعیت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newStatusName }).First;
            await rowLocator.WaitForAsync();

            // Click edit on this specific row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            // Wait for form to enter edit mode
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش وضعیت')");
            await editHeaderLocator.WaitForAsync();

            var updatedName = newStatusName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='مثال: In Progress']", updatedName);
            await Page.Keyboard.PressAsync("Tab");
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')");

            var editToastLocator = Page.Locator("text=ویرایش شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            var updatedRowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRowLocator.WaitForAsync();
            (await updatedRowLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Delete_Status_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var newStatusName = "وضعیت حذفی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: In Progress']", newStatusName);
            await Page.ClickAsync("button:has-text('ثبت وضعیت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newStatusName }).First;
            await rowLocator.WaitForAsync();

            // Click delete
            await rowLocator.Locator("button[title='حذف']").ClickAsync();

            // Confirm delete
            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync();
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            var deleteToastLocator = Page.Locator("text=حذف شد").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify row is gone
            await rowLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await rowLocator.IsVisibleAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var headerLocator = Page.Locator("h1:has-text('مدیریت وضعیت')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('ثبت وضعیت')");

            var errorLocator = Page.Locator("text=نام وضعیت الزامی است").First;
            await errorLocator.WaitForAsync();
            (await errorLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task StatusesSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            // 1. Create 3 items
            var prefix = "BulkStatus_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for (int i = 1; i <= 3; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: In Progress']", $"{prefix}{i}");
                await Page.FillAsync("input[type='color']", "#ff0000");
                await Page.ClickAsync("button:has-text('ثبت وضعیت')");

                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.WaitForTimeoutAsync(500);
            }

            // 2. Select all 3
            var searchInput = Page.Locator("input[placeholder='جستجوی وضعیت...']");
            await searchInput.FillAsync(prefix);
            await Page.WaitForTimeoutAsync(600);

            for (int i = 1; i <= 3; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                await row.ScrollIntoViewIfNeededAsync();
                await row.Locator("label.cyber-checkbox-container input, input[type='checkbox']").First.ClickAsync(new() { Force = true });
                var countLocator = Page.Locator("div.fixed.bottom-6", new() { HasTextString = $"{i}" }).First;
                await countLocator.WaitForAsync(new() { Timeout = 10000 });
            }

            // 3. Delete 1 via individual button
            var rowToDelete = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}1" });
            var deleteBtn = rowToDelete.Locator("button[title='حذف']");
            await deleteBtn.ClickAsync(new() { Force = true });

            var modalConfirm1 = Page.Locator("button:has-text('بله، حذف کن')").First;
            await modalConfirm1.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await modalConfirm1.ClickAsync(new() { Force = true });

            var deleteToastLocator = Page.Locator("text=حذف شد").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(600);

            // 4. Verify bulk action toolbar indicates selected count
            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6", new() { HasTextString = "2" }).First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 10000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();

            // 5. Click bulk delete
            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('حذف گروهی')");

            var bulkDeleteModalText = Page.Locator("text=وضعیت انتخاب شده مطمئن هستید").First;
            await bulkDeleteModalText.WaitForAsync(new() { Timeout = 10000 });

            var modalConfirm2 = Page.Locator("button:has-text('بله، حذف کن')").First;
            await modalConfirm2.ClickAsync(new() { Force = true });

            var pluralToast = Page.Locator("text=با موفقیت حذف").Last;
            await pluralToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(600);

            // 6. Create 2 more and test activate/deactivate
            for (int i = 4; i <= 5; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: In Progress']", $"{prefix}{i}");
                await Page.FillAsync("input[type='color']", "#ff0000");
                await Page.ClickAsync("button:has-text('ثبت وضعیت')");

                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            await searchInput.FillAsync(prefix);
            await Page.WaitForTimeoutAsync(600);

            for (int i = 4; i <= 5; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                await row.ScrollIntoViewIfNeededAsync();
                await row.Locator("label.cyber-checkbox-container input, input[type='checkbox']").First.ClickAsync(new() { Force = true });
                var countLocator = Page.Locator("div.fixed.bottom-6", new() { HasTextString = $"{i - 3}" }).First;
                await countLocator.WaitForAsync(new() { Timeout = 10000 });
            }

            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('غیرفعال‌سازی')");
            var pluralDeactivateToast = Page.Locator("text=با موفقیت غیرفعال").Last;
            await pluralDeactivateToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(600);

            var rowSingular = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}4" });
            await rowSingular.ScrollIntoViewIfNeededAsync();
            await rowSingular.Locator("label.cyber-checkbox-container input, input[type='checkbox']").First.ClickAsync(new() { Force = true });
            var countSingular = Page.Locator("div.fixed.bottom-6", new() { HasTextString = "1" }).First;
            await countSingular.WaitForAsync(new() { Timeout = 10000 });

            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('فعال‌سازی')");
            var singularActivateToast = Page.Locator("text=با موفقیت فعال").Last;
            await singularActivateToast.WaitForAsync(new() { Timeout = 15000 });
        }

        [Fact]
        public async Task StatusForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var statusName = "وضعیت لغوی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: In Progress']", statusName);
            await Page.ClickAsync("button:has-text('ثبت وضعیت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Click edit
            var rowLocator = Page.Locator("tr:has(td:has-text('" + statusName + "'))").First;
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            var editHeader = Page.Locator("h3:has-text('ویرایش وضعیت')").First;
            await editHeader.WaitForAsync();

            // Click cancel
            await Page.ClickAsync("button:has-text('انصراف')");

            var createHeader = Page.Locator("h3:has-text('ایجاد وضعیت جدید')").First;
            await createHeader.WaitForAsync();
            (await createHeader.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task StatusesSettings_3StateStatusFilter_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            await Page.WaitForSelectorAsync("h1:has-text('مدیریت وضعیت')");

            // Click Active filter
            await Page.ClickAsync("button:has-text('فعال (')");
            await Task.Delay(500);

            // Click Inactive filter
            await Page.ClickAsync("button:has-text('غیرفعال (')");
            await Task.Delay(500);

            // Click All filter
            await Page.ClickAsync("button:has-text('همه (')");
            await Task.Delay(500);
        }
    }
}


