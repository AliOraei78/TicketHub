using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using BCrypt.Net;
using TicketHub.Core.Entities;

namespace TicketHub.Infrastructure.Data
{
    public static class DbInitializer
    {
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public static async Task InitializeAsync(AppDbContext context, IConfiguration? configuration = null)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Apply migrations automatically if any exist
                await context.Database.MigrateAsync();

                // 1. Seed Roles independently
                // Check each role individually so missing ones are added even if others exist
                var defaultRoles = new[] { "ادمین", "پشتیبان", "کاربر", "مسئول فنی" };
                foreach (var roleName in defaultRoles)
                {
                    if (!await context.Roles.AnyAsync(r => r.Name == roleName))
                    {
                        await context.Roles.AddAsync(new Role { Name = roleName });
                    }
                }
                await context.SaveChangesAsync();

                // 2. Seed Admin User independently from Configuration / Environment Variables
                var adminEmail = configuration?["InitialAdmin:Email"];
                var adminPassword = configuration?["InitialAdmin:Password"];
                var adminName = configuration?["InitialAdmin:Name"] ?? "مدیر کل سیستم";
                var adminPhone = configuration?["InitialAdmin:PhoneNumber"] ?? "";

                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

                    if (adminUser == null)
                    {
                        if (!string.IsNullOrWhiteSpace(adminPassword))
                        {
                            adminUser = new User
                            {
                                Name = adminName,
                                Email = adminEmail,
                                Password = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 11),
                                CreatedAt = DateTime.UtcNow,
                                PhoneNumber = adminPhone,
                                IsActive = true,
                                IsConfirmed = true
                            };

                            await context.Users.AddAsync(adminUser);
                            await context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        bool isUpdated = false;
                        if (!string.IsNullOrWhiteSpace(adminName) && adminUser.Name != adminName)
                        {
                            adminUser.Name = adminName;
                            isUpdated = true;
                        }
                        if (!string.IsNullOrWhiteSpace(adminPhone) && adminUser.PhoneNumber != adminPhone)
                        {
                            adminUser.PhoneNumber = adminPhone;
                            isUpdated = true;
                        }
                        if (!string.IsNullOrWhiteSpace(adminPassword) && !BCrypt.Net.BCrypt.Verify(adminPassword, adminUser.Password))
                        {
                            adminUser.Password = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 11);
                            isUpdated = true;
                        }
                        if (isUpdated)
                        {
                            await context.SaveChangesAsync();
                        }
                    }

