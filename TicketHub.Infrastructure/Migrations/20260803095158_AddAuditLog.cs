using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatusId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TicketFieldValueId",
                table: "Attachments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_WorkflowStatusId",
                table: "Tickets",
                column: "WorkflowStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TicketFieldValueId",
                table: "Attachments",
                column: "TicketFieldValueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_TicketFieldValues_TicketFieldValueId",
                table: "Attachments",
                column: "TicketFieldValueId",
                principalTable: "TicketFieldValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_WorkflowStatuses_WorkflowStatusId",
                table: "Tickets",
                column: "WorkflowStatusId",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_TicketFieldValues_TicketFieldValueId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_WorkflowStatuses_WorkflowStatusId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_WorkflowStatusId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_TicketFieldValueId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "WorkflowStatusId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TicketFieldValueId",
                table: "Attachments");
        }
    }
}
