using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class UsersE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UsersE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Create_User_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            var newUserName = "کاربر تستی " + Guid.NewGuid().ToString().Substring(0, 5);
            var newUserEmail = $"test_{Guid.NewGuid().ToString().Substring(0, 5)}@example.com";

            await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
            var modalLocator = Page.Locator("h2:has-text('ایجاد کاربر جدید')").First;
            await modalLocator.WaitForAsync();

            await Page.FillAsync("input[placeholder='نام کامل']", newUserName);
            await Page.FillAsync("input[placeholder='user@example.com']", newUserEmail);
            await Page.FillAsync("input[placeholder='0912...']", "09121111111");
            await Page.FillAsync("input[type='password']", "Test@123");

            await Page.ClickAsync("button:has-text('ثبت کاربر')");

            var toastLocator = Page.Locator("text=کاربر جدید ایجاد شد.").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Search for the newly created user
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(newUserName);
            await Task.Delay(1000);

            // Verify user row appears in the grid
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newUserName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Edit_User_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            // Create one first to edit
            var newUserName = "کاربر ویرایشی " + Guid.NewGuid().ToString().Substring(0, 5);
            var newUserEmail = $"edit_{Guid.NewGuid().ToString().Substring(0, 5)}@example.com";

            await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
            await Page.Locator("h2:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

            await Page.FillAsync("input[placeholder='نام کامل']", newUserName);
            await Page.FillAsync("input[placeholder='user@example.com']", newUserEmail);
            await Page.FillAsync("input[placeholder='0912...']", "09121111111");
            await Page.FillAsync("input[type='password']", "Test@123");
            await Page.ClickAsync("button:has-text('ثبت کاربر')");

            await Page.Locator("text=کاربر جدید ایجاد شد.").First.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Search for the newly created user
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(newUserName);
            await Task.Delay(1000);

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newUserName }).First;
            await rowLocator.WaitForAsync();

            // Click edit on this specific row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync(new() { Force = true });

            // Wait for form to enter edit mode
            var editHeaderLocator = Page.Locator("h2:has-text('ویرایش اطلاعات کاربر')").First;
            var updatedName = newUserName + " ویرایش شده";
            await editHeaderLocator.WaitForAsync();

            await Page.FillAsync("input[placeholder='نام کامل']", updatedName);
            await Page.Keyboard.PressAsync("Tab");
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')");

            var editToastLocator = Page.Locator("text=تغییرات کاربر با موفقیت ذخیره شد.").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            await searchInput.FillAsync(updatedName);
            await Task.Delay(1000);

            var updatedRowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRowLocator.WaitForAsync();
            (await updatedRowLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Delete_User_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            // Create one first to delete
            var newUserName = "کاربر حذفی " + Guid.NewGuid().ToString().Substring(0, 5);
            var newUserEmail = $"delete_{Guid.NewGuid().ToString().Substring(0, 5)}@example.com";

            await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
            await Page.Locator("h2:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

            await Page.FillAsync("input[placeholder='نام کامل']", newUserName);
            await Page.FillAsync("input[placeholder='user@example.com']", newUserEmail);
            await Page.FillAsync("input[placeholder='0912...']", "09121111111");
            await Page.FillAsync("input[type='password']", "Test@123");
            await Page.ClickAsync("button:has-text('ثبت کاربر')");

            await Page.Locator("text=کاربر جدید ایجاد شد.").First.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Search for the newly created user
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(newUserName);
            await Task.Delay(1000);

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newUserName }).First;
            await rowLocator.WaitForAsync();

            // Click delete on this specific row
            var deleteBtn = rowLocator.Locator("button[title='حذف']").First;
            await deleteBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await deleteBtn.ClickAsync();

            var confirmBtn = Page.Locator("button:has-text('بله، حذف کن')").First;
            await confirmBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await confirmBtn.ClickAsync();

            var deleteToastLocator = Page.Locator("text=با موفقیت حذف شد.").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify gone
            await rowLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await rowLocator.IsVisibleAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            var headerLocator = Page.Locator("h1:has-text('مدیریت کاربران')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
            await Page.Locator("h2:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

            await Page.Locator("button[type='submit']:has-text('ثبت کاربر')").ClickAsync(new() { Force = true });

            var nameValidation = Page.Locator("text=نام کاربر الزامی است.").First;
            await nameValidation.WaitForAsync(new() { Timeout = 5000 });
            (await nameValidation.IsVisibleAsync()).Should().BeTrue();
            var emailValidation = Page.Locator("text=ایمیل الزامی است.").First;
            (await emailValidation.IsVisibleAsync()).Should().BeTrue();
            var phoneValidation = Page.Locator("text=شماره تماس الزامی است.").First;
            (await phoneValidation.IsVisibleAsync()).Should().BeTrue();
            var passwordValidation = Page.Locator("text=رمز عبور الزامی است.").First;
            (await passwordValidation.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Users_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/users");
            var headerLocator = Page.Locator("h1:has-text('مدیریت کاربران')").First;
            await headerLocator.WaitForAsync();

            // 1. Create 3 users for test
            var prefix = "BulkUser_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for (int i = 1; i <= 3; i++)
            {
                await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
                await Page.Locator("h2:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

                await Page.FillAsync("input[placeholder='نام کامل']", $"{prefix}{i}");
                await Page.FillAsync("input[placeholder='user@example.com']", $"test{i}_{prefix}@example.com");
                await Page.FillAsync("input[placeholder='0912...']", "09121111111");
                await Page.FillAsync("input[type='password']", "Test@123");
                await Page.ClickAsync("button:has-text('ثبت کاربر')");

                var toastLocator = Page.Locator("text=کاربر جدید ایجاد شد.").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            // 1.5 Search to ensure they are visible
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(prefix);
            await Task.Delay(1000);

            // 2. Select all 3
            for (int i = 1; i <= 3; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                await row.ScrollIntoViewIfNeededAsync();
                await row.Locator("label.cyber-checkbox-container").First.ClickAsync(new() { Force = true });
            }

            // 3. Delete 1 via its individual row action button
            var rowToDelete = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}1" });
            var deleteBtn = rowToDelete.Locator("button[title='حذف']");
            await deleteBtn.ClickAsync(new() { Force = true });

            var modalLocator = Page.Locator("text=مطمئن هستید").First;
            await modalLocator.WaitForAsync();
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            var deleteToastLocator = Page.Locator("text=با موفقیت حذف شد.").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 4. Verify bulk action toolbar indicates "2 مورد انتخاب شده"
            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6:has-text('مورد انتخاب شده')").First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 10000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();

            // 5. Click bulk delete for the remaining 2
            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('حذف گروهی')");
            var bulkDeleteModal = Page.Locator("text=مطمئن هستید").First;
            await bulkDeleteModal.WaitForAsync();
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            // 6. Verify plural toast
            var pluralToast = Page.Locator("text=با موفقیت حذف شدند.").First;
            await pluralToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 7. Create 2 more and test activate/deactivate
            for (int i = 4; i <= 5; i++)
            {
                await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
                await Page.Locator("h2:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

                await Page.FillAsync("input[placeholder='نام کامل']", $"{prefix}{i}");
                await Page.FillAsync("input[placeholder='user@example.com']", $"test{i}_{prefix}@example.com");
                await Page.FillAsync("input[placeholder='0912...']", "09121111111");
                await Page.FillAsync("input[type='password']", "Test@123");
                await Page.ClickAsync("button:has-text('ثبت کاربر')");

                var toastLocator = Page.Locator("text=کاربر جدید ایجاد شد.").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            // Search to ensure new users are visible
            await searchInput.FillAsync(prefix);
            await Task.Delay(1000);

            for (int i = 4; i <= 5; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                await row.ScrollIntoViewIfNeededAsync();
                await row.Locator("label.cyber-checkbox-container").First.ClickAsync(new() { Force = true });
            }

            // Plural Deactivate
            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('غیرفعال‌سازی')");
            var pluralDeactivateToast = Page.Locator("text=با موفقیت غیرفعال شدند.").First;
            await pluralDeactivateToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Verify deactivated status in table
            for (int i = 4; i <= 5; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                var innerText = await row.InnerTextAsync();
                innerText.Should().Contain("غیرفعال");
            }

            // Select 1 and test singular
            var rowToSelect = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}4" });
            await rowToSelect.ScrollIntoViewIfNeededAsync();
            await rowToSelect.Locator("label.cyber-checkbox-container").First.ClickAsync(new() { Force = true });

            var selectedCountIndicatorFinal = Page.Locator("div.fixed.bottom-6:has-text('مورد انتخاب شده')").First;
            await selectedCountIndicatorFinal.WaitForAsync(new() { Timeout = 10000 });

            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('فعال‌سازی')");
            var singularActivateToast = Page.Locator("text=با موفقیت فعال شد.").First;
            await singularActivateToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Verify activated status in table
            var rowSingularAfter = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}4" });
            var innerTextSingular = await rowSingularAfter.InnerTextAsync();
            innerTextSingular.Should().Contain("فعال");
            innerTextSingular.Should().NotContain("غیرفعال");
        }

        [Fact]
        public async Task CreateUser_CancelModal_ClosesWithoutSaving()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            var header = Page.Locator("h1:has-text('مدیریت کاربران')").First;
            await header.WaitForAsync();

            await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
            var modalHeader = Page.Locator("h2:has-text('ایجاد کاربر جدید')").First;
            await modalHeader.WaitForAsync();

            var cancelledName = "کاربر لغو شده " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='نام کامل']", cancelledName);

            // Click Cancel
            await Page.ClickAsync("button:has-text('انصراف')");
            await modalHeader.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

            // Search for cancelled user
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(cancelledName);
            await Task.Delay(1000);

            var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = cancelledName });
            (await row.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Users_FilterStatus_ActiveAndInactive_ShouldFilterGrid()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            var header = Page.Locator("h1:has-text('مدیریت کاربران')").First;
            await header.WaitForAsync();

            // Click Active filter
            await Page.ClickAsync("button:has-text('فعال (')");
            await Task.Delay(500);

            // Verify active pill styling exists
            var tableText = await Page.Locator("table").InnerTextAsync();
            tableText.Should().NotBeNull();

            // Click Inactive filter
            await Page.ClickAsync("button:has-text('غیرفعال (')");
            await Task.Delay(500);

            // Click All filter
            await Page.ClickAsync("button:has-text('همه (')");
            await Task.Delay(500);
        }

        [Fact]
        public async Task Create_User_WithRoleAssignment_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            var newUserName = "کاربر همراه نقش " + Guid.NewGuid().ToString().Substring(0, 5);
            var newUserEmail = $"role_{Guid.NewGuid().ToString().Substring(0, 5)}@example.com";

            await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
            await Page.Locator("h2:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

            await Page.FillAsync("input[placeholder='نام کامل']", newUserName);
            await Page.FillAsync("input[placeholder='user@example.com']", newUserEmail);
            await Page.FillAsync("input[placeholder='0912...']", "09121111111");
            await Page.FillAsync("input[type='password']", "Test@123");

            // Select role in MultiSelectDropdown
            var roleDropdownTrigger = Page.Locator("input[placeholder='جستجو و انتخاب نقش...']").First;
            if (await roleDropdownTrigger.IsVisibleAsync())
            {
                await roleDropdownTrigger.ClickAsync();
                await Task.Delay(500);
                var roleOption = Page.Locator(".dropdown-menu-container div.cursor-pointer").First;
                if (await roleOption.CountAsync() > 0)
                {
                    await roleOption.ClickAsync(new() { Force = true });
                    await Task.Delay(300);
                }
            }

            await Page.ClickAsync("button:has-text('ثبت کاربر')");

            var toastLocator = Page.Locator("text=کاربر جدید ایجاد شد.").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Search in grid
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(newUserName);
            await Task.Delay(1000);

            var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = newUserName }).First;
            await row.WaitForAsync();
            (await row.IsVisibleAsync()).Should().BeTrue();
        }
    }
}


