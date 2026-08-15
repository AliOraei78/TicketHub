using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class PermissionsE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public PermissionsE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Create_Permission_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/permissions");

            var newPermName = "دسترسی تستی " + Guid.NewGuid().ToString().Substring(0, 5);
            var resourceKey = "test.resource." + Guid.NewGuid().ToString().Substring(0, 5);

            await Page.FillAsync("input[placeholder='مثال: مدیریت کاربران']", newPermName);
            await Page.FillAsync("input[placeholder='مثال: Users.Manage']", resourceKey);

            // Select Permission Type (SlideSelect)
            await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع دسترسی')");
            await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('منو')");

            await Page.ClickAsync("button:has-text('ثبت دسترسی')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify permission row appears in the grid
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newPermName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Edit_Permission_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/permissions");

            var newPermName = "دسترسی ویرایشی " + Guid.NewGuid().ToString().Substring(0, 5);
            var resourceKey = "edit.resource." + Guid.NewGuid().ToString().Substring(0, 5);
            
            await Page.FillAsync("input[placeholder='مثال: مدیریت کاربران']", newPermName);
            await Page.FillAsync("input[placeholder='مثال: Users.Manage']", resourceKey);

            await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع دسترسی')");
            await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('منو')");

            await Page.ClickAsync("button:has-text('ثبت دسترسی')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newPermName }).First;
            await rowLocator.WaitForAsync();

            // Click edit on this specific row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش دسترسی')");
            await editHeaderLocator.WaitForAsync();

            var updatedName = newPermName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='مثال: مدیریت کاربران']", updatedName);
            await Page.Keyboard.PressAsync("Tab");
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')");

            var editToastLocator = Page.Locator("text=ویرایش شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            var updatedRowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRowLocator.WaitForAsync();
            (await updatedRowLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Delete_Permission_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/permissions");

            var newPermName = "دسترسی حذفی " + Guid.NewGuid().ToString().Substring(0, 5);
            var resourceKey = "delete.resource." + Guid.NewGuid().ToString().Substring(0, 5);

            await Page.FillAsync("input[placeholder='مثال: مدیریت کاربران']", newPermName);
            await Page.FillAsync("input[placeholder='مثال: Users.Manage']", resourceKey);

            await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع دسترسی')");
            await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('کامل')");

            await Page.ClickAsync("button:has-text('ثبت دسترسی')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newPermName }).First;
            await rowLocator.WaitForAsync();

            // Click delete on this specific row
            await rowLocator.Locator("button[title='حذف']").ClickAsync();

            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync();

            // Click confirm
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            var deleteToastLocator = Page.Locator("text=حذف شد").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify gone
            await rowLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await rowLocator.IsVisibleAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/permissions");

            var headerLocator = Page.Locator("h1:has-text('مدیریت دسترسی')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('ثبت دسترسی')");

            var errorLocator1 = Page.Locator("text=عنوان دسترسی الزامی است").First;
            await errorLocator1.WaitForAsync();
            (await errorLocator1.IsVisibleAsync()).Should().BeTrue();
            var errorLocator2 = Page.Locator("text=کلید منبع (ResourceKey) الزامی است").First;
            await errorLocator2.WaitForAsync();
            (await errorLocator2.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task PermissionsSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/permissions");

            var prefix = "BulkPerm_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for(int i = 1; i <= 3; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: مدیریت کاربران']", $"{prefix}{i}");
                await Page.FillAsync("input[placeholder='مثال: Users.Manage']", $"test.res.{Guid.NewGuid().ToString().Substring(0, 5)}");
                await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع دسترسی')");
                await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('منو')");
                await Page.ClickAsync("button:has-text('ثبت دسترسی')");
                
                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            for(int i = 1; i <= 3; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                await row.ScrollIntoViewIfNeededAsync();
                await row.Locator("label.cyber-checkbox-container input, input[type='checkbox']").First.ClickAsync(new() { Force = true });
                await Page.WaitForTimeoutAsync(300);
            }
            var countLocator = Page.Locator("div.fixed.bottom-6:has-text('مورد انتخاب شده')").First;
            await countLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.WaitForTimeoutAsync(400);

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

            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6:has-text('مورد انتخاب شده')").First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 10000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();
            
            await Page.Locator("div.fixed.bottom-6 button:has-text('حذف گروهی')").First.ClickAsync(new() { Force = true });
            var bulkDeleteModal = Page.Locator("text=دسترسی انتخاب شده مطمئن هستید").First;
            await bulkDeleteModal.WaitForAsync(new() { Timeout = 10000 });
            
            var modalConfirm2 = Page.Locator("button:has-text('بله، حذف کن')").First;
            await modalConfirm2.ClickAsync(new() { Force = true });

            var pluralToast = Page.Locator("text=با موفقیت حذف").Last;
            await pluralToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(600);

            // Test Activate/Deactivate
            for(int i = 4; i <= 5; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: مدیریت کاربران']", $"{prefix}{i}");
                await Page.FillAsync("input[placeholder='مثال: Users.Manage']", $"test.res.{Guid.NewGuid().ToString().Substring(0, 5)}");
                await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع دسترسی')");
                await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('منو')");
                await Page.ClickAsync("button:has-text('ثبت دسترسی')");
                
                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            for(int i = 4; i <= 5; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                await row.ScrollIntoViewIfNeededAsync();
                await row.Locator("label.cyber-checkbox-container input, input[type='checkbox']").First.ClickAsync(new() { Force = true });
                await Page.WaitForTimeoutAsync(300);
            }
            var bulkBar2 = Page.Locator("div.fixed.bottom-6:has-text('مورد انتخاب شده')").First;
            await bulkBar2.WaitForAsync(new() { Timeout = 10000 });
            await Page.WaitForTimeoutAsync(400);

            await Page.Locator("div.fixed.bottom-6 button:has-text('غیرفعال‌سازی')").First.ClickAsync(new() { Force = true });
            var pluralDeactivateToast = Page.Locator("text=با موفقیت غیر").Last;
            await pluralDeactivateToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(600);

            var rowSingular = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}4" });
            await rowSingular.ScrollIntoViewIfNeededAsync();
            await rowSingular.Locator("label.cyber-checkbox-container input, input[type='checkbox']").First.ClickAsync(new() { Force = true });
            await Page.WaitForTimeoutAsync(300);

            await Page.Locator("div.fixed.bottom-6 button:has-text('فعال‌سازی')").First.ClickAsync(new() { Force = true });
            var singularActivateToast = Page.Locator("text=با موفقیت فعال").Last;
            await singularActivateToast.WaitForAsync(new() { Timeout = 15000 });
        }

        [Fact]
        public async Task PermissionForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/permissions");

            var permName = "دسترسی لغوی " + Guid.NewGuid().ToString().Substring(0, 5);
            var resKey = "test.cancel." + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: مدیریت کاربران']", permName);
            await Page.FillAsync("input[placeholder='مثال: Users.Manage']", resKey);
            await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع دسترسی')");
            await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('منو')");
            await Page.ClickAsync("button:has-text('ثبت دسترسی')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Click edit
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = permName });
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            var editHeader = Page.Locator("h3:has-text('ویرایش دسترسی')").First;
            await editHeader.WaitForAsync();

            // Click cancel
            await Page.ClickAsync("button:has-text('انصراف')");

            var createHeader = Page.Locator("h3:has-text('ایجاد دسترسی جدید')").First;
            await createHeader.WaitForAsync();
            (await createHeader.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task PermissionsSettings_3StateStatusFilter_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/permissions");

            await Page.WaitForSelectorAsync("h1:has-text('مدیریت دسترسی')");

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


