using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

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
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلد')").First;
            await headerLocator.WaitForAsync();

            var newFieldName = "فیلد تست E2E " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", newFieldName);
            await Page.FillAsync("input[type='number']", "10");

            // Open SlideSelect and choose option
            await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع فیلد')");
            await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('متن')");

            // Submit form
            await Page.ClickAsync("button:has-text('ثبت فیلد')");

            // Wait for success toast
            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify grid contains the newly created record
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newFieldName }).First;
            await rowLocator.WaitForAsync();
            (await rowLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Edit_TicketField_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلد')").First;
            await headerLocator.WaitForAsync();

            var newFieldName = "فیلد ویرایشی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", newFieldName);
            await Page.FillAsync("input[type='number']", "20");
            await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع فیلد')");
            await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('متن')");
            await Page.ClickAsync("button:has-text('ثبت فیلد')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = newFieldName }).First;
            await rowLocator.WaitForAsync();

            // Click edit button in the row
            await rowLocator.Locator("button[title='ویرایش']").ClickAsync();

            // Wait for form to enter edit mode
            var editHeaderLocator = Page.Locator("h3:has-text('ویرایش فیلد')");
            await editHeaderLocator.WaitForAsync();

            // Modify Name
            var updatedName = newFieldName + " ویرایش شده";
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", updatedName);
            await Page.Keyboard.PressAsync("Tab");
            await Page.ClickAsync("button:has-text('ذخیره تغییرات')");

            // Wait for success toast
            var editToastLocator = Page.Locator("text=ویرایش شد").First;
            await editToastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Verify grid updated
            var updatedRow = Page.Locator("tr", new PageLocatorOptions { HasTextString = updatedName }).First;
            await updatedRow.WaitForAsync();
            (await updatedRow.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Delete_TicketField_Successfully()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلد')").First;
            await headerLocator.WaitForAsync();

            var newFieldName = "فیلد حذفی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", newFieldName);
            await Page.FillAsync("input[type='number']", "30");
            await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع فیلد')");
            await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('متن')");
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
            (await rowLocator.IsVisibleAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task Submit_EmptyForm_ShowsValidationMessages()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلد')").First;
            await headerLocator.WaitForAsync();

            // Click submit without filling anything
            await Page.ClickAsync("button:has-text('ثبت فیلد')");

            // Verify validation error for empty Name
            var errorLocator = Page.Locator("text=نام فیلد الزامی است").First;
            await errorLocator.WaitForAsync();
            (await errorLocator.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task TicketFieldsSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var headerLocator = Page.Locator("h1:has-text('مدیریت فیلد')").First;
            await headerLocator.WaitForAsync();

            var prefix = "BulkField_" + Guid.NewGuid().ToString().Substring(0, 5) + "_";
            for (int i = 1; i <= 3; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", $"{prefix}{i}");
                await Page.FillAsync("input[type='number']", $"{i}");

                await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع فیلد')");
                await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('متن')");

                await Page.ClickAsync("button:has-text('ثبت فیلد')");

                var toastLocator = Page.Locator("text=ایجاد شد").First;
                await toastLocator.WaitForAsync(new() { Timeout = 10000 });
                await Page.Mouse.ClickAsync(10, 10);
                await Page.WaitForTimeoutAsync(500);
            }

            // 2. Select all 3
            var searchInput = Page.Locator("input[placeholder='جستجوی فیلد...']");
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
            await rowToDelete.ScrollIntoViewIfNeededAsync();
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
            var bulkDeleteModal = Page.Locator("text=مطمئن هستید").First;
            await bulkDeleteModal.WaitForAsync(new() { Timeout = 10000 });

            var modalConfirm2 = Page.Locator("button:has-text('بله، حذف کن')").First;
            await modalConfirm2.ClickAsync(new() { Force = true });

            var pluralToast = Page.Locator("text=با موفقیت حذف").Last;
            await pluralToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(600);

            // Test Activate/Deactivate
            for (int i = 4; i <= 5; i++)
            {
                await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", $"{prefix}{i}");
                await Page.FillAsync("input[type='number']", $"{i}");
                await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع فیلد')");
                await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('متن')");
                await Page.ClickAsync("button:has-text('ثبت فیلد')");

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

            await Page.ClickAsync("div.fixed.bottom-6 button:has-text('غیرفعال‌سازی')");
            var pluralDeactivateToast = Page.Locator("text=با موفقیت غیر").Last;
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
        public async Task TicketFieldForm_CancelEdit_ResetsFormToCreateMode()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/settings/ticket-fields");

            var fieldName = "فیلد لغوی " + Guid.NewGuid().ToString().Substring(0, 5);
            await Page.FillAsync("input[placeholder='مثال: شماره موبایل']", fieldName);
            await Page.FillAsync("input[type='number']", "1");
            await Page.ClickAsync(".field-spark-wrap div.cursor-pointer:has-text('انتخاب نوع فیلد')");
            await Page.ClickAsync(".dropdown-menu-container div.cursor-pointer:has-text('متن')");
            await Page.ClickAsync("button:has-text('ثبت فیلد')");

            var toastLocator = Page.Locator("text=ایجاد شد").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });

            // Click edit
            var rowLocator = Page.Locator("tr", new PageLocatorOptions { HasTextString = fieldName });
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

            await Page.WaitForSelectorAsync("h1:has-text('مدیریت فیلد')");

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

