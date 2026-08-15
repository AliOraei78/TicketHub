using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Web.Store;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class WorkflowStoreTests
    {
        [Fact]
        public void ReduceLoadWorkflows_SetsIsLoadingToTrue()
        {
            var initialState = new WorkflowState(
                false,
                new List<WorkflowDto>(),
                0,
                new List<ProjectDto>(),
                new List<StatusDto>(),
                string.Empty,
                10,
                1,
                new List<int>(),
                new List<int>(),
                new List<int>()
            );

            var newState = WorkflowReducers.ReduceLoadWorkflows(initialState, new LoadWorkflowsAction());
            newState.IsLoading.Should().BeTrue();
        }

        [Fact]
        public void ReduceInitialDataLoaded_UpdatesProjectsAndStatuses()
        {
            var initialState = new WorkflowState(
                true,
                new List<WorkflowDto>(),
                0,
                new List<ProjectDto>(),
                new List<StatusDto>(),
                string.Empty,
                10,
                1,
                new List<int>(),
                new List<int>(),
                new List<int>()
            );

            var projects = new List<ProjectDto> { new ProjectDto { Id = 1, Name = "Alpha" } };
            var statuses = new List<StatusDto> { new StatusDto { Id = 1, Name = "Open" } };

            var action = new WorkflowInitialDataLoadedAction(projects, statuses);
            var newState = WorkflowReducers.ReduceInitialDataLoaded(initialState, action);

            newState.AvailableProjects.Should().BeEquivalentTo(projects);
            newState.AvailableStatuses.Should().BeEquivalentTo(statuses);
        }

        [Fact]
        public void ReduceWorkflowsLoaded_SetsWorkflowsAndStopsLoading()
        {
            var initialState = new WorkflowState(
                true,
                new List<WorkflowDto>(),
                0,
                new List<ProjectDto>(),
                new List<StatusDto>(),
                string.Empty,
                10,
                1,
                new List<int>(),
                new List<int>(),
                new List<int>()
            );

            var workflows = new List<WorkflowDto> { new WorkflowDto { Id = 1, Name = "Main WF" } };
            var action = new WorkflowsLoadedAction(workflows, 25, 2);

            var newState = WorkflowReducers.ReduceWorkflowsLoaded(initialState, action);
            newState.IsLoading.Should().BeFalse();
            newState.Workflows.Should().BeEquivalentTo(workflows);
            newState.TotalWorkflows.Should().Be(25);
            newState.CurrentPage.Should().Be(2);
        }

        [Fact]
        public void ReduceSetFilters_UpdatesProvidedValues()
        {
            var initialState = new WorkflowState(
                false,
                new List<WorkflowDto>(),
                10,
                new List<ProjectDto>(),
                new List<StatusDto>(),
                "old",
                8,
                1,
                new List<int>(),
                new List<int>(),
                new List<int>()
            );

            var action = new SetWorkflowFiltersAction("search term", 16, 3, new List<int> { 2 }, new List<int> { 5 });
            var newState = WorkflowReducers.ReduceSetFilters(initialState, action);

            newState.SearchTerm.Should().Be("search term");
            newState.PageSize.Should().Be(16);
            newState.CurrentPage.Should().Be(3);
            newState.SelectedFilterProjectIds.Should().Contain(2);
            newState.SelectedFilterStatusIds.Should().Contain(5);
        }
    }
}
