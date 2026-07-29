using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceUserProjectWithRoleProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Users_UserId",
                table: "TicketHistories");

            migrationBuilder.DropTable(
                name: "UserProjects");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "TicketHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RoleProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleProjects_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_RoleId",
                table: "TicketHistories",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleProjects_ProjectId",
                table: "RoleProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleProjects_RoleId_ProjectId",
                table: "RoleProjects",
                columns: new[] { "RoleId", "ProjectId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Roles_UserId",
                table: "TicketHistories",
                column: "UserId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Users_RoleId",
                table: "TicketHistories",
                column: "RoleId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Roles_UserId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Users_RoleId",
                table: "TicketHistories");

            migrationBuilder.DropTable(
                name: "RoleProjects");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_RoleId",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "TicketHistories");

            migrationBuilder.CreateTable(
                name: "UserProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProjects_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProjects_ProjectId",
                table: "UserProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProjects_UserId_ProjectId",
                table: "UserProjects",
                columns: new[] { "UserId", "ProjectId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Users_UserId",
                table: "TicketHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
