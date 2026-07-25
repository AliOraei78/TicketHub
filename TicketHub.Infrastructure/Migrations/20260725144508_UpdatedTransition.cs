using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedTransition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowStatuses_Workflows_WorkflowId",
                table: "WorkflowStatuses");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowStatuses_Workflows_WorkflowId",
                table: "WorkflowStatuses",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowStatuses_Workflows_WorkflowId",
                table: "WorkflowStatuses");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowStatuses_Workflows_WorkflowId",
                table: "WorkflowStatuses",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
