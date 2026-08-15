using Microsoft.Playwright;
using Xunit;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
            await Page.GotoAsync(Factory.ServerAddress + "/settings/categories");

            // 1. Verify Page Loaded
            var headerLocator = Page.Locator("h1:has-text('مدیریت انواع تیکت')").First;
            await headerLocator.WaitForAsync();
            (await headerLocator.IsVisibleAsync()).Should().BeTrue();
            // 2. Create Category
            var newCategoryName = "تیکت تست E2E " + System.Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: پشتیبانی فنی']", newCategoryName);
            await Page.CheckAsync("input[id='categoryIsActive']");
            
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
            // Assuming the first edit button is the one for the new category (or find row specifically)
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newCategoryName });
            var editBtn = rowLocator.Locator("button[title='ویرایش']");
            await editBtn.ClickAsync();

            // Wait for form to enter edit mode to prevent Blazor overwriting our input
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش نوع تیکت')");
            await editHeaderLocator.WaitForAsync();

            var updatedName = newCategoryName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='مثال: پشتیبانی فنی']", updatedName);
            await Page.Keyboard.PressAsync("Tab"); // Ensure Blazor updates the model
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
            (await updatedCatRow.IsVisibleAsync()).Should().BeFalse();    }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync(Factory.ServerAddress + "/settings/categories");

            var headerLocator = Page.Locator("h1:has-text('مدیریت انواع تیکت')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('ثبت نوع تیکت')");

            var validationMsg = Page.Locator("text=نام دسته‌بندی الزامی است").First;
            await validationMsg.WaitForAsync(new() { Timeout = 5000 });
            (await validationMsg.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task CategoriesSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync(Factory.ServerAddress + "/settings/categories");
            var headerLocator = Page.Locator("h1:has-text('مدیریت انواع تیکت')").First;
            await headerLocator.WaitForAsync();

            // 1. Create 3 categories for test
            var prefix = "BulkCat_" + System.Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for(int i = 1; i <= 3; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: پشتیبانی فنی']", $"{prefix}{i}");
                await Page.ClickAsync("button:has-text('ثبت نوع تیکت')");
                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                // Dismiss toast
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

            // 3. Delete 1 via its individual row action button
            var rowToDelete = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}1" });
            var deleteBtn = rowToDelete.Locator("button[title='حذف']");
            await deleteBtn.ClickAsync();
            
            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.ClickAsync("button:has-text('بله، حذف کن')");
            
            var deleteToastLocator = Page.Locator("text=حذف شد").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 4. Verify bulk action toolbar indicates "2 مورد انتخاب شده"
            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6:has-text('2 مورد انتخاب شده')").First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 10000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();
            // 5. Click bulk delete for the remaining 2
            await Page.ClickAsync("button:has-text('حذف')");
            var bulkDeleteModal = Page.Locator("text=آیا از حذف 2 نوع تیکت انتخاب شده مطمئن هستید؟").First;
            await bulkDeleteModal.WaitForAsync(new() { Timeout = 10000 });
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            // 6. Verify plural toast
            var pluralToast = Page.Locator("text=2 نوع تیکت با موفقیت حذف شدند").First;
            await pluralToast.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 7. Create 2 more and test activate/deactivate
            for(int i = 4; i <= 5; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: پشتیبانی فنی']", $"{prefix}{i}");
                await Page.ClickAsync("button:has-text('ثبت نوع تیکت')");
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

            // Plural Deactivate
            await Page.ClickAsync("button:has-text('غیرفعال‌سازی')");
            var pluralDeactivateToast = Page.Locator("text=2 نوع تیکت با موفقیت غیرفعال شدند").First;
            await pluralDeactivateToast.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Select 1 and test singular
            var rowSingular = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}4" });
            await rowSingular.ScrollIntoViewIfNeededAsync();
            var checkboxSingular = rowSingular.Locator("input[type='checkbox']");
            await checkboxSingular.CheckAsync();

            await Page.ClickAsync("button:has-text('فعال‌سازی')");
            var singularActivateToast = Page.Locator("text=1 نوع تیکت با موفقیت فعال شد").First;
            await singularActivateToast.WaitForAsync(new() { Timeout = 10000 });
        }

        [Fact]
        public async Task CategoryForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync(Factory.ServerAddress + "/dev/login");
            await Page.GotoAsync(Factory.ServerAddress + "/settings/categories");

            var categoryName = "نوع تیکت لغوی " + System.Guid.NewGuid().ToString().Substring(0, 5);
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