                    // 3. Assign Admin Role to Admin User independently
                    var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "ادمین");
                    if (adminRole != null && adminUser != null)
                    {
                        var userHasRole = await context.UserRoles
                            .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);

                        if (!userHasRole)
                        {
                            await context.UserRoles.AddAsync(new UserRole
                            {
                                UserId = adminUser.Id,
                                RoleId = adminRole.Id
                            });
                            await context.SaveChangesAsync();
                        }
                    }
                }

                // 3.1. Seed Additional Users for Remaining Roles (پشتیبان, مسئول فنی, کاربر, مهمان)
                var supportPass = configuration?["InitialSeedUsers:SupportPassword"] ?? "Support@123456";
                var techPass = configuration?["InitialSeedUsers:TechPassword"] ?? "Tech@123456";
                var userPass = configuration?["InitialSeedUsers:UserPassword"] ?? "User@123456";
                var guestPass = configuration?["InitialSeedUsers:GuestPassword"] ?? "guest";

                var defaultUserSeedData = new[]
                {
                    new { Name = "کاربر پشتیبان", Email = "support@tickethub.io", RoleName = "پشتیبان", Phone = "09120000001", Pass = supportPass },
                    new { Name = "مسئول فنی سیستم", Email = "tech@tickethub.io", RoleName = "مسئول فنی", Phone = "09120000002", Pass = techPass },
                    new { Name = "کاربر عادی", Email = "user@tickethub.io", RoleName = "کاربر", Phone = "09120000003", Pass = userPass },
                    new { Name = "کاربر مهمان", Email = "guest@tickethub.io", RoleName = "کاربر", Phone = "09120000004", Pass = guestPass }
                };

                foreach (var seed in defaultUserSeedData)
                {
                    var userObj = await context.Users.FirstOrDefaultAsync(u => u.Email == seed.Email);
                    if (userObj == null)
                    {
                        userObj = new User
                        {
                            Name = seed.Name,
                            Email = seed.Email,
                            Password = BCrypt.Net.BCrypt.HashPassword(seed.Pass, workFactor: 11),
                            CreatedAt = DateTime.UtcNow,
                            PhoneNumber = seed.Phone,
                            IsActive = true,
                            IsConfirmed = true
                        };
                        await context.Users.AddAsync(userObj);
                        await context.SaveChangesAsync();
                    }

                    var roleObj = await context.Roles.FirstOrDefaultAsync(r => r.Name == seed.RoleName);
                    if (roleObj != null && userObj != null)
                    {
                        var hasRole = await context.UserRoles
                            .AnyAsync(ur => ur.UserId == userObj.Id && ur.RoleId == roleObj.Id);

                        if (!hasRole)
                        {
                            await context.UserRoles.AddAsync(new UserRole
                            {
                                UserId = userObj.Id,
                                RoleId = roleObj.Id
                            });
                            await context.SaveChangesAsync();
                        }
                    }
                }

                // 4. Seed Statuses independently
                // Moved completely outside of the Admin User check block
                var defaultStatuses = new[]
                {
                new Status { Name = "باز", ColorCode = "#10B981" },
                new Status { Name = "در دست اقدام", ColorCode = "#3B82F6" },
                new Status { Name = "در انتظار تایید", ColorCode = "#F59E0B" },
                new Status { Name = "انجام شده", ColorCode = "#059669" },
                new Status { Name = "بسته شده", ColorCode = "#6B7280" },
                new Status { Name = "لغو شده", ColorCode = "#EF4444" },
                new Status { Name = "بررسی مجدد", ColorCode = "#3bd1f7" },
                new Status { Name = "تعیین مهلت اقدام", ColorCode = "#b227ce" }
            };

                foreach (var status in defaultStatuses)
                {
                    // Add the status only if it doesn't already exist in the database
                    if (!await context.Statuses.AnyAsync(s => s.Name == status.Name))
                    {
                        await context.Statuses.AddAsync(status);
                    }
                }

                // 5. Seed Categories
                var defaultCategories = new[]
                {
                new Category { Name = "عمومی" },
                new Category { Name = "فنی" },
                new Category { Name = "مالی" },
                new Category { Name = "باگ" },
                new Category { Name = "درخواست تغییر" }
            };

                foreach (var category in defaultCategories)
                {
                    if (!await context.Categories.AnyAsync(c => c.Name == category.Name))
                    {
                        await context.Categories.AddAsync(category);
                    }
                }
                await context.SaveChangesAsync();

                var generalProj = await context.Projects.FirstOrDefaultAsync(p => p.Name == "عمومی");
                if (generalProj != null)
                {
                    var allCats = await context.Categories.ToListAsync();
                    foreach (var c in allCats)
                    {
                        if (!await context.CategoryProjects.AnyAsync(cp => cp.CategoryId == c.Id && cp.ProjectId == generalProj.Id))
                        {
                            await context.CategoryProjects.AddAsync(new CategoryProject { CategoryId = c.Id, ProjectId = generalProj.Id });
                        }
                    }
                    await context.SaveChangesAsync();
                }

                // 6. Seed Priorities
                var defaultPriorities = new[]
                {
                new Priority { Name = "کم", Level = 1, ColorCode = "#10B981" },     // سبز
                new Priority { Name = "متوسط", Level = 2, ColorCode = "#3B82F6" },  // آبی
                new Priority { Name = "زیاد", Level = 3, ColorCode = "#F59E0B" },   // نارنجی
                new Priority { Name = "بحرانی", Level = 4, ColorCode = "#EF4444" }  // قرمز
            };

                foreach (var priority in defaultPriorities)
                {
                    if (!await context.Priorities.AnyAsync(p => p.Name == priority.Name))
                    {
                        await context.Priorities.AddAsync(priority);
                    }
                }

                // 7. Seed FieldTypes
                var defaultFieldTypes = new[]
                {
                new FieldType { Type = "متن کوتاه (Text)" },
                new FieldType { Type = "متن طولانی (TextArea)" },
                new FieldType { Type = "عدد (Number)" },
                new FieldType { Type = "تاریخ (Date)" },
                new FieldType { Type = "لیست کشویی (Dropdown)" },
                new FieldType { Type = "لیست کشویی چند گزینه‌ای (MultipleDropdown)" }, // مطابقت با Enum
                new FieldType { Type = "چک‌باکس (Checkbox)" },
                new FieldType { Type = "آپلود فایل (File)" },
                new FieldType { Type = "انتخاب رنگ (ColorPicker)" }            };

                foreach (var fieldType in defaultFieldTypes)
                {
                    if (!await context.FieldTypes.AnyAsync(f => f.Type == fieldType.Type))
                    {
                        await context.FieldTypes.AddAsync(fieldType);
                    }
                }
                await context.SaveChangesAsync();

                // 8. Seed Workflows
                if (!await context.Workflows.AnyAsync(w => w.Name == "عمومی" || w.Name == "جریان کاری عمومی"))
                {
                    // واکشی اطلاعات پایه به صورت Dictionary برای استفاده آسان و دقیق
                    var statuses = await context.Statuses.ToDictionaryAsync(s => s.Name, s => s.Id);
                    var fieldTypes = await context.FieldTypes.ToDictionaryAsync(f => f.Type, f => f.Id);
                    var roles = await context.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);

                    var nOpen = Guid.NewGuid();
                    var nPending1 = Guid.NewGuid();
                    var nCancelled = Guid.NewGuid();
                    var nDeadline = Guid.NewGuid();
                    var nInProgress = Guid.NewGuid();
                    var nPending2 = Guid.NewGuid();
                    var nReview = Guid.NewGuid();
                    var nDone = Guid.NewGuid();
                    var nPending3 = Guid.NewGuid();
                    var nClosed = Guid.NewGuid();

                    var wsOpen = new WorkflowStatus { StatusId = statuses["باز"], NodeId = nOpen, PositionX = 114.6, PositionY = 316.8, IsInitial = true };
                    var wsPending1 = new WorkflowStatus { StatusId = statuses["در انتظار تایید"], NodeId = nPending1, PositionX = 394.0, PositionY = 316.4 };
                    var wsCancelled = new WorkflowStatus { StatusId = statuses["لغو شده"], NodeId = nCancelled, PositionX = 397.0, PositionY = 584.2 };
                    var wsDeadline = new WorkflowStatus { StatusId = statuses["تعیین مهلت اقدام"], NodeId = nDeadline, PositionX = 634.0, PositionY = 316.6 };
                    var wsInProgress = new WorkflowStatus { StatusId = statuses["در دست اقدام"], NodeId = nInProgress, PositionX = 888.6, PositionY = 313.8 };
                    var wsPending2 = new WorkflowStatus { StatusId = statuses["در انتظار تایید"], NodeId = nPending2, PositionX = 1022.1, PositionY = 516.0 };
                    var wsReview = new WorkflowStatus { StatusId = statuses["بررسی مجدد"], NodeId = nReview, PositionX = 1024.8, PositionY = 133.8 };
                    var wsDone = new WorkflowStatus { StatusId = statuses["انجام شده"], NodeId = nDone, PositionX = 1155.0, PositionY = 311.0 };
                    var wsPending3 = new WorkflowStatus { StatusId = statuses["در انتظار تایید"], NodeId = nPending3, PositionX = 1395.3, PositionY = 312.8 };
                    var wsClosed = new WorkflowStatus { StatusId = statuses["بسته شده"], NodeId = nClosed, PositionX = 1604.8, PositionY = 314.2 };

                    var workflow = new Workflow
                    {
                        Name = "عمومی",
                        Description = "جریان کاری پیش‌فرض سیستم",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,

                        WorkflowStatuses = new List<WorkflowStatus>
                    {
                        wsOpen, wsPending1, wsCancelled, wsDeadline, wsInProgress, wsPending2, wsReview, wsDone, wsPending3, wsClosed
                    },

                        Transitions = new List<Transition>
                    {
                        // 1. مشاهده درخواست (باز -> در انتظار تایید)
                        new Transition
                        {
                            Name = "مشاهده درخواست",
                            FromStatus = wsOpen,
                            ToStatus = wsPending1,
                            SourcePort = "Right",
                            TargetPort = "Left",
                            FromNodeId = nOpen,
                            ToNodeId = nPending1,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["ادمین"] },
                                new TransitionRole { RoleId = roles["پشتیبان"] }
                            },
                            TransitionFields = new List<TransitionField>
                            {
                                new TransitionField
                                {
                                    FieldName = "پیوست مستندات:",
                                    FieldTypeId = fieldTypes["آپلود فایل (File)"],
                                    IsRequired = false,
                                    SortOrder = 2,
                                    IsActive = true
                                }
                            }
                        },

                        // 2. رد درخواست (در انتظار تایید -> لغو شده)
                        new Transition
                        {
                            Name = "رد درخواست",
                            FromStatus = wsPending1,
                            ToStatus = wsCancelled,
                            SourcePort = "Bottom",
                            TargetPort = "Top",
                            FromNodeId = nPending1,
                            ToNodeId = nCancelled,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["ادمین"] },
                                new TransitionRole { RoleId = roles["پشتیبان"] }
                            },
                            TransitionFields = new List<TransitionField>
                            {
                                new TransitionField
                                {
                                    FieldName = "علت رد:",
                                    FieldTypeId = fieldTypes["لیست کشویی (Dropdown)"],
                                    IsRequired = true,
                                    SortOrder = 0,
                                    Options = "درخواست تکراری, ثبت درخواستی دیگر, عدم پاسخگویی در زمان مناسب",
                                    IsActive = true
                                }
                            }
                        },

                        // 3. اجراع جهت بررسی و اقدام (در انتظار تایید -> تعیین مهلت اقدام)
                        new Transition
                        {
                            Name = "اجراع جهت بررسی و اقدام",
                            FromStatus = wsPending1,
                            ToStatus = wsDeadline,
                            SourcePort = "Right",
                            TargetPort = "Left",
                            FromNodeId = nPending1,
                            ToNodeId = nDeadline,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["پشتیبان"] },
                                new TransitionRole { RoleId = roles["ادمین"] }
                            },
                            TransitionFields = new List<TransitionField>()
                        },

                        // 4. تعیین مهلت اقدام (تعیین مهلت اقدام -> در دست اقدام)
                        new Transition
                        {
                            Name = "تعیین مهلت اقدام",
                            FromStatus = wsDeadline,
                            ToStatus = wsInProgress,
                            SourcePort = "Right",
                            TargetPort = "Left",
                            FromNodeId = nDeadline,
                            ToNodeId = nInProgress,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["ادمین"] }
                            },
                            TransitionFields = new List<TransitionField>
                            {
                                new TransitionField
                                {
                                    FieldName = "مهلت اقدام",
                                    FieldTypeId = fieldTypes["تاریخ (Date)"],
                                    IsRequired = false,
                                    SortOrder = 0,
                                    IsActive = true
                                }
                            }
                        },

                        // 5. نیاز به اطلاعات بیشتر (در دست اقدام -> در انتظار تایید)
                        new Transition
                        {
                            Name = "نیاز به اطلاعات بیشتر",
                            FromStatus = wsInProgress,
                            ToStatus = wsPending2,
                            SourcePort = "Bottom",
                            TargetPort = "Left",
                            FromNodeId = nInProgress,
                            ToNodeId = nPending2,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["مسئول فنی"] },
                                new TransitionRole { RoleId = roles["ادمین"] }
                            },
                            TransitionFields = new List<TransitionField>()
                        },

                        // 6. ارائه اطلاعات تکمیلی (در انتظار تایید -> در دست اقدام)
                        new Transition
                        {
                            Name = "ارائه اطلاعات تکمیلی",
                            FromStatus = wsPending2,
                            ToStatus = wsInProgress,
                            SourcePort = "Top",
                            TargetPort = "Right",
                            FromNodeId = nPending2,
                            ToNodeId = nInProgress,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["کاربر"] },
                                new TransitionRole { RoleId = roles["ادمین"] }
                            },
                            TransitionFields = new List<TransitionField>()
                        },

                        // 7. لغو درخواست (در دست اقدام -> لغو شده)
                        new Transition
                        {
                            Name = "لغو درخواست",
                            FromStatus = wsInProgress,
                            ToStatus = wsCancelled,
                            SourcePort = "Bottom",
                            TargetPort = "Right",
                            FromNodeId = nInProgress,
                            ToNodeId = nCancelled,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["ادمین"] }
                            },
                            TransitionFields = new List<TransitionField>
                            {
                                new TransitionField
                                {
                                    FieldName = "علت لغو:",
                                    FieldTypeId = fieldTypes["لیست کشویی (Dropdown)"],
                                    IsRequired = true,
                                    SortOrder = 0,
                                    Options = "درخواست تکراری, ثبت درخواستی دیگر, عدم پاسخگویی در زمان مناسب",
                                    IsActive = true
                                }
                            }
                        },

                        // 8. اتمام درخواست و ارسال جهت بازبینی (در دست اقدام -> انجام شده)
                        new Transition
                        {
                            Name = "اتمام درخواست و ارسال جهت بازبینی",
                            FromStatus = wsInProgress,
                            ToStatus = wsDone,
                            SourcePort = "Right",
                            TargetPort = "Left",
                            FromNodeId = nInProgress,
                            ToNodeId = nDone,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["مسئول فنی"] },
                                new TransitionRole { RoleId = roles["ادمین"] }
                            },
                            TransitionFields = new List<TransitionField>()
                        },

                        // 9. ارجاع خودکار بدلیل عدم اقدام در زمان مقرر (در دست اقدام -> بررسی مجدد)
                        new Transition
                        {
                            Name = "ارجاع خودکار بدلیل عدم اقدام در زمان مقرر",
                            FromStatus = wsInProgress,
                            ToStatus = wsReview,
                            SourcePort = "Right",
                            TargetPort = "Bottom",
                            FromNodeId = nInProgress,
                            ToNodeId = nReview,
                            IsAutomated = 1,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["ادمین"] }
                            },
                            TransitionFields = new List<TransitionField>()
                        },

                        // 10. ارسال جهت بررسی و تخصیص مهلت زمانی جدید (در دست اقدام -> بررسی مجدد)
                        new Transition
                        {
                            Name = "ارسال جهت بررسی و تخصیص مهلت زمانی جدید",
                            FromStatus = wsInProgress,
                            ToStatus = wsReview,
                            SourcePort = "Top",
                            TargetPort = "Left",
                            FromNodeId = nInProgress,
                            ToNodeId = nReview,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>(),
                            TransitionFields = new List<TransitionField>
                            {
                                new TransitionField
                                {
                                    FieldName = "علت درخواست مهلت زمانی بیشتر:",
                                    FieldTypeId = fieldTypes["متن طولانی (TextArea)"],
                                    IsRequired = true,
                                    SortOrder = 0,
                                    IsActive = true
                                }
                            }
                        },

                        // 11. ارجاع جهت اقدام مجدد (بررسی مجدد -> در دست اقدام)
                        new Transition
                        {
                            Name = "ارجاع جهت اقدام مجدد",
                            FromStatus = wsReview,
                            ToStatus = wsInProgress,
                            SourcePort = "Left",
                            TargetPort = "Top",
                            FromNodeId = nReview,
                            ToNodeId = nInProgress,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["ادمین"] },
                                new TransitionRole { RoleId = roles["پشتیبان"] },
                                new TransitionRole { RoleId = roles["مسئول فنی"] }
                            },
                            TransitionFields = new List<TransitionField>
                            {
                                new TransitionField
                                {
                                    FieldName = "پیوست:",
                                    FieldTypeId = fieldTypes["آپلود فایل (File)"],
                                    IsRequired = false,
                                    SortOrder = 1,
                                    IsActive = true
                                },
                                new TransitionField
                                {
                                    FieldName = "مهلت زمانی جدید:",
                                    FieldTypeId = fieldTypes["تاریخ (Date)"],
                                    IsRequired = false,
                                    SortOrder = 2,
                                    IsActive = true
                                }
                            }
                        },

                        // 12. ارجاع جهت بررسی و تایید فرستنده (انجام شده -> در انتظار تایید)
                        new Transition
                        {
                            Name = "ارجاع جهت بررسی و تایید فرستنده",
                            FromStatus = wsDone,
                            ToStatus = wsPending3,
                            SourcePort = "Right",
                            TargetPort = "Left",
                            FromNodeId = nDone,
                            ToNodeId = nPending3,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["ادمین"] }
                            },
                            TransitionFields = new List<TransitionField>()
                        },

                        // 13. تایید اقدامات و خاتمه درخواست (در انتظار تایید -> بسته شده)
                        new Transition
                        {
                            Name = "تایید اقدامات و خاتمه درخواست",
                            FromStatus = wsPending3,
                            ToStatus = wsClosed,
                            SourcePort = "Right",
                            TargetPort = "Left",
                            FromNodeId = nPending3,
                            ToNodeId = nClosed,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["ادمین"] },
                                new TransitionRole { RoleId = roles["کاربر"] }
                            },
                            TransitionFields = new List<TransitionField>()
                        },

                        // 14. ارجاع جهت بررسی مجدد (در انتظار تایید -> بررسی مجدد)
                        new Transition
                        {
                            Name = "ارجاع جهت بررسی مجدد",
                            FromStatus = wsPending3,
                            ToStatus = wsReview,
                            SourcePort = "Top",
                            TargetPort = "Right",
                            FromNodeId = nPending3,
                            ToNodeId = nReview,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["کاربر"] },
                                new TransitionRole { RoleId = roles["ادمین"] }
                            },
                            TransitionFields = new List<TransitionField>
                            {
                                new TransitionField
                                {
                                    FieldName = "علت ارجاع مجدد درخواست:",
                                    FieldTypeId = fieldTypes["متن طولانی (TextArea)"],
                                    IsRequired = false,
                                    SortOrder = 0,
                                    Placeholder = "لطفا علت عدم تایید اقدامات را بفرمایید...",
                                    IsActive = true
                                }
                            }
                        },

                        // 15. لغو درخواست (باز -> لغو شده)
                        new Transition
                        {
                            Name = "لغو درخواست",
                            FromStatus = wsOpen,
                            ToStatus = wsCancelled,
                            SourcePort = "Bottom",
                            TargetPort = "Left",
                            FromNodeId = nOpen,
                            ToNodeId = nCancelled,
                            IsAutomated = 0,
                            IsActive = true,
                            AllowedRoles = new List<TransitionRole>
                            {
                                new TransitionRole { RoleId = roles["ادمین"] },
                                new TransitionRole { RoleId = roles["پشتیبان"] },
                                new TransitionRole { RoleId = roles["کاربر"] }
                            },
                            TransitionFields = new List<TransitionField>
                            {
                                new TransitionField
                                {
                                    FieldName = "علت لغو درخواست:",
                                    FieldTypeId = fieldTypes["لیست کشویی (Dropdown)"],
                                    IsRequired = true,
                                    SortOrder = 0,
                                    Options = "درخواست تکراری, ثبت درخواستی دیگر, عدم پاسخگویی در زمان مناسب",
                                    IsActive = true
                                }
                            }
                        }
                    }
                    };

                    await context.Workflows.AddAsync(workflow);
                    await context.SaveChangesAsync();
                }

                // 9. Seed Projects
                if (!await context.Projects.AnyAsync(p => p.Name == "عمومی"))
                {
                    var generalWorkflow = await context.Workflows.FirstOrDefaultAsync(w => w.Name == "عمومی" || w.Name == "جریان کاری عمومی");
                    var targetRoleNames = new[] { "ادمین", "مسئول فنی", "کاربر", "پشتیبان" };
                    var projectRoles = await context.Roles.Where(r => targetRoleNames.Contains(r.Name)).ToListAsync();

                    if (generalWorkflow != null)
                    {
                        var defaultProject = new Project
                        {
                            Name = "عمومی",
                            Description = "پروژه تخصیص یافته برای درخواستهای عمومی",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            WorkflowId = generalWorkflow.Id,
                            RoleProjects = projectRoles.Select(r => new RoleProject { RoleId = r.Id }).ToList()
                        };

                        await context.Projects.AddAsync(defaultProject);
                        await context.SaveChangesAsync();

                        var allCats = await context.Categories.ToListAsync();
                        foreach (var c in allCats)
                        {
                            if (!await context.CategoryProjects.AnyAsync(cp => cp.CategoryId == c.Id && cp.ProjectId == defaultProject.Id))
                            {
                                await context.CategoryProjects.AddAsync(new CategoryProject { CategoryId = c.Id, ProjectId = defaultProject.Id });
                            }
                        }
                        await context.SaveChangesAsync();
                    }
                }

                // 10. Ensure status "باز" has IsInitial = true for all workflows in DB
                var allWorkflows = await context.Workflows
                    .Include(w => w.WorkflowStatuses)
                    .ThenInclude(ws => ws.Status)
                    .ToListAsync();

                foreach (var wf in allWorkflows)
                {
                    if (wf.WorkflowStatuses != null && wf.WorkflowStatuses.Any())
                    {
                        if (!wf.WorkflowStatuses.Any(ws => ws.IsInitial))
                        {
                            var openStatus = wf.WorkflowStatuses.FirstOrDefault(ws => ws.Status != null && ws.Status.Name == "باز")
                                            ?? wf.WorkflowStatuses.First();
                            openStatus.IsInitial = true;
                        }
                    }
                }
                await context.SaveChangesAsync();

                // 11. Seed Permissions and Role Assignments (Always executes independently)
                var moduleDefinitions = new[]
                {
                new { Title = "مدیریت تیکت‌ها", ResourceKey = "/tickets" },
                new { Title = "پروژه‌ها", ResourceKey = "/projects" },
                new { Title = "مدیریت کاربران", ResourceKey = "/users" },
                new { Title = "مدیریت جریان‌های کاری", ResourceKey = "/workflows" },
                new { Title = "لاگ‌های سیستم", ResourceKey = "/system-logs" },
                new { Title = "تنظیمات سیستم", ResourceKey = "تنظیمات سیستم" },
                new { Title = "مدیریت نقش‌ها", ResourceKey = "/settings/roles" },
                new { Title = "مدیریت وضعیت‌ها", ResourceKey = "/settings/statuses" },
                new { Title = "مدیریت انواع تیکت", ResourceKey = "/settings/categories" },
                new { Title = "مدیریت اولویت‌ها", ResourceKey = "/settings/priorities" },
                new { Title = "مدیریت فیلدهای تیکت", ResourceKey = "/settings/ticket-fields" },
                new { Title = "مدیریت دسترسی‌ها", ResourceKey = "/settings/permissions" }
            };

                // Helper to get or create permission
                async Task<Permission> GetOrCreatePermAsync(string title, string key, TicketHub.Application.Enums.PermissionType type)
                {
                    var perm = await context.Permissions.FirstOrDefaultAsync(p => p.ResourceKey == key && p.Type == type);
                    if (perm == null)
                    {
                        perm = new Permission { Title = title, ResourceKey = key, Type = type, IsActive = true };
                        await context.Permissions.AddAsync(perm);
                        await context.SaveChangesAsync();
                    }
                    return perm;
                }

                // Helper to sync exclusive role permission for a resource key
                async Task AssignExclusivePermToRoleAsync(string title, string key, TicketHub.Application.Enums.PermissionType desiredType, string roleName)
                {
                    var roleObj = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                    if (roleObj == null) return;

                    var desiredPerm = await GetOrCreatePermAsync(title, key, desiredType);

                    var existingRolePerms = await context.RolePermissions
                        .Include(rp => rp.Permission)
                        .Where(rp => rp.RoleId == roleObj.Id && rp.Permission.ResourceKey == key)
                        .ToListAsync();

                    var toRemove = existingRolePerms.Where(rp => rp.PermissionId != desiredPerm.Id).ToList();
                    if (toRemove.Any())
                    {
                        context.RolePermissions.RemoveRange(toRemove);
                    }

                    if (!existingRolePerms.Any(rp => rp.PermissionId == desiredPerm.Id))
                    {
                        await context.RolePermissions.AddAsync(new RolePermission { RoleId = roleObj.Id, PermissionId = desiredPerm.Id });
                    }

                    await context.SaveChangesAsync();
                }

                // 1. Admin Role ("ادمین"): Full Access to all modules and menus
                foreach (var item in moduleDefinitions)
                {
                    await AssignExclusivePermToRoleAsync(item.Title, item.ResourceKey, TicketHub.Application.Enums.PermissionType.Full, "ادمین");
                }

                // 2. Support & Technical Roles ("پشتیبان" and "مسئول فنی"):
                // - SystemSection access to Ticket section (/tickets)
                // - Menu (Read-only) access to Projects (/projects), Workflows (/workflows), and Users (/users)
                var supportRoles = new[] { "پشتیبان", "مسئول فنی" };
                foreach (var roleName in supportRoles)
                {
                    await AssignExclusivePermToRoleAsync("مدیریت تیکت‌ها", "/tickets", TicketHub.Application.Enums.PermissionType.SystemSection, roleName);
                    await AssignExclusivePermToRoleAsync("پروژه‌ها", "/projects", TicketHub.Application.Enums.PermissionType.Menu, roleName);
                    await AssignExclusivePermToRoleAsync("مدیریت کاربران", "/users", TicketHub.Application.Enums.PermissionType.Menu, roleName);
                    await AssignExclusivePermToRoleAsync("مدیریت جریان‌های کاری", "/workflows", TicketHub.Application.Enums.PermissionType.Menu, roleName);
                }

                // 3. Regular User Role ("کاربر"): Menu access to Ticket section (/tickets)
                await AssignExclusivePermToRoleAsync("مدیریت تیکت‌ها", "/tickets", TicketHub.Application.Enums.PermissionType.Menu, "کاربر");

                await context.SaveChangesAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}