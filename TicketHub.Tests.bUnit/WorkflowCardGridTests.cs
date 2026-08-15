using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.WorkFlows;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class WorkflowCardGridTests : BUnitComponentTestBase
    {
        [Fact]
        public void Render_WorkflowCardGrid_WithCards()
        {
            var workflows = new List<WorkflowDto>
            {
                new WorkflowDto
                {
                    Id = 1,
                    Name = "جریان اصلی پشتیبانی",
                    Description = "جریان پیش‌فرض برای تیکت‌های عمومی",
                    IsActive = true,
                    WorkflowStatuses = new List<WorkflowStatusDto>
                    {
                        new WorkflowStatusDto { Id = 1 },
                        new WorkflowStatusDto { Id = 2 }
                    },
                    Transitions = new List<TransitionDto>
                    {
                        new TransitionDto { Id = 1 }
                    },
                    Projects = new List<ProjectDto>
                    {
                        new ProjectDto { Id = 1, Name = "پروژه آلفا" }
                    }
                }
            };

            var cut = Render<WorkflowCardGrid>(parameters => parameters
                .Add(p => p.Workflows, workflows)
                .Add(p => p.IsLoading, false)
            );

            cut.Markup.Should().Contain("جریان اصلی پشتیبانی");
            cut.Markup.Should().Contain("جریان پیش‌فرض برای تیکت‌های عمومی");
        }

        [Fact]
        public void EditButton_Click_InvokesOnEdit()
        {
            WorkflowDto? editedWorkflow = null;
            var workflows = new List<WorkflowDto>
            {
                new WorkflowDto { Id = 10, Name = "جریان قابل ویرایش" }
            };

            var cut = Render<WorkflowCardGrid>(parameters => parameters
                .Add(p => p.Workflows, workflows)
                .Add(p => p.OnEdit, EventCallback.Factory.Create<WorkflowDto>(this, (Action<WorkflowDto>)(w => editedWorkflow = w)))
            );

            var editBtn = cut.Find("button:contains('طراحی و ویرایش مسیر')");
            editBtn.Click();

            editedWorkflow.Should().NotBeNull();
            editedWorkflow!.Id.Should().Be(10);
        }

        [Fact]
        public void DeleteButton_Click_InvokesOnDelete()
        {
            WorkflowDto? deletedWorkflow = null;
            var workflows = new List<WorkflowDto>
            {
                new WorkflowDto { Id = 20, Name = "جریان قابل حذف" }
            };

            var cut = Render<WorkflowCardGrid>(parameters => parameters
                .Add(p => p.Workflows, workflows)
                .Add(p => p.OnDelete, EventCallback.Factory.Create<WorkflowDto>(this, (Action<WorkflowDto>)(w => deletedWorkflow = w)))
            );

            var deleteBtn = cut.Find("button[title='حذف جریان کار']");
            deleteBtn.Click();

            deletedWorkflow.Should().NotBeNull();
            deletedWorkflow!.Id.Should().Be(20);
        }
    }
}
