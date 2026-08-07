using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Fluxor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Web.Components.Pages.Admin.WorkFlows.WorkflowEditor;
using TicketHub.Web.Store;
using Xunit;
using TicketHub.Core.Common;

namespace TicketHub.Tests.bUnit
{
    public class WorkflowEditorTests : TestContext
    {
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IState<WorkflowEditorState>> _mockState;

        public WorkflowEditorTests()
        {
            _mockDispatcher = new Mock<IDispatcher>();
            _mockState = new Mock<IState<WorkflowEditorState>>();

            var defaultState = new WorkflowEditorState(
                false,
                false,
                new List<StatusDto>
                {
                    new StatusDto { Id = 1, Name = "Open", ColorCode = "#000000" },
                    new StatusDto { Id = 2, Name = "Closed", ColorCode = "#ffffff" }
                },
                new List<RoleDto>(),
                new List<FieldTypeDto>
                {
                    new FieldTypeDto { Id = 1, Type = "Text" },
                    new FieldTypeDto { Id = 2, Type = "Number" },
                    new FieldTypeDto { Id = 3, Type = "DropDown" },
                    new FieldTypeDto { Id = 4, Type = "Date" },
                    new FieldTypeDto { Id = 5, Type = "Checkbox" }
                },
                null
            );

            _mockState.Setup(s => s.Value).Returns(defaultState);

            Services.AddSingleton(_mockDispatcher.Object);
            Services.AddSingleton(_mockState.Object);
            Services.AddSingleton(new Mock<IActionSubscriber>().Object);
        }

        [Fact]
        public void LoadEditor_DispatchesLoadDataAction()
        {
            var cut = Render<WorkflowEditor>(parameters => parameters.Add(p => p.Id, 1));
            _mockDispatcher.Verify(d => d.Dispatch(It.Is<LoadEditorDataAction>(a => a.WorkflowId == 1)), Times.Once);
        }

        [Fact]
        public void AddStatusToCanvas_And_DrawTransition_SavesCorrectly()
        {
            // Act
            var cut = Render<WorkflowEditor>();
            
            var toolbarNameInput = cut.Find("input[placeholder='نام جریان کاری...']");
            toolbarNameInput.Input("My Test Workflow");

            // We can directly invoke private methods using reflection or just test that if the state has nodes/connections, it saves them correctly.
            // Since Blazor Canvas Drag & Drop is tricky to mock in bUnit without deep JSInterop mocks,
            // we will simulate the SaveAction directly with populated Nodes/Connections to ensure the component handles transitions and transition fields.
            
            var nodes = new List<CanvasNodeDto>
            {
                new CanvasNodeDto { Id = Guid.NewGuid(), Status = new StatusDto { Id = 1, Name = "Open" }, X = 100, Y = 100 },
                new CanvasNodeDto { Id = Guid.NewGuid(), Status = new StatusDto { Id = 2, Name = "In Progress" }, X = 300, Y = 100 }
            };

            var connectionId = Guid.NewGuid();
            var connections = new List<CanvasConnection>
            {
                new CanvasConnection 
                { 
                    Id = connectionId, 
                    FromNodeId = nodes[0].Id, 
                    ToNodeId = nodes[1].Id, 
                    SourcePort = "Right", 
                    TargetPort = "Left", 
                    Name = "Start Work",
                    CustomFields = new List<CanvasTransitionField>
                    {
                        new CanvasTransitionField { FieldName = "Field1", FieldTypeId = 1 },
                        new CanvasTransitionField { FieldName = "Field2", FieldTypeId = 2 },
                        new CanvasTransitionField { FieldName = "Field3", FieldTypeId = 3, Options = "A,B" },
                        new CanvasTransitionField { FieldName = "Field4", FieldTypeId = 4 },
                        new CanvasTransitionField { FieldName = "Field5", FieldTypeId = 5 }
                    }
                }
            };

            // Using reflection to set private fields for the test since Fluxor state modification inside component tests requires triggering EditorDataLoadedAction
            cut.Instance.GetType().GetField("CanvasNodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cut.Instance, nodes);
            cut.Instance.GetType().GetField("Connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cut.Instance, connections);

            var saveButton = cut.Find("button.bg-blue-600"); // Save button in toolbar
            saveButton.Click();

            _mockDispatcher.Verify(d => d.Dispatch(It.Is<SaveWorkflowEditorAction>(a => 
                a.Name == "My Test Workflow" && 
                a.Nodes.Count == 2 && 
                a.Connections.Count == 1 &&
                a.Connections[0].CustomFields.Count == 5)), Times.Once);
        }
    }
}
