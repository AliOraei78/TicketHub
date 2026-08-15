using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;
using System;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class RolesE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public RolesE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Create_Role_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/roles");

            var newRoleName = "نقش جدید " + Guid.NewGuid().ToString().Substring(0, 5);

            // Fill the form on the left pane
            await Page.FillAsync("input[placeholder='مثال: کارشناس پشتیبانی فنی']", newRoleName);
            await Page.ClickAsync("button:has-text('ثبت نقش')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify role row appears in the grid
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newRoleName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Edit_Role_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/roles");

            // Create one first to edit
            var newRoleName = "نقش جدید " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: کارشناس پشتیبانی فنی']", newRoleName);
            await Page.ClickAsync("button:has-text('ثبت نقش')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr:has(td:has-text('" + newRoleName + "'))").First;
            await rowLocator.WaitForAsync();

            // Click edit on this specific row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            // Wait for form to enter edit mode to prevent Blazor overwriting our input
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش نقش')");
            await editHeaderLocator.WaitForAsync();

            var updatedName = newRoleName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='مثال: کارشناس پشتیبانی فنی']", updatedName);
            await Page.Keyboard.PressAsync("Tab"); // Ensure Blazor updates the model before submit
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')"); // Button text changes to '????? ???????' when editing

            var editToastLocator = Page.Locator("text=ویرایش شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            var updatedRowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRowLocator.WaitForAsync();
            (await updatedRowLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Delete_Role_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/roles");

            // Create one first to delete
            var newRoleName = "نقش جدید " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: کارشناس پشتیبانی فنی']", newRoleName);
            await Page.ClickAsync("button:has-text('ثبت نقش')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr:has(td:has-text('" + newRoleName + "'))").First;
            await rowLocator.WaitForAsync();

            // Click delete on this specific row
            await rowLocator.Locator("button[title='حذف']").ClickAsync();

            var modalLocator = Page.Locator("text=حذف اطلاعات").First;
            await modalLocator.WaitForAsync();

            // Click confirm
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            var deleteToastLocator = Page.Locator("text=حذف شد").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify gone
            await rowLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await rowLocator.IsVisibleAsync()).Should().BeFalse();        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/roles");

            var headerLocator = Page.Locator("h1:has-text('مدیریت نقش‌های سیستم')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('ثبت نقش')");

            var errorLocator = Page.Locator("text=نام نقش الزامی است").First;
            await errorLocator.WaitForAsync();
            (await errorLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task RolesSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/roles");

            var prefix = "BulkRole_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for(int i = 1; i <= 3; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: کارشناس پشتیبانی فنی']", $"{prefix}{i}");
                await Page.ClickAsync("button:has-text('ثبت نقش')");
                
                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            for(int i = 1; i <= 3; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                await row.ScrollIntoViewIfNeededAsync();
                var checkbox = row.Locator("input[type='checkbox']");
                await checkbox.CheckAsync();
                var countLocator = Page.Locator($"div.fixed.bottom-6:has-text('{i} مورد انتخاب شده')").First;
                await countLocator.WaitForAsync(new() { Timeout = 10000 });
            }

            var rowToDelete = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}1" });
            var deleteBtn = rowToDelete.Locator("button[title='حذف']");
            await deleteBtn.ClickAsync();
            
            var modalLocator = Page.Locator("text=حذف اطلاعات").First;
            await modalLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.ClickAsync("button:has-text('بله، حذف کن')");
            
            var deleteToastLocator = Page.Locator("text=نقش با موفقیت حذف شد.").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6:has-text('2 مورد انتخاب شده')").First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 10000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();
            await Page.ClickAsync("button:has-text('حذف')");
            var bulkDeleteModal = Page.Locator("text=حذف اطلاعات").First;
            await bulkDeleteModal.WaitForAsync(new() { Timeout = 10000 });
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            var pluralToast = Page.Locator("text=2 نقش با موفقیت حذف شدند.").First;
            await pluralToast.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Test Activate/Deactivate
            for(int i = 4; i <= 5; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: کارشناس پشتیبانی فنی']", $"{prefix}{i}");
                await Page.ClickAsync("button:has-text('ثبت نقش')");
                
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
            var pluralDeactivateToast = Page.Locator("text=2 نقش با موفقیت غیرفعال شدند.").First;
            await pluralDeactivateToast.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            var rowSingular = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}4" });
            await rowSingular.ScrollIntoViewIfNeededAsync();
            var checkboxSingular = rowSingular.Locator("input[type='checkbox']");
            await checkboxSingular.CheckAsync();

            await Page.ClickAsync("button:has-text('فعال‌سازی')");
            var singularActivateToast = Page.Locator("text=1 نقش با موفقیت فعال شد.").First;
            await singularActivateToast.WaitForAsync(new() { Timeout = 10000 });
        }

        [Fact]
        public async Task RoleForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/roles");

            var roleName = "نقش لغوی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: کارشناس پشتیبانی فنی']", roleName);
            await Page.ClickAsync("button:has-text('ثبت نقش')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Click edit
            var rowLocator = Page.Locator("tr:has(td:has-text('" + roleName + "'))").First;
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            var editHeader = Page.Locator("h3:has-text('ویرایش نقش')").First;
            await editHeader.WaitForAsync();

            // Click cancel
            await Page.ClickAsync("button:has-text('انصراف')");

            var createHeader = Page.Locator("h3:has-text('تعریف نقش جدید')").First;
            await createHeader.WaitForAsync();
            (await createHeader.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task RolesSettings_3StateStatusFilter_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/roles");

            await Page.WaitForSelectorAsync("h1:has-text('مدیریت نقش‌های سیستم')");

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

