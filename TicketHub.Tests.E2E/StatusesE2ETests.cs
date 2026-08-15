using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

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
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var newStatusName = "وضعیت تست " + System.Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: In Progress']", newStatusName);

            // Optional: Pick a color (Playwright can fill type="color")
            await Page.FillAsync("input[type='color']", "#ff0000");

            await Page.CheckAsync("input[id='needApproval']");

            // Submit form
            await Page.ClickAsync("button:has-text('ثبت وضعیت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newStatusName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Edit_Status_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var newStatusName = "وضعیت ویرایشی " + System.Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: In Progress']", newStatusName);
            await Page.ClickAsync("button:has-text('ثبت وضعیت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newStatusName }).First;
            await rowLocator.WaitForAsync();

            // Click edit on this specific row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            // Wait for form to enter edit mode to prevent Blazor overwriting our input
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش وضعیت')");
            await editHeaderLocator.WaitForAsync();

            var updatedName = newStatusName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='مثال: In Progress']", updatedName);
            await Page.Keyboard.PressAsync("Tab"); // Ensure Blazor updates the model before submit
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')"); 

            var editToastLocator = Page.Locator("text=ویرایش شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            var updatedRowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRowLocator.WaitForAsync();
            (await updatedRowLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Delete_Status_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var newStatusName = "وضعیت حذفی " + System.Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: In Progress']", newStatusName);
            await Page.ClickAsync("button:has-text('ثبت وضعیت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newStatusName }).First;
            await rowLocator.WaitForAsync();

            // Click delete
            await rowLocator.Locator("button[title='حذف']").ClickAsync();

            // Confirm delete
            var modalLocator = Page.Locator("text=آیا از حذف وضعیت").First;
            await modalLocator.WaitForAsync();
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            var deleteToastLocator = Page.Locator("text=حذف شد").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify row is gone
            await rowLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await rowLocator.IsVisibleAsync()).Should().BeFalse();        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var headerLocator = Page.Locator("h1:has-text('مدیریت وضعیت‌ها')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('ثبت وضعیت')");

            var errorLocator = Page.Locator("text=نام وضعیت الزامی است").First;
            await errorLocator.WaitForAsync();
            (await errorLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task StatusesSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            // 1. Create 3 items
            var prefix = "BulkStatus_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for(int i = 1; i <= 3; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: In Progress']", $"{prefix}{i}");
                await Page.FillAsync("input[type='color']", "#ff0000");
                await Page.ClickAsync("button:has-text('ثبت وضعیت')");
                
                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            // 2. Select all 3
            for(int i = 1; i <= 3; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                await row.ScrollIntoViewIfNeededAsync();
                var checkbox = row.Locator("input[type='checkbox']");
                await checkbox.CheckAsync();
                var countLocator = Page.Locator($"div.fixed.bottom-6:has-text('{i} مورد انتخاب شده')").First;
                await countLocator.WaitForAsync(new() { Timeout = 10000 });
            }

            // 3. Delete 1 via individual button
            var rowToDelete = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}1" });
            var deleteBtn = rowToDelete.Locator("button[title='حذف']");
            await deleteBtn.ClickAsync();
            
            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.ClickAsync("button:has-text('بله، حذف کن')");
            
            var deleteToastLocator = Page.Locator("text=وضعیت با موفقیت حذف شد.").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 4. Verify bulk action toolbar indicates "2 مورد انتخاب شده"
            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6:has-text('2 مورد انتخاب شده')").First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 10000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();
            // 5. Click bulk delete for the remaining 2
            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('حذف')");
            var bulkDeleteModal = Page.Locator("text=مطمئن هستید").First;
            await bulkDeleteModal.WaitForAsync(new() { Timeout = 10000 });
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            // 6. Verify plural toast
            var pluralToast = Page.Locator("text=2 وضعیت با موفقیت حذف شدند.").First;
            await pluralToast.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 7. Create 2 more and test activate/deactivate
            for(int i = 4; i <= 5; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: In Progress']", $"{prefix}{i}");
                await Page.FillAsync("input[type='color']", "#ff0000");
                await Page.ClickAsync("button:has-text('ثبت وضعیت')");
                
                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            for(int i = 4; i <= 5; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                await row.ScrollIntoViewIfNeededAsync();
                var checkbox = row.Locator("input[type='checkbox']");
                await checkbox.CheckAsync();
                var countLocator = Page.Locator($"div.fixed.bottom-6:has-text('{i - 3} مورد انتخاب شده')").First;
                await countLocator.WaitForAsync(new() { Timeout = 10000 });
            }

            await Page.ClickAsync("button:has-text('غیرفعال‌سازی')");
            var pluralDeactivateToast = Page.Locator("text=2 وضعیت با موفقیت غیرفعال شدند.").First;
            await pluralDeactivateToast.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            var rowSingular = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}4" });
            await rowSingular.ScrollIntoViewIfNeededAsync();
            var checkboxSingular = rowSingular.Locator("input[type='checkbox']");
            await checkboxSingular.CheckAsync();

            await Page.ClickAsync("button:has-text('فعال‌سازی')");
            var singularActivateToast = Page.Locator("text=1 وضعیت با موفقیت فعال شد.").First;
            await singularActivateToast.WaitForAsync(new() { Timeout = 10000 });
        }

        [Fact]
        public async Task StatusForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/statuses");

            var statusName = "وضعیت لغوی " + System.Guid.NewGuid().ToString().Substring(0, 5);
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

            await Page.WaitForSelectorAsync("h1:has-text('مدیریت وضعیت‌ها')");

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


