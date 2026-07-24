using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addedisactive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "TicketHistories");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Transitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CommentId",
                table: "TicketHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkFlowId",
                table: "TicketHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_CommentId",
                table: "TicketHistories",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_WorkFlowId",
                table: "TicketHistories",
                column: "WorkFlowId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Comments_CommentId",
                table: "TicketHistories",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories",
                column: "WorkFlowId",
                principalTable: "Workflows",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Comments_CommentId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Workflows_WorkFlowId",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_CommentId",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_WorkFlowId",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Transitions");

            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "WorkFlowId",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
