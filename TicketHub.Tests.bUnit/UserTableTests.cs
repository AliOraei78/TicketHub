using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;
using TicketHub.Web.Components.Pages.Admin.Users;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class UserTableTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_UsersTable_WithColumnsAndRows()
        {
            var users = new List<UserDto>
            {
                new UserDto
                {
                    Id = 1,
                    Name = "کاربر اول",
                    Email = "user1@example.com",
                    PhoneNumber = "09121111111",
                    IsActive = true,
                    UserRoles = new List<UserRole>
                    {
                        new UserRole { Role = new Role { Id = 1, Name = "مدیر سیستم" } }
                    }
                },
                new UserDto
                {
                    Id = 2,
                    Name = "کاربر دوم",
                    Email = "user2@example.com",
                    PhoneNumber = "09122222222",
                    IsActive = false,
                    UserRoles = new List<UserRole>()
                }
            };

            var cut = Render<UserTable>(parameters => parameters
                .Add(p => p.Users, users)
                .Add(p => p.IsLoading, false)
                .Add(p => p.CurrentPage, 1)
                .Add(p => p.PageSize, 10)
            );

            cut.Markup.Should().Contain("کاربر اول");
            cut.Markup.Should().Contain("user1@example.com");
            cut.Markup.Should().Contain("کاربر دوم");
            cut.Markup.Should().Contain("user2@example.com");
            cut.Markup.Should().Contain("مدیر سیستم");
            cut.Markup.Should().Contain("بدون نقش");
        }

        [Fact]
        public void Render_UserStatusBadge_ActiveAndInactive()
        {
            var users = new List<UserDto>
            {
                new UserDto { Id = 1, Name = "فعال", IsActive = true },
                new UserDto { Id = 2, Name = "غیرفعال", IsActive = false }
            };

            var cut = Render<UserTable>(parameters => parameters
                .Add(p => p.Users, users)
                .Add(p => p.IsLoading, false)
            );

            cut.Markup.Should().Contain("فعال");
            cut.Markup.Should().Contain("غیرفعال");
        }

        [Fact]
        public void EditButton_Click_InvokesOnEdit()
        {
            UserDto? editedUser = null;
            var users = new List<UserDto>
            {
                new UserDto { Id = 10, Name = "کاربر ویرایش", Email = "edit@test.com" }
            };

            var cut = Render<UserTable>(parameters => parameters
                .Add(p => p.Users, users)
                .Add(p => p.OnEdit, EventCallback.Factory.Create<UserDto>(this, (Action<UserDto>)(u => editedUser = u)))
            );

            var editBtn = cut.Find("button[title='ویرایش']");
            editBtn.Click();

            editedUser.Should().NotBeNull();
            editedUser!.Id.Should().Be(10);
        }

        [Fact]
        public void DeleteButton_Click_InvokesOnDelete()
        {
            UserDto? deletedUser = null;
            var users = new List<UserDto>
            {
                new UserDto { Id = 20, Name = "کاربر حذف", Email = "del@test.com" }
            };

            var cut = Render<UserTable>(parameters => parameters
                .Add(p => p.Users, users)
                .Add(p => p.OnDelete, EventCallback.Factory.Create<UserDto>(this, (Action<UserDto>)(u => deletedUser = u)))
            );

            var deleteBtn = cut.Find("button[title='حذف']");
            deleteBtn.Click();

            deletedUser.Should().NotBeNull();
            deletedUser!.Id.Should().Be(20);
        }

        [Fact]
        public void EmptyList_RendersEmptyState()
        {
            var cut = Render<UserTable>(parameters => parameters
                .Add(p => p.Users, new List<UserDto>())
                .Add(p => p.IsLoading, false)
            );

            cut.Markup.Should().Contain("کاربری در سیستم یافت نشد");
        }
    }
}
