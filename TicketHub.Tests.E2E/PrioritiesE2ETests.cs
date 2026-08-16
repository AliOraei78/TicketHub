using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System;

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
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var newPriorityName = "اولویت تست " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: بحرانی / فوری']", newPriorityName);
            await Page.FillAsync("input[type='number']", "15");
            await Page.FillAsync("input[type='color']", "#ff0000");

            // Submit form
            await Page.ClickAsync("button:has-text('ثبت اولویت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newPriorityName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Edit_Priority_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var newPriorityName = "اولویت ویرایشی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: بحرانی / فوری']", newPriorityName);
            await Page.FillAsync("input[type='number']", "20");
            await Page.ClickAsync("button:has-text('ثبت اولویت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newPriorityName }).First;
            await rowLocator.WaitForAsync();

            // Click edit on this specific row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            // Wait for form to enter edit mode
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش اولویت')");
            await editHeaderLocator.WaitForAsync();

            var updatedName = newPriorityName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='مثال: بحرانی / فوری']", updatedName);
            await Page.Keyboard.PressAsync("Tab");
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')");

            var editToastLocator = Page.Locator("text=ویرایش شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            var updatedRowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRowLocator.WaitForAsync();
            (await updatedRowLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Delete_Priority_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var newPriorityName = "اولویت حذفی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: بحرانی / فوری']", newPriorityName);
            await Page.FillAsync("input[type='number']", "25");
            await Page.ClickAsync("button:has-text('ثبت اولویت')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newPriorityName }).First;
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
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var headerLocator = Page.Locator("h1:has-text('مدیریت اولویت')").First;
            await headerLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('ثبت اولویت')");

            var errorLocator = Page.Locator("text=نام اولویت الزامی است").First;
            await errorLocator.WaitForAsync();
            (await errorLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task PrioritiesSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var prefix = "BulkPriority_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for (int i = 1; i <= 3; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: بحرانی / فوری']", $"{prefix}{i}");
                await Page.FillAsync("input[type='number']", "15");
                await Page.FillAsync("input[type='color']", "#ff0000");
                await Page.ClickAsync("button:has-text('ثبت اولویت')");

                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            var searchInput = Page.Locator("input[placeholder='جستجوی اولویت...']");
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

            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6", new() { HasTextString = "2" }).First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 10000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();
            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('حذف گروهی')");

            var bulkDeleteModalText = Page.Locator("text=اولویت انتخاب شده مطمئن هستید").First;
            await bulkDeleteModalText.WaitForAsync(new() { Timeout = 10000 });

            var modalConfirm2 = Page.Locator("button:has-text('بله، حذف کن')").First;
            await modalConfirm2.ClickAsync(new() { Force = true });

            var pluralToast = Page.Locator("text=با موفقیت حذف").Last;
            await pluralToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.WaitForTimeoutAsync(600);

            // Test Activate/Deactivate
            for (int i = 4; i <= 5; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: بحرانی / فوری']", $"{prefix}{i}");
                await Page.FillAsync("input[type='number']", "15");
                await Page.FillAsync("input[type='color']", "#ff0000");
                await Page.ClickAsync("button:has-text('ثبت اولویت')");

                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
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
        public async Task PriorityForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/priorities");

            var priorityName = "اولویت لغوی " + Guid.NewGuid().ToString().Substring(0, 5);
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

            await Page.WaitForSelectorAsync("h1:has-text('مدیریت اولویت')");

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


