using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedWorkflowFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions");

            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "Transitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Transitions_WorkflowId",
                table: "Transitions",
                column: "WorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories",
                column: "WorkFlowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transitions_Workflows_WorkflowId",
                table: "Transitions",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_Workflows_WorkflowId",
                table: "Transitions");

            migrationBuilder.DropIndex(
                name: "IX_Transitions_WorkflowId",
                table: "Transitions");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "Transitions");

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                columns: table => new
                {
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    TransitionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => new { x.WorkflowId, x.TransitionId });
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_Transitions_TransitionId",
                        column: x => x.TransitionId,
                        principalTable: "Transitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_TransitionId",
                table: "WorkflowTransitions",
                column: "TransitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories",
                column: "WorkFlowId",
                principalTable: "Workflows",
                principalColumn: "Id");
        }
    }
}
