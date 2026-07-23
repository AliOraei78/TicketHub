using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedTransition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserRoles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserProjects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Statuses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Roles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Transitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromState = table.Column<int>(type: "int", nullable: false),
                    ToState = table.Column<int>(type: "int", nullable: false),
                    IsAutomated = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransitionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    TransitionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ParentHistoryId = table.Column<int>(type: "int", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransitionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransitionHistories_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransitionHistories_Transitions_TransitionId",
                        column: x => x.TransitionId,
                        principalTable: "Transitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransitionRoles",
                columns: table => new
                {
                    TransitionId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransitionRoles", x => new { x.TransitionId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_TransitionRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransitionRoles_Transitions_TransitionId",
                        column: x => x.TransitionId,
                        principalTable: "Transitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TicketId = table.Column<int>(type: "int", nullable: true),
                    TransitionHistoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_TransitionHistories_TransitionHistoryId",
                        column: x => x.TransitionHistoryId,
                        principalTable: "TransitionHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TicketId",
                table: "Attachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TransitionHistoryId",
                table: "Attachments",
                column: "TransitionHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransitionHistories_TicketId",
                table: "TransitionHistories",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TransitionHistories_TransitionId",
                table: "TransitionHistories",
                column: "TransitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransitionRoles_RoleId",
                table: "TransitionRoles",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "TransitionRoles");

            migrationBuilder.DropTable(
                name: "TransitionHistories");

            migrationBuilder.DropTable(
                name: "Transitions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserProjects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Roles");
        }
    }
}
