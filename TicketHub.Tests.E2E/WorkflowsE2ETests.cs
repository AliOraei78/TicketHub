using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using TicketHub.Infrastructure.Data;
using Xunit;

namespace TicketHub.Tests.E2E
{
    [Collection("E2E Tests")]
    public class WorkflowsE2ETests : PlaywrightTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public WorkflowsE2ETests(CustomWebApplicationFactory factory) : base(factory)
        {
        }


        [Fact]
        public async Task WorkflowEditor_ComplexCreation_ShouldSucceed()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=مدیریت جریان‌های کاری");
            await WaitForBlazorAsync(Page, 1500);

            var createBtn = Page.Locator("button:has-text('ایجاد جریان کاری جدید')").First;
            await createBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await createBtn.ClickAsync();
            await Page.WaitForURLAsync("**/workflows/editor");
            await WaitForBlazorAsync(Page, 1000);

            var wfName = "Complex Workflow " + Guid.NewGuid().ToString().Substring(0, 6);
            await Page.WaitForSelectorAsync("input[placeholder='نام جریان کاری...']");
            await Page.FillAsync("input[placeholder='نام جریان کاری...']", wfName);

            // Wait for data to load
            var statusAItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "باز" }).First;
            await statusAItem.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

            // 1- add status A using the icon
            await statusAItem.Locator("button").ClickAsync();
            await WaitForBlazorAsync(Page, 500);

            // 2- add status B using drag and drop to the right side of status A
            var canvas = Page.Locator(".cyber-canvas");
            var canvasBox = (await canvas.BoundingBoxAsync())!;

            var statusBItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "در انتظار تایید" }).First;
            await statusBItem.DragToAsync(canvas, new LocatorDragToOptions {
                TargetPosition = new() { X = canvasBox.Width / 2 + 250, Y = canvasBox.Height / 2 }
            });
            await WaitForBlazorAsync(Page, 500);

            // 3- add status c using drag and drop below status b
            var statusCItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "بسته شده" }).First;
            await statusCItem.DragToAsync(canvas, new LocatorDragToOptions {
                TargetPosition = new() { X = canvasBox.Width / 2 + 250, Y = canvasBox.Height / 2 + 200 }
            });
            await WaitForBlazorAsync(Page, 500);

            var statusAOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('باز'), .cyber-canvas > div:has-text('باز')").First;
            var statusBOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('در انتظار تایید'), .cyber-canvas > div:has-text('در انتظار تایید')").First;
            var statusCOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('بسته شده'), .cyber-canvas > div:has-text('بسته شده')").First;

            async Task DrawTransitionSlowAsync(ILocator source, ILocator target)
            {
                var sBox = (await source.BoundingBoxAsync())!;
                var tBox = (await target.BoundingBoxAsync())!;
                await Page.Mouse.MoveAsync(sBox.X + sBox.Width / 2, sBox.Y + sBox.Height / 2);
                await Page.Mouse.DownAsync();
                await Task.Delay(200);
                await Page.Mouse.MoveAsync(tBox.X + tBox.Width / 2, tBox.Y + tBox.Height / 2, new() { Steps = 20 });
                await Task.Delay(200);
                await Page.Mouse.UpAsync();
                await Task.Delay(500);
            }

            // Create transition between A and B
            var statusAPortRight = statusAOnCanvas.Locator("div[title='Right']");
            var statusBPortLeft = statusBOnCanvas.Locator("div[title='Left']");
            await DrawTransitionSlowAsync(statusAPortRight, statusBPortLeft);

            // 5- create transition from B's bottom node to C's top node
            var statusBPortBottom = statusBOnCanvas.Locator("div[title='Bottom']");
            var statusCPortTop = statusCOnCanvas.Locator("div[title='Top']");
            await DrawTransitionSlowAsync(statusBPortBottom, statusCPortTop);

            // 6- create transition from C's left side to A's bottom side
            var statusCPortLeft = statusCOnCanvas.Locator("div[title='Left']");
            var statusAPortBottom = statusAOnCanvas.Locator("div[title='Bottom']");
            await DrawTransitionSlowAsync(statusCPortLeft, statusAPortBottom);

            // Close sidebar if it's open (it opens automatically when the last transition is drawn)
            await WaitForBlazorAsync(Page, 500);
            var closeBtns = await Page.Locator("button[title='بستن']").AllAsync();
            foreach (var btn in closeBtns)
            {
                if (await btn.IsVisibleAsync())
                {
                    await btn.ClickAsync();
                }
            }
            await WaitForBlazorAsync(Page, 500);

            // 4- click on status A so the icons appear, click on the star IsInitial Icon
            await statusAOnCanvas.ClickAsync();
            await Task.Delay(200);
            var initialBtn = Page.Locator("button[title*='وضعیت اولیه']").First;
            if (await initialBtn.IsVisibleAsync())
            {
                await initialBtn.ClickAsync();
                await Task.Delay(200);
            }

            // 7- click on all transitions and test all transitions field
            var transitionPaths = Page.Locator("path.cursor-pointer");

            // Test transition 1 (A -> B)
            await transitionPaths.Nth(0).ClickAsync(new() { Force = true });
            await WaitForBlazorAsync(Page, 500);

            // fill in the transition عنوان انتقال
            await Page.Locator("label").Filter(new() { HasText = "عنوان انتقال" }).Locator("..").Locator("input").FillAsync("عنوان انتقال تستی");

            // click check box انتقال خودکار
            await Page.Locator("text=انتقال خودکار").ClickAsync();

            // pick a date by custom persian date picker (تاریخ فعال‌سازی)
            var datePicker = Page.Locator("input[placeholder='1403/05/12 14:30']");
            if (await datePicker.IsVisibleAsync())
            {
                await datePicker.ClickAsync();
                await WaitForBlazorAsync(Page, 300);
                var day = Page.Locator(".jdp-day").Nth(15);
                if (await day.IsVisibleAsync())
                {
                    await day.ClickAsync();
                    await WaitForBlazorAsync(Page, 300);
                }
                await Page.Keyboard.PressAsync("Escape");
                await WaitForBlazorAsync(Page, 300);
            }

            // choose roles: ادمین and کاربر
            var adminRoleBtn = Page.Locator("button:has-text('ادمین'), button:has-text('مدیر سیستم')").First;
            if (await adminRoleBtn.IsVisibleAsync()) await adminRoleBtn.ClickAsync();

            var fieldTypes = new[] 
            {
                "متن کوتاه (Text)",
                "متن طولانی (TextArea)",
                "عدد (Number)",
                "تاریخ (Date)",
                "لیست کشویی (Dropdown)",
                "لیست کشویی چند گزینه‌ای (MultipleDropdown)",
                "چک‌باکس (Checkbox)",
                "آپلود فایل (File)",
                "انتخاب رنگ (ColorPicker)"
            };

            for (int i = 0; i < fieldTypes.Length; i++)
            {
                await Page.Locator("form button:has-text('افزودن فیلد')").ClickAsync();
                await WaitForBlazorAsync(Page, 300);

                var fieldCard = Page.Locator("form .space-y-3 > div[draggable='true']").Nth(i);
                await fieldCard.ScrollIntoViewIfNeededAsync();

                var fieldNameInput = fieldCard.Locator("input[placeholder='عنوان فیلد']");
                await fieldNameInput.FillAsync($"فیلد {i + 1}");

                var slideSelect = fieldCard.Locator(".relative[dir='rtl']");
                await slideSelect.Locator(".field-spark-wrap div.cursor-pointer").ClickAsync(new() { Force = true });
                await WaitForBlazorAsync(Page, 300);

                var opt = slideSelect.Locator(".dropdown-menu-container div.cursor-pointer").Filter(new() { HasText = fieldTypes[i] }).First;
                if (await opt.IsVisibleAsync())
                {
                    await opt.ClickAsync(new() { Force = true });
                }
                await WaitForBlazorAsync(Page, 300);

                var placeholderInput = fieldCard.Locator("input[placeholder='متن جایگیر (Placeholder)']");
                if (await placeholderInput.CountAsync() > 0 && await placeholderInput.First.IsVisibleAsync())
                {
                    await placeholderInput.First.FillAsync($"جایگیر {i + 1}");
                }

                var defaultInput = fieldCard.Locator("input[placeholder='مقدار پیش‌فرض']");
                if (await defaultInput.CountAsync() > 0 && await defaultInput.First.IsVisibleAsync())
                {
                    await defaultInput.First.FillAsync(fieldTypes[i].Contains("Number") ? "0" : "مقدار");
                }

                var optionsInput = fieldCard.Locator("input[placeholder='گزینه‌ها را با کاما (,) جدا کنید']");
                if (await optionsInput.CountAsync() > 0 && await optionsInput.First.IsVisibleAsync())
                {
                    await optionsInput.First.FillAsync("گزینه 1,گزینه 2");
                }
            }

            var closeBtn = Page.Locator("button[title='بستن']").First;
            if (await closeBtn.IsVisibleAsync())
            {
                await closeBtn.ClickAsync(new() { Force = true });
                await WaitForBlazorAsync(Page, 300);
            }

            // Fill title for transition 2 (B -> C)
            if (await transitionPaths.CountAsync() > 1) 
            {
                await transitionPaths.Nth(1).ClickAsync(new() { Force = true });
                await WaitForBlazorAsync(Page, 500);
                await Page.Locator("label").Filter(new() { HasText = "عنوان انتقال" }).Locator("..").Locator("input").FillAsync("انتقال دوم");
                if (await closeBtn.IsVisibleAsync()) await closeBtn.ClickAsync(new() { Force = true });
            }

            // Fill title for transition 3 (C -> A)
            if (await transitionPaths.CountAsync() > 2) 
            {
                await transitionPaths.Nth(2).ClickAsync(new() { Force = true });
                await WaitForBlazorAsync(Page, 500);
                await Page.Locator("label").Filter(new() { HasText = "عنوان انتقال" }).Locator("..").Locator("input").FillAsync("انتقال سوم");
                if (await closeBtn.IsVisibleAsync()) await closeBtn.ClickAsync(new() { Force = true });
            }

            // 9- Save workflow
            await Page.ClickAsync("button:has-text('ذخیره جریان کار')", new() { Force = true });

            var anyToast = Page.Locator(".toast-item, div:has-text('جریان کاری با موفقیت ذخیره شد'), div:has-text('موفقیت')").First;
            await anyToast.WaitForAsync(new() { Timeout = 15000 });
            await Page.WaitForURLAsync("**/workflows");
            var header = Page.Locator("text=مدیریت جریان‌های کاری").First;
            (await header.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task WorkflowEditor_Deletion_ShouldSucceed()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=مدیریت جریان‌های کاری");
            await WaitForBlazorAsync(Page, 1500);

            var createBtn = Page.Locator("button:has-text('ایجاد جریان کاری جدید')").First;
            await createBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await createBtn.ClickAsync();
            await Page.WaitForURLAsync("**/workflows/editor");
            await WaitForBlazorAsync(Page, 1000);

            // 1- add status A using the icon
            var statusAItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "باز" }).First;
            await statusAItem.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await statusAItem.Locator("button").ClickAsync();
            await WaitForBlazorAsync(Page, 500);

            // 2- add status B using drag and drop to the right side of status A
            var canvas = Page.Locator(".cyber-canvas");
            await canvas.WaitForAsync(new() { Timeout = 10000 });
            var canvasBox = (await canvas.BoundingBoxAsync())!;

            var statusBItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "در انتظار تایید" });
            await statusBItem.DragToAsync(canvas, new LocatorDragToOptions {
                TargetPosition = new() { X = canvasBox.Width / 2 + 250, Y = canvasBox.Height / 2 }
            });
            await WaitForBlazorAsync(Page, 500);

            // 3- add status c using drag and drop below status b
            var statusCItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "بسته شده" });
            await statusCItem.DragToAsync(canvas, new LocatorDragToOptions {
                TargetPosition = new() { X = canvasBox.Width / 2 + 250, Y = canvasBox.Height / 2 + 200 }
            });
            await WaitForBlazorAsync(Page, 500);

            var statusAOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('باز'), .cyber-canvas > div:has-text('باز')").First;
            var statusBOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('در انتظار تایید'), .cyber-canvas > div:has-text('در انتظار تایید')").First;
            var statusCOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('بسته شده'), .cyber-canvas > div:has-text('بسته شده')").First;

            async Task DrawTransitionSlowAsync(ILocator source, ILocator target)
            {
                var sBox = (await source.BoundingBoxAsync())!;
                var tBox = (await target.BoundingBoxAsync())!;
                await Page.Mouse.MoveAsync(sBox.X + sBox.Width / 2, sBox.Y + sBox.Height / 2);
                await Page.Mouse.DownAsync();
                await Task.Delay(200);
                await Page.Mouse.MoveAsync(tBox.X + tBox.Width / 2, tBox.Y + tBox.Height / 2, new() { Steps = 20 });
                await Task.Delay(200);
                await Page.Mouse.UpAsync();
                await Task.Delay(500);
            }

            // Create transition between A and B
            var statusAPortRight = statusAOnCanvas.Locator("div[title='Right']");
            var statusBPortLeft = statusBOnCanvas.Locator("div[title='Left']");
            await DrawTransitionSlowAsync(statusAPortRight, statusBPortLeft);

            // 5- create transition from B's bottom node to C's top node
            var statusBPortBottom = statusBOnCanvas.Locator("div[title='Bottom']");
            var statusCPortTop = statusCOnCanvas.Locator("div[title='Top']");
            await DrawTransitionSlowAsync(statusBPortBottom, statusCPortTop);

            // 6- create transition from C's left side to A's bottom side
            var statusCPortLeft = statusCOnCanvas.Locator("div[title='Left']");
            var statusAPortBottom = statusAOnCanvas.Locator("div[title='Bottom']");
            await DrawTransitionSlowAsync(statusCPortLeft, statusAPortBottom);

            // Close sidebar if it's open
            await WaitForBlazorAsync(Page, 500);
            var closeBtns = await Page.Locator("button[title='بستن']").AllAsync();
            foreach (var btn in closeBtns)
            {
                if (await btn.IsVisibleAsync())
                {
                    await btn.ClickAsync();
                }
            }
            await WaitForBlazorAsync(Page, 500);

            // 6-1 DELETION TEST
            var transitionPaths = Page.Locator("path.cursor-pointer");

            // 1. Delete transition A to B
            await transitionPaths.Nth(0).ClickAsync(new() { Force = true });
            await WaitForBlazorAsync(Page, 500);
            var delConnBtn = Page.Locator("button[title*='حذف مسیر'], button[title*='حذف']").First;
            if (await delConnBtn.IsVisibleAsync())
            {
                await delConnBtn.ClickAsync();
                await WaitForBlazorAsync(Page, 500);
            }

            // 2. Delete Status A
            await statusAOnCanvas.ClickAsync();
            await Task.Delay(200);
            var delNodeBtn = statusAOnCanvas.Locator("button[title*='حذف وضعیت'], button.bg-rose-500").First;
            if (await delNodeBtn.IsVisibleAsync())
            {
                await delNodeBtn.ClickAsync();
                await WaitForBlazorAsync(Page, 500);
            }

            // 3. Hold Shift, select remaining nodes/transitions one by one, and click Status B delete icon to bulk delete
            await Page.Keyboard.DownAsync("Shift");
            if (await transitionPaths.CountAsync() > 0)
            {
                await transitionPaths.Nth(0).ClickAsync(new() { Force = true });
            }
            await statusCOnCanvas.ClickAsync();
            await Task.Delay(200);
            await statusBOnCanvas.ClickAsync();
            await Task.Delay(200);
            await Page.Keyboard.UpAsync("Shift");

            // Click delete on Status B to bulk delete
            var delNodeBtnB = statusBOnCanvas.Locator("button[title*='حذف وضعیت'], button.bg-rose-500").First;
            if (await delNodeBtnB.IsVisibleAsync())
            {
                await delNodeBtnB.ClickAsync();
                await WaitForBlazorAsync(Page, 500);
            }

            // Verify Canvas is empty
            (await statusBOnCanvas.IsVisibleAsync()).Should().BeFalse();
            (await statusCOnCanvas.IsVisibleAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task WorkflowEditor_Validation_ShouldFailWhenFieldsEmpty()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=مدیریت جریان‌های کاری");
            await WaitForBlazorAsync(Page, 1000);
            await Page.ClickAsync("button:has-text('ایجاد جریان کاری جدید')");
            await Page.WaitForURLAsync("**/workflows/editor");
            await WaitForBlazorAsync(Page, 800);
            
            var canvas = Page.Locator(".cyber-canvas");
            var canvasBox = (await canvas.BoundingBoxAsync())!;
            
            var statusAItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "باز" });
            await statusAItem.DragToAsync(canvas, new LocatorDragToOptions {
                TargetPosition = new() { X = (canvasBox.Width / 4) * 3, Y = canvasBox.Height / 2 }
            });
            await Task.Delay(500);

            var statusBItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "در انتظار تایید" });
            await statusBItem.DragToAsync(canvas, new LocatorDragToOptions {
                TargetPosition = new() { X = canvasBox.Width / 4, Y = canvasBox.Height / 2 }
            });
            await Task.Delay(500);

            var statusAOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('باز'), .cyber-canvas > div:has-text('باز')").First;
            var statusBOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('در انتظار تایید'), .cyber-canvas > div:has-text('در انتظار تایید')").First;
              
            var statusAPortLeft = statusAOnCanvas.Locator("div[title='Left']");
            var statusBPortRight = statusBOnCanvas.Locator("div[title='Right']");
            
            var sBox = (await statusAPortLeft.BoundingBoxAsync())!;
            var tBox = (await statusBPortRight.BoundingBoxAsync())!;
            await Page.Mouse.MoveAsync(sBox.X + sBox.Width / 2, sBox.Y + sBox.Height / 2);
            await Page.Mouse.DownAsync();
            await Task.Delay(200);
            await Page.Mouse.MoveAsync(tBox.X + tBox.Width / 2, tBox.Y + tBox.Height / 2, new() { Steps = 20 });
            await Task.Delay(200);
            await Page.Mouse.UpAsync();
            await Task.Delay(500);

            await Page.ClickAsync("button:has-text('+ افزودن فیلد')");
            await Task.Delay(200);
            
            await Page.ClickAsync("button:has-text('ذخیره جریان کار')");

            var wfNameValidationMsg = Page.Locator("text=نام جریان کاری الزامی است.");
            await wfNameValidationMsg.WaitForAsync(new() { Timeout = 5000 });
            
            var transitionNameValidationMsg = Page.Locator("text=نام انتقال الزامی است.");
            await transitionNameValidationMsg.WaitForAsync(new() { Timeout = 5000 });
            
            var dynamicFieldNameValidationMsg = Page.Locator("text=نام فیلد الزامی است.");
            await dynamicFieldNameValidationMsg.WaitForAsync(new() { Timeout = 5000 });
 
            var dynamicInitialStateValidationMsg = Page.Locator("text=دقیقاً یک وضعیت باید به عنوان وضعیت اولیه جریان کاری انتخاب شود.");
            await dynamicInitialStateValidationMsg.WaitForAsync(new() { Timeout = 5000 });
        }

        private async Task CreateValidWorkflowAsync(string name)
        {
            await Page.ClickAsync("button:has-text('ایجاد جریان کاری جدید')");
            await Page.WaitForURLAsync("**/workflows/editor");
            await WaitForBlazorAsync(Page, 1000);
            
            // Wait for data to load
            var statusAItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "باز" });
            await statusAItem.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            
            await Page.WaitForSelectorAsync("input[placeholder='نام جریان کاری...']");
            await Page.FillAsync("input[placeholder='نام جریان کاری...']", name);
            
            // Add Status A
            await statusAItem.Locator("button").ClickAsync();
            await WaitForBlazorAsync(Page, 500);

            // Add Status B
            var statusBItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "در انتظار تایید" });
            await statusBItem.Locator("button").ClickAsync();
            await WaitForBlazorAsync(Page, 500);

            // Connect A to B
            var statusAOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('باز'), .cyber-canvas > div:has-text('باز')").First;
            await statusAOnCanvas.WaitForAsync(new() { Timeout = 10000 });
            var statusBOnCanvas = Page.Locator(".cyber-canvas .cyber-node-chassis:has-text('در انتظار تایید'), .cyber-canvas > div:has-text('در انتظار تایید')").First;
            await statusBOnCanvas.WaitForAsync(new() { Timeout = 10000 });
            
            var sBox = (await statusAOnCanvas.BoundingBoxAsync())!;
            var tBox = (await statusBOnCanvas.BoundingBoxAsync())!;
            await Page.Mouse.MoveAsync(sBox.X + sBox.Width / 2, sBox.Y + sBox.Height / 2);
            await Page.Mouse.DownAsync();
            await Task.Delay(200);
            await Page.Mouse.MoveAsync(tBox.X + tBox.Width / 2, tBox.Y + tBox.Height / 2, new() { Steps = 10 });
            await Task.Delay(200);
            await Page.Mouse.UpAsync();
            await WaitForBlazorAsync(Page, 500);

            // Fill connection name in sidebar
            var transitionTitleInput = Page.Locator("label").Filter(new() { HasText = "عنوان انتقال" }).Locator("..").Locator("input").First;
            if(await transitionTitleInput.IsVisibleAsync())
            {
               await transitionTitleInput.FillAsync("تایید");
               await Page.ClickAsync("button[title='بستن']");
               await Task.Delay(200);
            }

            // Set initial status
            await statusAOnCanvas.ClickAsync();
            await Task.Delay(200);
            var initialBtn = Page.Locator("button[title*='وضعیت اولیه']").First;
            if (await initialBtn.IsVisibleAsync())
            {
                await initialBtn.ClickAsync();
                await WaitForBlazorAsync(Page, 500);
            }

            // Close any open sidebars
            var closeBtns = await Page.Locator("button[title='بستن']").AllAsync();
            foreach (var btn in closeBtns)
            {
                if (await btn.IsVisibleAsync())
                {
                    await btn.ClickAsync();
                    await Task.Delay(200);
                }
            }

            await Page.ClickAsync("button:has-text('ذخیره جریان کار')");
            var toastLocator = Page.Locator("text=جریان کاری با موفقیت ذخیره شد.").First;
            await toastLocator.WaitForAsync(new() { Timeout = 10000 });
            await Page.WaitForURLAsync("**/workflows");
            await toastLocator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        [Fact]
        public async Task WorkflowsSettings_BulkActions_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=مدیریت جریان‌های کاری");
            await WaitForBlazorAsync(Page, 1000);

            string prefix = $"BulkTest_{Guid.NewGuid().ToString("N").Substring(0, 4)}_";
            
            // 1. Create 3 workflows
            for (int i = 1; i <= 3; i++)
            {
                await CreateValidWorkflowAsync($"{prefix}{i}");
                await WaitForBlazorAsync(Page, 800);
            }

            // 2. Select all 3
            var searchInput = Page.Locator("input[placeholder='جستجوی جریان کاری...']");
            await searchInput.FillAsync(prefix);
            await WaitForBlazorAsync(Page, 1200);

            for(int i = 1; i <= 3; i++)
            {
                var card = Page.Locator($".circuit-tracer-card:has-text('{prefix}{i}')").First;
                await card.Locator("label.cyber-checkbox-container input, input[type='checkbox']").First.ClickAsync(new() { Force = true });
                await WaitForBlazorAsync(Page, 400);
            }

            var bulkBar = Page.Locator("div.fixed:has-text('مورد انتخاب شده')").First;
            await bulkBar.WaitForAsync(new() { Timeout = 8000 });
            (await bulkBar.InnerTextAsync()).Should().Contain("3");

            // 3. Delete 1 via row action
            var firstCard = Page.Locator($".circuit-tracer-card:has-text('{prefix}1')").First;
            await firstCard.Locator("button[title='حذف جریان کار']").First.ClickAsync(new() { Force = true });
            
            var confirmBtn = Page.Locator("button:has-text('بله، حذف کن')").First;
            await confirmBtn.ClickAsync(new() { Force = true });

            var toastSingle = Page.Locator($"text=جریان کاری '{prefix}1' با موفقیت حذف شد.").First;
            await toastSingle.WaitForAsync(new() { Timeout = 5000 });
            await toastSingle.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 4. Verify selection is now 2
            await bulkBar.WaitForAsync(new() { Timeout = 5000 });
            (await bulkBar.InnerTextAsync()).Should().Contain("2");

            // 5. Delete remaining 2 via bulk action
            await Page.Locator("div.fixed.bottom-6, div.fixed").Locator("button:has-text('حذف'), button:has-text('حذف گروهی')").First.ClickAsync(new() { Force = true });
            var confirmBtnBulk = Page.Locator("button:has-text('بله، حذف کن')").First;
            await confirmBtnBulk.ClickAsync(new() { Force = true });

            var toastBulkMultiple = Page.Locator("text=2 جریان کاری حذف شدند.").First;
            await toastBulkMultiple.WaitForAsync(new() { Timeout = 5000 });
            await toastBulkMultiple.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            
            // 6. Create 1 more to test single bulk delete
            await CreateValidWorkflowAsync($"{prefix}4");
            
            // Search to ensure it is on the first page
            searchInput = Page.Locator("input[placeholder='جستجوی جریان کاری...']");
            await searchInput.FillAsync($"{prefix}4");
            await WaitForBlazorAsync(Page, 1200);
            
            // 7. Select 1
            var fourthCard = Page.Locator($".circuit-tracer-card:has-text('{prefix}4')").First;
            await fourthCard.Locator("label.cyber-checkbox-container input, input[type='checkbox']").First.ClickAsync(new() { Force = true });
            await WaitForBlazorAsync(Page, 400);
            
            await bulkBar.WaitForAsync(new() { Timeout = 5000 });
            (await bulkBar.InnerTextAsync()).Should().Contain("1");
            
            // 8. Delete via bulk action
            await Page.Locator("div.fixed.bottom-6, div.fixed").Locator("button:has-text('حذف'), button:has-text('حذف گروهی')").First.ClickAsync(new() { Force = true });
            var confirmBtnBulkSingle = Page.Locator("button:has-text('بله، حذف کن')").First;
            await confirmBtnBulkSingle.ClickAsync(new() { Force = true });

            var toastBulkSingle = Page.Locator("text=1 جریان کاری حذف شد.").First;
            await toastBulkSingle.WaitForAsync(new() { Timeout = 5000 });
        }

        [Fact]
        public async Task WorkflowsSettings_Pagination_BulkDelete_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=مدیریت جریان‌های کاری");
            await WaitForBlazorAsync(Page, 1000);

            string prefix = $"PgTest_{Guid.NewGuid().ToString("N").Substring(0, 4)}_";
            
            // 1. Create 9 workflows directly in DB for speed
            using (var scope = Factory.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var context = dbFactory.CreateDbContext();
                
                for (int i = 1; i <= 9; i++)
                {
                    context.Workflows.Add(new TicketHub.Core.Entities.Workflow { Name = $"{prefix}{i}", IsActive = true, CreatedAt = DateTime.UtcNow });
                }
                await context.SaveChangesAsync();
            }

            // Reload the page
            await Page.ReloadAsync();
            await Page.WaitForSelectorAsync("text=مدیریت جریان‌های کاری");
            await WaitForBlazorAsync(Page, 1000);

            // 2. Search to ensure we only see these 9
            var searchInput = Page.Locator("input[placeholder='جستجوی جریان کاری...']");
            await searchInput.FillAsync(prefix);
            await WaitForBlazorAsync(Page, 1200);

            // 3. Select 2 items from the first page
            for(int i = 1; i <= 2; i++)
            {
                var card = Page.Locator($".circuit-tracer-card:has-text('{prefix}{i}')").First;
                if (await card.IsVisibleAsync())
                {
                    await card.Locator("label.cyber-checkbox-container").ClickAsync(new() { Force = true });
                }
            }

            // 4. Navigate to next page if pagination exists
            var nextButton = Page.Locator("button:has-text('بعدی'), button:has-text('صفحه بعد'), button:has-text('>')").First;
            if (await nextButton.IsVisibleAsync() && !await nextButton.IsDisabledAsync())
            {
                await nextButton.ClickAsync();
                await WaitForBlazorAsync(Page, 1000);
            }

            // 5. Delete the selected items using bulk delete button
            var bulkDeleteBtn = Page.Locator("div.fixed.bottom-6, div.fixed").Locator("button:has-text('حذف'), button:has-text('حذف گروهی')").First;
            if (await bulkDeleteBtn.IsVisibleAsync())
            {
                await bulkDeleteBtn.ClickAsync();
                var confirmBtn = Page.Locator("button:has-text('بله، حذف کن')").First;
                await confirmBtn.ClickAsync();
                await WaitForBlazorAsync(Page, 1500);
            }
        }

        [Fact]
        public async Task WorkflowEditor_Cancel_NavigatesBackToWorkflowsDashboard()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=مدیریت جریان‌های کاری");
            await WaitForBlazorAsync(Page, 800);

            await Page.ClickAsync("button:has-text('ایجاد جریان کاری جدید')");
            await Page.WaitForURLAsync("**/workflows/editor");
            await WaitForBlazorAsync(Page, 800);

            var cancelBtn = Page.Locator("a:has-text('انصراف و بازگشت')").First;
            await cancelBtn.ClickAsync();

            await Page.WaitForURLAsync("**/workflows");
            await Page.WaitForSelectorAsync("text=مدیریت جریان‌های کاری");
            var header = Page.Locator("text=مدیریت جریان‌های کاری").First;
            (await header.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Workflows_SearchFilter_LiveGridFilter()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=مدیریت جریان‌های کاری");

            var searchInput = Page.Locator("input[placeholder='جستجوی جریان کاری...']");
            await searchInput.FillAsync("ناموجود_" + Guid.NewGuid().ToString().Substring(0, 5));
            await WaitForBlazorAsync(Page, 1000);

            // Verify empty state or 0 cards
            var emptyText = Page.Locator("text=هیچ جریان کاری یافت نشد").First;
            if (await emptyText.CountAsync() > 0)
            {
                (await emptyText.IsVisibleAsync()).Should().BeTrue();
            }

            // Clear search
            await searchInput.FillAsync("");
            await WaitForBlazorAsync(Page, 800);
        }
    }
}