using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditedTransitionRe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowStatuses_Workflows_WorkflowId",
                table: "WorkflowStatuses");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories",
                column: "WorkFlowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowStatuses_Workflows_WorkflowId",
                table: "WorkflowStatuses",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowStatuses_Workflows_WorkflowId",
                table: "WorkflowStatuses");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories",
                column: "WorkFlowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowStatuses_Workflows_WorkflowId",
                table: "WorkflowStatuses",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
