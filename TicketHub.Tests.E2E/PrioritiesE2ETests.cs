using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class PrioritiesE2ETests : PlaywrightTestBase
    {
        public PrioritiesE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Create_Priority_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var newPriorityName = "اولویت تست " + System.Guid.NewGuid().ToString().Substring(0, 5);
            // InputText doesn't have a placeholder in this component, select by label or type
            await Page.Locator("text=عنوان اولویت").Locator("xpath=following-sibling::input").FillAsync(newPriorityName);
            await Page.FillAsync("input[type='number']", "15");

            // Optional: Pick a color (Playwright can fill type="color")
            await Page.FillAsync("input[type='color']", "#ff0000");

            // Submit form
            await Page.ClickAsync("button:has-text('ثبت اولویت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newPriorityName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Edit_Priority_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var newPriorityName = "اولویت ویرایشی " + System.Guid.NewGuid().ToString().Substring(0, 5);
            await Page.Locator("text=عنوان اولویت").Locator("xpath=following-sibling::input").FillAsync(newPriorityName);
            await Page.FillAsync("input[type='number']", "20");
            await Page.ClickAsync("button:has-text('ثبت اولویت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newPriorityName }).First;
            await rowLocator.WaitForAsync();

            // Click edit on this specific row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            // Wait for form to enter edit mode to prevent Blazor overwriting our input
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش اولویت')");
            await editHeaderLocator.WaitForAsync();

            var updatedName = newPriorityName + " ویرایش شده";
            await Page.Locator("text=عنوان اولویت").Locator("xpath=following-sibling::input").FillAsync(updatedName);
            await Page.Keyboard.PressAsync("Tab"); // Ensure Blazor updates the model before submit
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')"); 

            var editToastLocator = Page.Locator("text=ویرایش شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            var updatedRowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRowLocator.WaitForAsync();
            (await updatedRowLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Delete_Priority_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var newPriorityName = "اولویت حذفی " + System.Guid.NewGuid().ToString().Substring(0, 5);
            await Page.Locator("text=عنوان اولویت").Locator("xpath=following-sibling::input").FillAsync(newPriorityName);
            await Page.FillAsync("input[type='number']", "25");
            await Page.ClickAsync("button:has-text('ثبت اولویت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newPriorityName }).First;
            await rowLocator.WaitForAsync();

            // Click delete
            await rowLocator.Locator("button[title='حذف']").ClickAsync();

            // Confirm delete
            var modalLocator = Page.Locator("text=آیا از حذف اولویت").First;
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
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var headerLocator = Page.Locator("h1:has-text('مدیریت اولویت‌ها')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('ثبت اولویت')");

            var errorLocator = Page.Locator("text=نام اولویت الزامی است").First;
            await errorLocator.WaitForAsync();
            (await errorLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task PrioritiesSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var prefix = "BulkPriority_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for(int i = 1; i <= 3; i++)
            {
                await Page.Locator("text=عنوان اولویت").Locator("xpath=following-sibling::input").FillAsync($"{prefix}{i}");
                await Page.FillAsync("input[type='number']", "15");
                await Page.FillAsync("input[type='color']", "#ff0000");
                await Page.ClickAsync("button:has-text('ثبت اولویت')");
                
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
            
            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.ClickAsync("button:has-text('بله، حذف کن')");
            
            var deleteToastLocator = Page.Locator("text=اولویت با موفقیت حذف شد.").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6:has-text('2 مورد انتخاب شده')").First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 10000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();
            await Page.ClickAsync("button:has-text('حذف')");
            var bulkDeleteModal = Page.Locator("text=مطمئن هستید").First;
            await bulkDeleteModal.WaitForAsync(new() { Timeout = 10000 });
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            var pluralToast = Page.Locator("text=2 اولویت با موفقیت حذف شدند.").First;
            await pluralToast.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // Test Activate/Deactivate
            for(int i = 4; i <= 5; i++)
            {
                await Page.Locator("text=عنوان اولویت").Locator("xpath=following-sibling::input").FillAsync($"{prefix}{i}");
                await Page.FillAsync("input[type='number']", "15");
                await Page.FillAsync("input[type='color']", "#ff0000");
                await Page.ClickAsync("button:has-text('ثبت اولویت')");
                
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
            var pluralDeactivateToast = Page.Locator("text=2 اولویت با موفقیت غیرفعال شدند.").First;
            await pluralDeactivateToast.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            var rowSingular = Page.Locator("tr", new PageLocatorOptions { HasTextString = $"{prefix}4" });
            await rowSingular.ScrollIntoViewIfNeededAsync();
            var checkboxSingular = rowSingular.Locator("input[type='checkbox']");
            await checkboxSingular.CheckAsync();

            await Page.ClickAsync("button:has-text('فعال‌سازی')");
            var singularActivateToast = Page.Locator("text=1 اولویت با موفقیت فعال شد.").First;
            await singularActivateToast.WaitForAsync(new() { Timeout = 10000 });
        }

        [Fact]
        public async Task PriorityForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var priorityName = "اولویت لغوی " + System.Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: بحرانی / فوری']", priorityName);
            await Page.FillAsync("input[type='number']", "33");
            await Page.ClickAsync("button:has-text('ثبت اولویت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Click edit
            var rowLocator = Page.Locator("tr:has(td:has-text('" + priorityName + "'))").First;
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            var editHeader = Page.Locator("h3:has-text('ویرایش اولویت')").First;
            await editHeader.WaitForAsync();

            // Click cancel
            await Page.ClickAsync("button:has-text('انصراف')");

            var createHeader = Page.Locator("h3:has-text('ایجاد اولویت جدید')").First;
            await createHeader.WaitForAsync();
            (await createHeader.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task PrioritiesSettings_3StateStatusFilter_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            await Page.WaitForSelectorAsync("h1:has-text('مدیریت اولویت‌ها')");

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


