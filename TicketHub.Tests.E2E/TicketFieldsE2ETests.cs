using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class TicketFieldsE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public TicketFieldsE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Create_TicketField_Successfully()
        {
            // Navigate to the component/page
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلدهای تیکت')").First;
            await headerLocator.WaitForAsync();

            var newFieldName = "فیلد تست E2E " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", newFieldName);
            await Page.FillAsync("input[type='number']", "10");

            // Open SlideSelect and choose option
            await Page.ClickAsync("text=انتخاب نوع فیلد...");
            await Task.Delay(500);
            await Page.ClickAsync("li:has-text('متن کوتاه (Text)')");
            await Task.Delay(300);

            // Submit form
            await Page.ClickAsync("button:has-text('ثبت فیلد')");

            // Wait for success toast
            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify grid contains the newly created record
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newFieldName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Edit_TicketField_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلدهای تیکت')").First;
            await headerLocator.WaitForAsync();

            var newFieldName = "فیلد ویرایشی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", newFieldName);
            await Page.FillAsync("input[type='number']", "20");
            await Page.ClickAsync("text=انتخاب نوع فیلد...");
            await Task.Delay(500);
            await Page.ClickAsync("li:has-text('متن کوتاه (Text)')");
            await Task.Delay(300);
            await Page.ClickAsync("button:has-text('ثبت فیلد')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newFieldName }).First;
            await rowLocator.WaitForAsync();

            // Click edit button in the row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            // Wait for form to enter edit mode to prevent Blazor overwriting our input
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش فیلد')");
            await editHeaderLocator.WaitForAsync();

            // Modify Name
            var updatedName = newFieldName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", updatedName);
            await Page.Keyboard.PressAsync("Tab"); // Ensure Blazor updates the model
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')"); 

            // Wait for success toast
            var editToastLocator = Page.Locator("text=ویرایش شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify grid updated
            var updatedRow = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRow.WaitForAsync();
            (await updatedRow.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task Delete_TicketField_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلدهای تیکت')").First;
            await headerLocator.WaitForAsync();

            var newFieldName = "فیلد حذفی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", newFieldName);
            await Page.FillAsync("input[type='number']", "30");
            await Page.ClickAsync("text=انتخاب نوع فیلد...");
            await Task.Delay(500);
            await Page.ClickAsync("li:has-text('متن کوتاه (Text)')");
            await Task.Delay(300);
            await Page.ClickAsync("button:has-text('ثبت فیلد')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newFieldName }).First;
            await rowLocator.WaitForAsync();

            // Click delete button
            await rowLocator.Locator("button[title='حذف']").ClickAsync();

            // Modal should appear, click confirm
            var modalLocator = Page.Locator("text=آیا از حذف").First;
            await modalLocator.WaitForAsync();

            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            // Wait for success toast
            var deleteToastLocator = Page.Locator("text=حذف شد").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify row is gone
            await rowLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
            (await rowLocator.IsVisibleAsync()).Should().BeFalse();        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            // Wait for page
            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلدهای تیکت')").First;
            await headerLocator.WaitForAsync();

            // Click submit without filling anything
            await Page.ClickAsync("button:has-text('ثبت فیلد')");

            // Verify validation error for empty Name
            var errorLocator = Page.Locator("text=نام فیلد الزامی است").First;
            await errorLocator.WaitForAsync();
            (await errorLocator.IsVisibleAsync()).Should().BeTrue();        }

        [Fact]
        public async Task TicketFieldsSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلدهای تیکت')").First;
            await headerLocator.WaitForAsync();

            var prefix = "BulkField_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for (int i = 1; i <= 3; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", $"{prefix}{i}");
                await Page.FillAsync("input[type='number']", $"{i}");
                
                await Page.ClickAsync("text=انتخاب نوع فیلد...");
                await Task.Delay(500);
                await Page.ClickAsync("li:has-text('متن کوتاه (Text)')");
                await Task.Delay(300);
                
                await Page.ClickAsync("button:has-text('ثبت فیلد')");
                
                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            for (int i = 1; i <= 3; i++)
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
            
            var deleteToastLocator = Page.Locator("text=فیلد تیکت با موفقیت حذف شد.").First;
            await deleteToastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            var selectedCountIndicator = Page.Locator("div.fixed.bottom-6:has-text('2 مورد انتخاب شده')").First;
            await selectedCountIndicator.WaitForAsync(new() { Timeout = 10000 });
            (await selectedCountIndicator.IsVisibleAsync()).Should().BeTrue();
            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('حذف')");
            var bulkDeleteModal = Page.Locator("text=مطمئن هستید").First;
            await bulkDeleteModal.WaitForAsync(new() { Timeout = 10000 });
            await Page.ClickAsync("button:has-text('بله، حذف کن')");

            var pluralToast = Page.Locator("text=2 فیلد تیکت با موفقیت حذف شدند.").First;
            await pluralToast.WaitForAsync(new() { Timeout = 10000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);
        }

        [Fact]
        public async Task TicketFieldForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var fieldName = "فیلد لغوی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", fieldName);
            await Page.FillAsync("input[type='number']", "1");
            await Page.ClickAsync("text=انتخاب نوع فیلد...");
            await Task.Delay(500);
            await Page.ClickAsync("li:has-text('متن کوتاه (Text)')");
            await Task.Delay(300);
            await Page.ClickAsync("button:has-text('ثبت فیلد')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Click edit
            var rowLocator = Page.Locator("tr:has(td:has-text('" + fieldName + "'))").First;
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            var editHeader = Page.Locator("h3:has-text('ویرایش فیلد')").First;
            await editHeader.WaitForAsync();

            // Click cancel
            await Page.ClickAsync("button:has-text('انصراف')");

            var createHeader = Page.Locator("h3:has-text('ایجاد فیلد جدید')").First;
            await createHeader.WaitForAsync();
            (await createHeader.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task TicketFieldsSettings_3StateStatusFilter_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            await Page.WaitForSelectorAsync("h1:has-text('مدیریت فیلدهای داینامیک تیکت')");

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

