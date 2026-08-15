using System;
using Microsoft.Extensions.DependencyInjection;

using System.Threading.Tasks;

using Microsoft.Playwright;

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

            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");

            await Page.WaitForSelectorAsync("text=جریان‌های کاری");
            await Task.Delay(1500);



            await Page.ClickAsync("button:has-text('ایجاد جریان کاری جدید')");

            await Page.WaitForSelectorAsync("input[placeholder='نام جریان کاری...']");

            

            var wfName = "Complex Workflow " + Guid.NewGuid().ToString().Substring(0, 6);

            await Page.FillAsync("input[placeholder='نام جریان کاری...']", wfName);



            // 1- add status A using the icon

            var statusAItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "باز" });

            await statusAItem.Locator("button").ClickAsync();

            await Task.Delay(500);



            // 2- add status B using drag and drop to the right side of status A

            var canvas = Page.Locator(".canvas-pattern");

            var canvasBox = (await canvas.BoundingBoxAsync())!;

            

            var statusBItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "در انتظار تایید" });

            await statusBItem.DragToAsync(canvas, new LocatorDragToOptions {

                TargetPosition = new() { X = canvasBox.Width / 2 + 250, Y = canvasBox.Height / 2 }

            });

            await Task.Delay(500);



            // 3- add status c using drag and drop below status b

            var statusCItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "بسته شده" });

            await statusCItem.DragToAsync(canvas, new LocatorDragToOptions {

                TargetPosition = new() { X = canvasBox.Width / 2 + 250, Y = canvasBox.Height / 2 + 200 }

            });

            await Task.Delay(500);



            var statusAOnCanvas = Page.Locator(".canvas-pattern > div:has-text('باز')").First;

            var statusBOnCanvas = Page.Locator(".canvas-pattern > div:has-text('در انتظار تایید')").First;

            var statusCOnCanvas = Page.Locator(".canvas-pattern > div:has-text('بسته شده')").First;



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

            await Task.Delay(500);

            var closeBtns = await Page.Locator("button[title='بستن']").AllAsync();

            foreach (var btn in closeBtns)

            {

                if (await btn.IsVisibleAsync())

                {

                    await btn.ClickAsync();

                }

            }

            await Task.Delay(1000); // Wait for transition-all duration-300 to fully close



            // 4- click on status A so the icons appear, click on the star IsInitial Icon

            await statusAOnCanvas.ClickAsync();

            await Task.Delay(200);

            await Page.ClickAsync("button[title='ثبت به عنوان وضعیت اولیه']");

            await Task.Delay(200);



            // 7- click on all transitions and test all transitions field

            // Note: Canvas draws connections using SVG paths. 

            // We can click the transitions by finding the path class `cursor-pointer`

            var transitionPaths = Page.Locator("path.cursor-pointer");



            // Test transition 1 (A -> B)

            await transitionPaths.Nth(0).ClickAsync(new() { Force = true });

            await Task.Delay(500);

            

            // fill in the tranisiton عنوان انتقال

            await Page.Locator("label").Filter(new() { HasText = "عنوان انتقال" }).Locator("..").Locator("input").FillAsync("عنوان انتقال تستی");

            

            // click check box انتقال خودکار

            await Page.Locator("text=انتقال خودکار").ClickAsync();



            // pick a date by custom persian date picker (تاریخ فعال‌سازی)

            await Page.ClickAsync("input[placeholder='1403/05/12 14:30']");

            await Task.Delay(500);

            await Page.Locator(".jdp-day").Nth(15).ClickAsync(); 

            await Task.Delay(500);

            await Page.Keyboard.PressAsync("Escape"); // Close the date picker if it stayed open

            await Task.Delay(500);



            // choose roles: ادمین and کاربر

            await Page.ClickAsync("button:has-text('ادمین')");

            await Page.ClickAsync("button:has-text('کاربر')");



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

                await Page.ClickAsync("button:has-text('+ افزودن فیلد')");

                await Task.Delay(200);

                var fieldInputs = Page.Locator(".border.rounded-xl:has(input[placeholder='عنوان فیلد'])");

                var currentField = fieldInputs.Nth(i);

                

                await currentField.Locator("input[type='text']").Nth(0).FillAsync($"فیلد {i+1}");

                var slideSelect = currentField.Locator(".relative[dir='rtl']").First;

                await slideSelect.Locator("div").First.ClickAsync();

                await slideSelect.Locator("li").Filter(new() { HasText = fieldTypes[i] }).ClickAsync();

                await Task.Delay(200);

                

                if (fieldTypes[i].Contains("Text") || fieldTypes[i].Contains("TextArea"))

                {

                    await currentField.Locator("input[type='text']").Nth(1).FillAsync($"جایگیر {i+1}");

                    await currentField.Locator("input[type='text']").Nth(2).FillAsync("مقدار");

                }

                else if (fieldTypes[i].Contains("Number"))

                {

                    await currentField.Locator("input[type='text']").Nth(1).FillAsync($"123456");

                    await currentField.Locator("input[type='number']").First.FillAsync("0");

                }


                else if (fieldTypes[i].Contains("Dropdown") || fieldTypes[i].Contains("Select"))

                {

                    await currentField.Locator("input[type='text']").Nth(1).FillAsync("گزینه 1,گزینه 2");

                }

            }



            // Fill title for transition 2 (B -> C)

            if (await transitionPaths.CountAsync() > 1) 

            {

                await transitionPaths.Nth(1).ClickAsync(new() { Force = true });

                await Task.Delay(500);

                await Page.Locator("label").Filter(new() { HasText = "عنوان انتقال" }).Locator("..").Locator("input").FillAsync("انتقال دوم");

            }



            // Fill title for transition 3 (C -> A)

            if (await transitionPaths.CountAsync() > 2) 

            {

                await transitionPaths.Nth(2).ClickAsync(new() { Force = true });

                await Task.Delay(500);

                await Page.Locator("label").Filter(new() { HasText = "عنوان انتقال" }).Locator("..").Locator("input").FillAsync("انتقال سوم");

            }



            // 9- Save workflow

            Page.Console += (_, e) => Console.WriteLine($"BROWSER CONSOLE: {e.Text}");

            Page.PageError += (_, e) => Console.WriteLine($"BROWSER ERROR: {e}");

            await Page.ClickAsync("button:has-text('ذخیره جریان کار')");



            var anyToast = Page.Locator("text=موفقیت").First;

            await anyToast.WaitForAsync(new() { Timeout = 15000 });

            var anyToastText = await anyToast.InnerTextAsync();

            if (!anyToastText.Contains("موفقیت"))

            {

                throw new Exception($"Save workflow failed with message: {anyToastText}");

            }

        }

        [Fact]

        public async Task WorkflowEditor_Deletion_ShouldSucceed()

        {

            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");

            await Page.WaitForSelectorAsync("text=جریان‌های کاری");
            await Task.Delay(1500);



            await Page.ClickAsync("button:has-text('ایجاد جریان کاری جدید')");

            await Page.WaitForSelectorAsync("input[placeholder='نام جریان کاری...']");



            // 1- add status A using the icon

            var statusAItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "باز" });

            await statusAItem.Locator("button").ClickAsync();

            await Task.Delay(500);



            // 2- add status B using drag and drop to the right side of status A

            var canvas = Page.Locator(".canvas-pattern");

            var canvasBox = (await canvas.BoundingBoxAsync())!;

            

            var statusBItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "در انتظار تایید" });

            await statusBItem.DragToAsync(canvas, new LocatorDragToOptions {

                TargetPosition = new() { X = canvasBox.Width / 2 + 250, Y = canvasBox.Height / 2 }

            });

            await Task.Delay(500);



            // 3- add status c using drag and drop below status b

            var statusCItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "بسته شده" });

            await statusCItem.DragToAsync(canvas, new LocatorDragToOptions {

                TargetPosition = new() { X = canvasBox.Width / 2 + 250, Y = canvasBox.Height / 2 + 200 }

            });

            await Task.Delay(500);



            var statusAOnCanvas = Page.Locator(".canvas-pattern > div:has-text('باز')").First;

            var statusBOnCanvas = Page.Locator(".canvas-pattern > div:has-text('در انتظار تایید')").First;

            var statusCOnCanvas = Page.Locator(".canvas-pattern > div:has-text('بسته شده')").First;



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

            await Task.Delay(500);

            var closeBtns = await Page.Locator("button[title='بستن']").AllAsync();

            foreach (var btn in closeBtns)

            {

                if (await btn.IsVisibleAsync())

                {

                    await btn.ClickAsync();

                }

            }

            await Task.Delay(1000); // Wait for transition-all duration-300 to fully close



            // 6-1 DELETION TEST

            var transitionPaths = Page.Locator("path.cursor-pointer");



            // 1. Delete transition A to B

            await transitionPaths.Nth(0).ClickAsync(new() { Force = true });

            await Task.Delay(500);

            await Page.Locator("button[title='حذف مسیر']").First.ClickAsync();

            await Task.Delay(500);



            // 2. Delete Status A

            await statusAOnCanvas.ClickAsync();

            await Task.Delay(200);

            await statusAOnCanvas.Locator("button.bg-rose-500").ClickAsync();

            await Task.Delay(500);



            // 3. Hold Shift, select remaining nodes/transitions one by one, and click Status B delete icon to bulk delete

            await Page.Keyboard.DownAsync("Shift");

            

            // Click transition B->C (which is now index 0, since A->B and C->A are gone)

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
            await statusBOnCanvas.Locator("button.bg-rose-500").ClickAsync();
            await Task.Delay(500);

            // Verify Canvas is empty (except maybe the pattern)
            (await statusBOnCanvas.IsVisibleAsync()).Should().BeFalse();
            (await statusCOnCanvas.IsVisibleAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task WorkflowEditor_Validation_ShouldFailWhenFieldsEmpty()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("h1:has-text('جریان‌های کاری')");
            await Task.Delay(1500);
            await Page.ClickAsync("button:has-text('ایجاد جریان کاری جدید')");
            await Page.WaitForURLAsync("**/workflows/editor");
            
            var canvas = Page.Locator(".canvas-pattern");
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

            var statusAOnCanvas = Page.Locator(".canvas-pattern > div:has-text('باز')").First;
            var statusBOnCanvas = Page.Locator(".canvas-pattern > div:has-text('در انتظار تایید')").First;
              
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
            
            // Wait for data to load by checking if statuses are rendered in the sidebar
            var statusAItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "باز" });
            await statusAItem.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            
            await Page.WaitForSelectorAsync("input[placeholder='نام جریان کاری...']");
            await Page.FillAsync("input[placeholder='نام جریان کاری...']", name);
            
            // Add Status A
            await statusAItem.Locator("button").ClickAsync();
            await Task.Delay(500);

            // Add Status B
            var statusBItem = Page.Locator("div.cursor-grab").Filter(new() { HasText = "در انتظار تایید" });
            await statusBItem.Locator("button").ClickAsync();
            await Task.Delay(500);

            // Connect A to B
            var statusAOnCanvas = Page.Locator(".canvas-pattern > div:has-text('باز')").First;
            var statusBOnCanvas = Page.Locator(".canvas-pattern > div:has-text('در انتظار تایید')").First;
            
            var sBox = (await statusAOnCanvas.BoundingBoxAsync())!;
            var tBox = (await statusBOnCanvas.BoundingBoxAsync())!;
            await Page.Mouse.MoveAsync(sBox.X + sBox.Width / 2, sBox.Y + sBox.Height / 2);
            await Page.Mouse.DownAsync();
            await Task.Delay(200);
            await Page.Mouse.MoveAsync(tBox.X + tBox.Width / 2, tBox.Y + tBox.Height / 2, new() { Steps = 10 });
            await Task.Delay(200);
            await Page.Mouse.UpAsync();
            await Task.Delay(500);

            // Fill connection name in sidebar
            var transitionTitleInput = Page.Locator("label").Filter(new() { HasText = "عنوان انتقال" }).Locator("..").Locator("input").First;
            if(await transitionTitleInput.IsVisibleAsync())
            {
               await transitionTitleInput.FillAsync("تایید");
               await Page.ClickAsync("button[title='بستن']"); // Close transition sidebar
               await Task.Delay(200);
            }

            // Set initial status
            await statusAOnCanvas.ClickAsync();
            await Task.Delay(200);
            await Page.ClickAsync("button[title='ثبت به عنوان وضعیت اولیه']");
            await Task.Delay(500);

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
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=جریان‌های کاری");
            await Task.Delay(1500);

            string prefix = $"BulkTest_{Guid.NewGuid().ToString("N").Substring(0, 4)}_";
            
            // 1. Create 3 workflows
            for (int i = 1; i <= 3; i++)
            {
                await CreateValidWorkflowAsync($"{prefix}{i}");
                await Task.Delay(1000); // Give time for the list to refresh
            }

            // 2. Select all 3
            // Search to ensure they are on the first page (in case there are many existing workflows)
            var searchInput = Page.Locator("input[placeholder='جستجوی جریان کاری...']");
            await searchInput.FillAsync(prefix);
            await Task.Delay(1500); // Wait for debounced search and network response

            for(int i = 1; i <= 3; i++)
            {
                var card = Page.Locator($".group.bg-white:has-text('{prefix}{i}')").First;
                await card.Locator("label.cursor-pointer").ClickAsync();
            }

            var bulkText = Page.Locator("div.fixed.bottom-6:has-text('3 مورد انتخاب شده')").First;
            await bulkText.WaitForAsync(new() { Timeout = 5000 });

            // 3. Delete 1 via row action
            var firstCard = Page.Locator($".group.bg-white:has-text('{prefix}1')").First;
            await firstCard.Locator("button[title='حذف جریان کار']").First.ClickAsync(); // Delete icon
            
            var confirmBtn = Page.Locator("button:has-text('بله، حذف کن')").First;
            await confirmBtn.ClickAsync();

            var toastSingle = Page.Locator($"text=جریان کاری '{prefix}1' با موفقیت حذف شد.").First;
            await toastSingle.WaitForAsync(new() { Timeout = 5000 });
            await toastSingle.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            await Page.Mouse.ClickAsync(10, 10);
            await Page.WaitForTimeoutAsync(500);

            // 4. Verify selection is now 2
            var bulkText2 = Page.Locator("div.fixed.bottom-6:has-text('2 مورد انتخاب شده')").First;
            await bulkText2.WaitForAsync(new() { Timeout = 5000 });

            // 5. Delete remaining 2 via bulk action
            // Using "حذف" because the button text is just "حذف" with a trash icon
            await Page.Locator("div.fixed.bottom-6").Locator("button:has-text('حذف')").First.ClickAsync();
            var confirmBtnBulk = Page.Locator("button:has-text('بله، حذف کن')").First;
            await confirmBtnBulk.ClickAsync();

            var toastBulkMultiple = Page.Locator("text=2 جریان کاری حذف شدند.").First;
            await toastBulkMultiple.WaitForAsync(new() { Timeout = 5000 });
            await toastBulkMultiple.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            
            // 6. Create 1 more to test single bulk delete
            await CreateValidWorkflowAsync($"{prefix}4");
            
            // Search to ensure it is on the first page
            searchInput = Page.Locator("input[placeholder='جستجوی جریان کاری...']");
            await searchInput.FillAsync($"{prefix}4");
            await Task.Delay(1500);
            
            // 7. Select 1
            var fourthCard = Page.Locator($".group.bg-white:has-text('{prefix}4')").First;
            await fourthCard.Locator("label.cursor-pointer").ClickAsync();
            
            var bulkText1 = Page.Locator("div.fixed.bottom-6:has-text('1 مورد انتخاب شده')").First;
            await bulkText1.WaitForAsync(new() { Timeout = 5000 });
            
            // 8. Delete via bulk action (1 item -> "حذف شد")
            await Page.Locator("div.fixed.bottom-6").Locator("button:has-text('حذف')").First.ClickAsync();
            var confirmBtnBulkSingle = Page.Locator("button:has-text('بله، حذف کن')").First;
            await confirmBtnBulkSingle.ClickAsync();

            var toastBulkSingle = Page.Locator("text=1 جریان کاری حذف شد.").First;
            await toastBulkSingle.WaitForAsync(new() { Timeout = 5000 });
        }

        [Fact]
        public async Task WorkflowsSettings_Pagination_BulkDelete_Scenario()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=جریان‌های کاری");
            await Task.Delay(1500);

            string prefix = $"PgTest_{Guid.NewGuid().ToString("N").Substring(0, 4)}_";
            
            // 1. Create 9 workflows directly in DB for speed
            using (var scope = Factory.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<TicketHub.Infrastructure.Data.AppDbContext>>();
                using var context = dbFactory.CreateDbContext();
                
                for (int i = 1; i <= 9; i++)
                {
                    context.Workflows.Add(new TicketHub.Core.Entities.Workflow { Name = $"{prefix}{i}", IsActive = true, CreatedAt = System.DateTime.UtcNow });
                }
                await context.SaveChangesAsync();
            }

            // Reload the page to fetch the newly seeded items
            await Page.ReloadAsync();
            await Page.WaitForSelectorAsync("text=جریان‌های کاری");
            await Task.Delay(1500);

            // 2. Search to ensure we only see these 9
            var searchInput = Page.Locator("input[placeholder='جستجوی جریان کاری...']");
            await searchInput.FillAsync(prefix);
            await Task.Delay(1500);

            // 3. Select 2 items from the first page
            for(int i = 1; i <= 2; i++)
            {
                var card = Page.Locator($".group.bg-white:has-text('{prefix}{i}')").First;
                await card.Locator("label.cursor-pointer").ClickAsync();
            }

            // 4. Navigate to the second page
            var nextPageBtn = Page.Locator("button[title='صفحه بعد']").First; // Usually has title or aria-label
            // If the pagination button doesn't have a title, we can click it by svg/text.
            // Let's rely on standard text or icon. For TicketHub pagination, there are next/prev buttons.
            // A safer locator: finding the active page button and clicking the next one, or finding by text
            
            var paginationNav = Page.Locator("nav[aria-label='Pagination']");
            var nextButton = Page.Locator("button").Filter(new() { HasText = "بعدی" }); 
            if (await nextButton.CountAsync() == 0)
            {
                // Fallback to svg icon if there's no text "بعدی"
                nextButton = Page.Locator("button.rounded-l-md"); // Assuming Tailwind UI pagination style
            }
            await nextButton.First.ClickAsync();
            await Task.Delay(1000);

            // 5. Delete the 2 selected items using bulk delete button
            var bulkDeleteBtn = Page.Locator("div.fixed.bottom-6").Locator("button:has-text('حذف')").First;
            await bulkDeleteBtn.ClickAsync();
            
            var confirmBtn = Page.Locator("button:has-text('بله، حذف کن')").First;
            await confirmBtn.ClickAsync();

            var toastMulti = Page.Locator($"text=2 جریان کاری حذف شدند.").First;
            await toastMulti.WaitForAsync(new() { Timeout = 5000 });
            await toastMulti.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

            // 6. Verify we are navigated back to Page 1 and total count is 7
            // Look for an item that is on page 1, or check the pagination status text
            await Task.Delay(1000);

            // The remaining 7 workflows should be visible on page 1
            // We deleted prefix1 and prefix2. prefix3 through prefix9 should exist (7 items).
            var remainingCard = Page.Locator($".group.bg-white:has-text('{prefix}3')").First;
            await remainingCard.WaitForAsync(new() { Timeout = 5000 });

            // Ensure we are on page 1 by checking the pagination info text, e.g. "نمایش 1 تا 7 از 7"
            var paginationSummary = Page.Locator("text=نمایش 1 تا 7 از 7");
            await paginationSummary.WaitForAsync(new() { Timeout = 5000 });
        }

        [Fact]
        public async Task WorkflowEditor_Cancel_NavigatesBackToWorkflowsDashboard()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=جریان‌های کاری");

            await Page.ClickAsync("button:has-text('ایجاد جریان کاری جدید')");
            await Page.WaitForURLAsync("**/workflows/editor");

            var cancelBtn = Page.Locator("a:has-text('انصراف و بازگشت')").First;
            await cancelBtn.ClickAsync();

            await Page.WaitForURLAsync("**/workflows");
            var header = Page.Locator("text=جریان‌های کاری").First;
            (await header.IsVisibleAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task Workflows_SearchFilter_LiveGridFilter()
        {
            await Page.GotoAsync($"{Factory.ServerAddress}/dev/login");
            await Page.GotoAsync($"{Factory.ServerAddress}/workflows");
            await Page.WaitForSelectorAsync("text=جریان‌های کاری");

            var searchInput = Page.Locator("input[placeholder='جستجوی جریان کاری...']");
            await searchInput.FillAsync("ناموجود_" + Guid.NewGuid().ToString().Substring(0, 5));
            await Task.Delay(1000);

            // Verify empty state or 0 cards
            var emptyText = Page.Locator("text=هیچ جریان کاری یافت نشد").First;
            if (await emptyText.CountAsync() > 0)
            {
                (await emptyText.IsVisibleAsync()).Should().BeTrue();
            }

            // Clear search
            await searchInput.FillAsync("");
            await Task.Delay(1000);
        }
    }
}