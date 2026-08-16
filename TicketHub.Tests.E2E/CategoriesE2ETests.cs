using Microsoft.Playwright;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using System;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class CategoriesE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public CategoriesE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CategoriesSettings_CRUD_HappyPath()
        {
            await Page.GotoAsync(Factory.ServerAddress + "/dev/login");
            await Page.GotoAsync(Factory.ServerAddress + "/settings/categories");

            // 1. Verify Page Loaded
            var headerLocator = Page.Locator("h1:has-text('مدیریت انواع تیکت')").First;
            await headerLocator.WaitForAsync();
            (await headerLocator.IsVisibleAsync()).Should().BeTrue();

            // 2. Create Category
            var newCategoryName = "تیکت تست E2E " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: پشتیبانی فنی']", newCategoryName);

            await Page.ClickAsync("button:has-text('ثبت نوع تیکت')");

            // Wait for success toast
            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });
            (await toastLocator.IsVisibleAsync()).Should().BeTrue();

            // Verify the new category is in the grid
            var newCatRow = Page.Locator($"text={newCategoryName}");
            await newCatRow.WaitForAsync();
            (await newCatRow.IsVisibleAsync()).Should().BeTrue();

            // 3. Edit Category
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newCategoryName });
            var editBtn = rowLocator.Locator("button[title='ویرایش']");
            await editBtn.ClickAsync();

            // Wait for form to enter edit mode
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش نوع تیکت')");
            await editHeaderLocator.WaitForAsync();

            var updatedName = newCategoryName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='مثال: پشتیبانی فنی']", updatedName);
            await Page.Keyboard.PressAsync("Tab");
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')");

            // Wait for success toast
            var editToastLocator = Page.Locator("text=ویرایش شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });
            (await editToastLocator.IsVisibleAsync()).Should().BeTrue();
            var updatedCatRow = Page.Locator($"text={updatedName}");
            await updatedCatRow.WaitForAsync();
            (await updatedCatRow.IsVisibleAsync()).Should().BeTrue();

            // 4. Delete Category
            var updatedRowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName });
            var deleteBtn = updatedRowLocator.Locator("button[title='حذف']");
            await deleteBtn.ClickAsync();

            // Modal should appear
            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync();
            (await modalLocator.IsVisibleAsync()).Should().BeTrue();
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            // Wait for success toast
            var deleteToastLocator = Page.Locator("text=حذف شد").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });
            (await deleteToastLocator.IsVisibleAsync()).Should().BeTrue();

            // Verify it's gone
            await updatedCatRow.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await updatedCatRow.IsVisibleAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync(Factory.ServerAddress + "/dev/login");
            await Page.GotoAsync(Factory.ServerAddress + "/settings/categories");

            var headerLocator = Page.Locator("h1:has-text('مدیریت انواع تیکت')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('ثبت نوع تیکت')");

            var validationMsg = Page.Locator("text=نام دسته‌بندی الزامی است").First;
            await validationMsg.WaitForAsync(new() { Timeout = 5000 });
            (await validationMsg.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task CategoriesSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync(Factory.ServerAddress + "/dev/login");
            await Page.GotoAsync(Factory.ServerAddress + "/settings/categories");
            var headerLocator = Page.Locator("h1:has-text('مدیریت انواع تیکت')").First;
            await headerLocator.WaitForAsync();

            // 1. Create 3 categories for test
            var prefix = "BulkCat_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for (int i = 1; i <= 3; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: پشتیبانی فنی']", $"{prefix}{i}");
                await Page.ClickAsync("button:has-text('ثبت نوع تیکت')");
                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            // 2. Select all 3
            var searchInput = Page.Locator("input[placeholder='جستجوی دسته‌بندی...']");
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

            // 3. Delete 1 via its individual row action button
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

            // 5. Click bulk delete for the remaining 2
            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('حذف گروهی')");

            var bulkDeleteModalText = Page.Locator("text=نوع تیکت انتخاب شده مطمئن هستید").First;
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
                await Page.FillAsync("input[placeholder='مثال: پشتیبانی فنی']", $"{prefix}{i}");
                await Page.ClickAsync("button:has-text('ثبت نوع تیکت')");
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

            // Plural Deactivate
            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('غیرفعال‌سازی')");
            var pluralDeactivateToast = Page.Locator("text=با موفقیت غیرفعال").Last;
            await pluralDeactivateToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(600);

            // Select 1 and test singular
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
        public async Task CategoryForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync(Factory.ServerAddress + "/dev/login");
            await Page.GotoAsync(Factory.ServerAddress + "/settings/categories");

            var categoryName = "نوع تیکت لغوی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: پشتیبانی فنی']", categoryName);
            await Page.ClickAsync("button:has-text('ثبت نوع تیکت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Click edit
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = categoryName });
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            var editHeader = Page.Locator("h3:has-text('ویرایش نوع تیکت')").First;
            await editHeader.WaitForAsync();

            // Click cancel
            await Page.ClickAsync("button:has-text('انصراف')");

            var createHeader = Page.Locator("h3:has-text('ایجاد نوع تیکت جدید')").First;
            await createHeader.WaitForAsync();
            (await createHeader.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task CategoriesSettings_3StateStatusFilter_Scenario()
        {
            await Page.GotoAsync(Factory.ServerAddress + "/dev/login");
            await Page.GotoAsync(Factory.ServerAddress + "/settings/categories");

            await Page.WaitForSelectorAsync("h1:has-text('مدیریت انواع تیکت')");

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

