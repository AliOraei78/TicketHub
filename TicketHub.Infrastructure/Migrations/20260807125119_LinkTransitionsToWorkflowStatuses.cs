using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkTransitionsToWorkflowStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_Statuses_FromState",
                table: "Transitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_Statuses_ToState",
                table: "Transitions");

            migrationBuilder.AddForeignKey(
                name: "FK_Transitions_WorkflowStatuses_FromState",
                table: "Transitions",
                column: "FromState",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transitions_WorkflowStatuses_ToState",
                table: "Transitions",
                column: "ToState",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_WorkflowStatuses_FromState",
                table: "Transitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_WorkflowStatuses_ToState",
                table: "Transitions");

            migrationBuilder.AddForeignKey(
                name: "FK_Transitions_Statuses_FromState",
                table: "Transitions",
                column: "FromState",
                principalTable: "Statuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transitions_Statuses_ToState",
                table: "Transitions",
                column: "ToState",
                principalTable: "Statuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
