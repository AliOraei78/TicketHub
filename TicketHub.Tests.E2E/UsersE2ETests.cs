using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Collections.Generic;

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
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            var newUserName = "کاربر تستی " + Guid.NewGuid().ToString().Substring(0, 5);
            var newUserEmail = $"test_{Guid.NewGuid().ToString().Substring(0, 5)}@example.com";

            try
            {
                var btn = Page.Locator("button:has-text('افزودن کاربر جدید')");
                await btn.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                await btn.ClickAsync();
            }
            catch(TimeoutException)
            {
                var html = await Page.ContentAsync();
                throw new Exception("Button not found. HTML: " + html);
            }
            var modalLocator = Page.Locator("h3:has-text('ایجاد کاربر جدید')").First;
            await modalLocator.WaitForAsync();

            var inputs = Page.Locator("input[type='text']");
            // The name input is after the placeholders for roles/projects in multiselects
            // We can just use the label to fill the right one
            await Page.Locator("label:text-is('نام کاربر')").Locator("..").Locator("input").FillAsync(newUserName);
            await Page.Locator("label:text-is('ایمیل')").Locator("..").Locator("input").FillAsync(newUserEmail);
            await Page.Locator("label:text-is('شماره تلفن')").Locator("..").Locator("input").FillAsync("09121111111");
            await Page.Locator("label:text-is('رمز عبور')").Locator("..").Locator("input").FillAsync("Test@123");

            await Page.ClickAsync("button:has-text('ثبت کاربر')");

            var toastLocator = Page.Locator("text=کاربر جدید ایجاد شد.").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Search for the newly created user
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(newUserName);
            await Task.Delay(1500); // Wait for debounce and reload

            // Verify user row appears in the grid
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newUserName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Edit_User_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            // Create one first to edit
            var newUserName = "کاربر ویرایشی " + Guid.NewGuid().ToString().Substring(0, 5);
            var newUserEmail = $"edit_{Guid.NewGuid().ToString().Substring(0, 5)}@example.com";

            await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
            await Page.Locator("h3:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

            await Page.Locator("label:text-is('نام کاربر')").Locator("..").Locator("input").FillAsync(newUserName);
            await Page.Locator("label:text-is('ایمیل')").Locator("..").Locator("input").FillAsync(newUserEmail);
            await Page.Locator("label:text-is('شماره تلفن')").Locator("..").Locator("input").FillAsync("09121111111");
            await Page.Locator("label:text-is('رمز عبور')").Locator("..").Locator("input").FillAsync("Test@123");
            await Page.ClickAsync("button:has-text('ثبت کاربر')");

            await Page.Locator("text=کاربر جدید ایجاد شد.").First.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Search for the newly created user
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(newUserName);
            await Task.Delay(1500); // Wait for debounce and reload

            var rowLocator = Page.Locator("tr:has(td:has-text('" + newUserName + "'))").First;
            await rowLocator.WaitForAsync();

            // Click edit on this specific row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            // Wait for form to enter edit mode
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش اطلاعات کاربر')");
            var updatedName = newUserName + " ویرایش شده";
            await editHeaderLocator.WaitForAsync();

            await Page.Locator("label:text-is('نام کاربر')").Locator("..").Locator("input").FillAsync(updatedName);
            await Page.Keyboard.PressAsync("Tab"); // Ensure Blazor updates the model before submit
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')"); 

            var editToastLocator = Page.Locator("text=تغییرات کاربر با موفقیت ذخیره شد.").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            var updatedRowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRowLocator.WaitForAsync();
            (await updatedRowLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Delete_User_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            // Create one first to delete
            var newUserName = "کاربر حذفی " + Guid.NewGuid().ToString().Substring(0, 5);
            var newUserEmail = $"delete_{Guid.NewGuid().ToString().Substring(0, 5)}@example.com";

            await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
            await Page.Locator("h3:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

            await Page.Locator("label:text-is('نام کاربر')").Locator("..").Locator("input").FillAsync(newUserName);
            await Page.Locator("label:text-is('ایمیل')").Locator("..").Locator("input").FillAsync(newUserEmail);
            await Page.Locator("label:text-is('شماره تلفن')").Locator("..").Locator("input").FillAsync("09121111111");
            await Page.Locator("label:text-is('رمز عبور')").Locator("..").Locator("input").FillAsync("Test@123");
            await Page.ClickAsync("button:has-text('ثبت کاربر')");

            await Page.Locator("text=کاربر جدید ایجاد شد.").First.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Search for the newly created user to handle pagination
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(newUserName);
            await Task.Delay(1500); // Wait for debounce and reload

            var rowLocator = Page.Locator("tr:has(td:has-text('" + newUserName + "'))").First;
            await rowLocator.WaitForAsync();

            // Click delete on this specific row
            await rowLocator.Locator("button[title='حذف']").ClickAsync();

            var modalLocator = Page.Locator("text=مطمئن هستید؟").First;
            await modalLocator.WaitForAsync();

            // Click confirm
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            var deleteToastLocator = Page.Locator("text=1 کاربر با موفقیت حذف شد.").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify gone
            await rowLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await rowLocator.IsVisibleAsync()).Should().BeFalse();        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/users");

            var headerLocator = Page.Locator("h1:has-text('مدیریت کاربران')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
            await Page.Locator("h3:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

            await Page.ClickAsync("button:has-text('ثبت کاربر')");

            var nameValidation = Page.Locator("text=نام کاربر الزامی است.").First;
            await nameValidation.WaitForAsync(new() { Timeout = 5000 });
            (await nameValidation.IsVisibleAsync()).Should().BeTrue();
            var emailValidation = Page.Locator("text=ایمیل الزامی است.").First;
            (await emailValidation.IsVisibleAsync()).Should().BeTrue();
            var phoneValidation = Page.Locator("text=شماره تماس الزامی است.").First;
            (await phoneValidation.IsVisibleAsync()).Should().BeTrue();
            var passwordValidation = Page.Locator("text=رمز عبور الزامی است.").First;
            (await passwordValidation.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Users_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/users");
            var headerLocator = Page.Locator("h1:has-text('مدیریت کاربران')").First;
            await headerLocator.WaitForAsync();

            // 1. Create 3 users for test
            var prefix = "BulkUser_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for(int i = 1; i <= 3; i++)
            {
                await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
                await Page.Locator("h3:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

                await Page.Locator("label:text-is('نام کاربر')").Locator("..").Locator("input").FillAsync($"{prefix}{i}");
                await Page.Locator("label:text-is('ایمیل')").Locator("..").Locator("input").FillAsync($"test{i}_{prefix}@example.com");
                await Page.Locator("label:text-is('شماره تلفن')").Locator("..").Locator("input").FillAsync("09121111111");
                await Page.Locator("label:text-is('رمز عبور')").Locator("..").Locator("input").FillAsync("Test@123");
                await Page.ClickAsync("button:has-text('ثبت کاربر')");

                var toastLocator = Page.Locator("text=کاربر جدید ایجاد شد.").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                // Dismiss toast
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            // 1.5 Search to ensure they are on the first page
            var searchInput = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInput.FillAsync(prefix);
            await Task.Delay(1500);

            // 2. Select all 3
            for(int i = 1; i <= 3; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                var checkbox = row.Locator("input[type='checkbox']");
                await checkbox.ClickAsync(new() { Force = true });
            }

            // 3. Delete 1 via its individual row action button
            var rowToDelete = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}1" });
            var deleteBtn = rowToDelete.Locator("button[title='حذف']");
            await deleteBtn.ClickAsync();
            
            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync();
            await Page.ClickAsync("button:has-text('بله، حذف کن')");
            
            var deleteToastLocator = Page.Locator("text=1 کاربر با موفقیت حذف شد.").First;
            await deleteToastLocator.WaitForAsync();
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 4. Verify bulk action toolbar indicates "2 مورد انتخاب شده"
            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6:has-text('2 مورد انتخاب شده')").First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 5000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();
            // 5. Click bulk delete for the remaining 2
            await Page.ClickAsync("button:has-text('حذف')");
            var bulkDeleteModal = Page.Locator("text=مطمئن هستید؟").First;
            await bulkDeleteModal.WaitForAsync();
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            // 6. Verify plural toast
            var pluralToast = Page.Locator("text=2 کاربر با موفقیت حذف شدند.").First;
            await pluralToast.WaitForAsync();
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 7. Create 2 more and test activate/deactivate
            for(int i = 4; i <= 5; i++)
            {
                await Page.ClickAsync("button:has-text('افزودن کاربر جدید')");
                await Page.Locator("h3:has-text('ایجاد کاربر جدید')").First.WaitForAsync();

                await Page.Locator("label:text-is('نام کاربر')").Locator("..").Locator("input").FillAsync($"{prefix}{i}");
                await Page.Locator("label:text-is('ایمیل')").Locator("..").Locator("input").FillAsync($"test{i}_{prefix}@example.com");
                await Page.Locator("label:text-is('شماره تلفن')").Locator("..").Locator("input").FillAsync("09121111111");
                await Page.Locator("label:text-is('رمز عبور')").Locator("..").Locator("input").FillAsync("Test@123");
                await Page.ClickAsync("button:has-text('ثبت کاربر')");

                var toastLocator = Page.Locator("text=کاربر جدید ایجاد شد.").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            // Search to ensure new users are visible
            var searchInputSingular = Page.Locator("input[placeholder='جستجوی نام یا ایمیل...']");
            await searchInputSingular.FillAsync(prefix);
            await Task.Delay(1500);

            for(int i = 4; i <= 5; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                var checkbox = row.Locator("input[type='checkbox']");
                await checkbox.ClickAsync(new() { Force = true });
            }

            // Plural Deactivate
            await Page.ClickAsync("button:has-text('غیرفعال‌سازی')");
            var pluralDeactivateToast = Page.Locator("text=2 کاربر با موفقیت غیرفعال شدند.").First;
            await pluralDeactivateToast.WaitForAsync();
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Verify deactivated status in table
            for(int i = 4; i <= 5; i++)
            {
                var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}{i}" });
                var innerText = await row.InnerTextAsync();
                innerText.Should().Contain("غیرفعال");            }


            // Select 1 and test singular
            // Checkboxes were cleared by the previous action. We select 4.
            var rowToSelect = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}4" });
            var checkboxToSelect = rowToSelect.Locator("input[type='checkbox']");
            await checkboxToSelect.ClickAsync(new() { Force = true });

            var selectedCountIndicatorFinal = Page.Locator("div.fixed.bottom-6:has-text('1 مورد انتخاب شده')").First;
            await selectedCountIndicatorFinal.WaitForAsync(new() { Timeout = 5000 });

            await Page.ClickAsync("button:has-text('فعال‌سازی')");
            var singularActivateToast = Page.Locator("text=1 کاربر با موفقیت فعال شد.").First;
            await singularActivateToast.WaitForAsync();
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
            await Page.Locator("label:text-is('نام کاربر')").Locator("..").Locator("input").FillAsync(cancelledName);

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
            await Page.ClickAsync("button:has-text('کاربران فعال')");
            await Task.Delay(800);

            // Verify active pill styling exists
            var tableText = await Page.Locator("table").InnerTextAsync();
            tableText.Should().NotBeNull();

            // Click Inactive filter
            await Page.ClickAsync("button:has-text('غیرفعال')");
            await Task.Delay(800);

            // Click All filter
            await Page.ClickAsync("button:has-text('همه')");
            await Task.Delay(800);
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

            await Page.Locator("label:text-is('نام کاربر')").Locator("..").Locator("input").FillAsync(newUserName);
            await Page.Locator("label:text-is('ایمیل')").Locator("..").Locator("input").FillAsync(newUserEmail);
            await Page.Locator("label:text-is('شماره تلفن')").Locator("..").Locator("input").FillAsync("09121111111");
            await Page.Locator("label:text-is('رمز عبور')").Locator("..").Locator("input").FillAsync("Test@123");

            // Select role in MultiSelectDropdown
            var roleDropdownTrigger = Page.Locator("label:has-text('نقش‌های دسترسی')").Locator("..").Locator("input").First;
            if (await roleDropdownTrigger.IsVisibleAsync())
            {
                await roleDropdownTrigger.ClickAsync();
                await Task.Delay(500);
                var roleOption = Page.Locator("div.cursor-pointer").Filter(new() { HasText = "ادمین" }).Or(Page.Locator("div.cursor-pointer").Filter(new() { HasText = "مدیر" })).First;
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
            await Task.Delay(1500);

            var row = Page.Locator("tr", new PageLocatorOptions { HasTextString = newUserName }).First;
            await row.WaitForAsync();
            (await row.IsVisibleAsync()).Should().BeTrue();
        }
    }
}


