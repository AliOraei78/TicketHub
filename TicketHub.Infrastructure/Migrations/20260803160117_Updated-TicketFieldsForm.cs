using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedTicketFieldsForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_TicketHistories_TicketHistoryId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TicketHistoryId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "CommentText",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "TicketHistoryId",
                table: "Comments");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "TicketFields",
                newName: "Placeholder");

            migrationBuilder.AddColumn<string>(
                name: "DefaultValue",
                table: "TicketFields",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "TicketFields",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "TicketFields",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultValue",
                table: "TicketFields");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "TicketFields");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "TicketFields");

            migrationBuilder.RenameColumn(
                name: "Placeholder",
                table: "TicketFields",
                newName: "Description");

            migrationBuilder.AddColumn<int>(
                name: "CommentId",
                table: "TicketHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommentText",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TicketHistoryId",
                table: "Comments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TicketHistoryId",
                table: "Comments",
                column: "TicketHistoryId",
                unique: true,
                filter: "[TicketHistoryId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_TicketHistories_TicketHistoryId",
                table: "Comments",
                column: "TicketHistoryId",
                principalTable: "TicketHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
