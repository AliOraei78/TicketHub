using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransitionFieldValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TransitionFieldValueId",
                table: "Attachments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransitionFieldValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TicketHistoryId = table.Column<int>(type: "int", nullable: false),
                    TransitionFieldId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransitionFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransitionFieldValues_TicketHistories_TicketHistoryId",
                        column: x => x.TicketHistoryId,
                        principalTable: "TicketHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransitionFieldValues_TransitionFields_TransitionFieldId",
                        column: x => x.TransitionFieldId,
                        principalTable: "TransitionFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TransitionFieldValueId",
                table: "Attachments",
                column: "TransitionFieldValueId");

            migrationBuilder.CreateIndex(
                name: "IX_TransitionFieldValues_TicketHistoryId_TransitionFieldId",
                table: "TransitionFieldValues",
                columns: new[] { "TicketHistoryId", "TransitionFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransitionFieldValues_TransitionFieldId",
                table: "TransitionFieldValues",
                column: "TransitionFieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_TransitionFieldValues_TransitionFieldValueId",
                table: "Attachments",
                column: "TransitionFieldValueId",
                principalTable: "TransitionFieldValues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_TransitionFieldValues_TransitionFieldValueId",
                table: "Attachments");

            migrationBuilder.DropTable(
                name: "TransitionFieldValues");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_TransitionFieldValueId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "TransitionFieldValueId",
                table: "Attachments");
        }
    }
}
