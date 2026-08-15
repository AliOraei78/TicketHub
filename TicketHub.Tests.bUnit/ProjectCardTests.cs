using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using TicketHub.Application.DTOs;
using TicketHub.Core.Common;
using TicketHub.Web.Components.Pages.Admin.Projects;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class ProjectCardTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_ProjectInfo_Correctly()
        {
            var project = new ProjectDto
            {
                Id = 5,
                Name = "پروژه آلفا سایبری",
                Description = "سیستم تستی پروژه‌های زیرساختی",
                IsActive = true,
                CreatedAt = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc)
            };

            var cut = Render<ProjectCard>(parameters => parameters
                .Add(p => p.Project, project)
            );

            cut.Markup.Should().Contain("پروژه آلفا سایبری");
            cut.Markup.Should().Contain("PRJ-005");
            cut.Markup.Should().Contain("سیستم تستی پروژه‌های زیرساختی");
        }

        [Fact]
        public void Render_ActiveBadge_WhenIsActiveIsTrue()
        {
            var project = new ProjectDto
            {
                Id = 1,
                Name = "پروژه فعال",
                IsActive = true
            };

            var cut = Render<ProjectCard>(parameters => parameters
                .Add(p => p.Project, project)
            );

            cut.Markup.Should().Contain("فعال");
            cut.Markup.Should().Contain("text-emerald-300");
        }

        [Fact]
        public void Render_InactiveBadge_WhenIsActiveIsFalse()
        {
            var project = new ProjectDto
            {
                Id = 2,
                Name = "پروژه غیرفعال",
                IsActive = false
            };

            var cut = Render<ProjectCard>(parameters => parameters
                .Add(p => p.Project, project)
            );

            cut.Markup.Should().Contain("غیرفعال");
            cut.Markup.Should().Contain("text-slate-400");
        }

        [Fact]
        public void Render_WorkflowBadge_WhenWorkflowExists()
        {
            var project = new ProjectDto
            {
                Id = 3,
                Name = "پروژه با جریان",
                Workflow = new WorkflowDto { Id = 10, Name = "جریان اصلی پشتیبانی" }
            };

            var cut = Render<ProjectCard>(parameters => parameters
                .Add(p => p.Project, project)
            );

            cut.Markup.Should().Contain("جریان اصلی پشتیبانی");
            cut.Markup.Should().Contain("text-purple-300");
        }

        [Fact]
        public void Render_NoWorkflow_WhenWorkflowIsNull()
        {
            var project = new ProjectDto
            {
                Id = 4,
                Name = "پروژه بدون جریان",
                Workflow = null
            };

            var cut = Render<ProjectCard>(parameters => parameters
                .Add(p => p.Project, project)
            );

            cut.Markup.Should().Contain("بدون جریان کاری");
        }

        [Fact]
        public void Render_RoleCount_Correctly()
        {
            var project = new ProjectDto
            {
                Id = 7,
                Name = "پروژه دارای نقش",
                RoleIds = new List<int> { 1, 2, 3 }
            };

            var cut = Render<ProjectCard>(parameters => parameters
                .Add(p => p.Project, project)
            );

            cut.Markup.Should().Contain("3");
            cut.Markup.Should().Contain("نقش");
        }

        [Fact]
        public void Render_PersianCreationDate_Correctly()
        {
            var project = new ProjectDto
            {
                Id = 8,
                Name = "پروژه دارای تاریخ",
                CreatedAt = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc)
            };

            var cut = Render<ProjectCard>(parameters => parameters
                .Add(p => p.Project, project)
            );

            var expectedDate = project.CreatedAt.ToPersianDateString();
            cut.Markup.Should().Contain(expectedDate);
        }

        [Fact]
        public void EditButton_Click_InvokesOnEditCallback()
        {
            bool edited = false;
            var project = new ProjectDto { Id = 10, Name = "پروژه ویرایش" };

            var cut = Render<ProjectCard>(parameters => parameters
                .Add(p => p.Project, project)
                .Add(p => p.OnEdit, () => { edited = true; })
            );

            var editButton = cut.Find("button[title='ویرایش پروژه']");
            editButton.Click();

            edited.Should().BeTrue();
        }

        [Fact]
        public void DeleteButton_Click_InvokesOnDeleteCallback()
        {
            bool deleted = false;
            var project = new ProjectDto { Id = 11, Name = "پروژه حذف" };

            var cut = Render<ProjectCard>(parameters => parameters
                .Add(p => p.Project, project)
                .Add(p => p.OnDelete, () => { deleted = true; })
            );

            var deleteButton = cut.Find("button[title='حذف پروژه']");
            deleteButton.Click();

            deleted.Should().BeTrue();
        }
    }
}
