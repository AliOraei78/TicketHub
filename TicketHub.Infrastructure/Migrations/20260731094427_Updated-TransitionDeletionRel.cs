using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedTransitionDeletionRel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_Workflows_WorkflowId",
                table: "Transitions");

            migrationBuilder.AddForeignKey(
                name: "FK_Transitions_Workflows_WorkflowId",
                table: "Transitions",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_Workflows_WorkflowId",
                table: "Transitions");

            migrationBuilder.AddForeignKey(
                name: "FK_Transitions_Workflows_WorkflowId",
                table: "Transitions",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
