using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedWorkflowEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "Transitions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "Statuses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transitions_WorkflowId",
                table: "Transitions",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_WorkflowId",
                table: "Statuses",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkflowId",
                table: "Projects",
                column: "WorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Workflows_WorkflowId",
                table: "Projects",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Statuses_Workflows_WorkflowId",
                table: "Statuses",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transitions_Workflows_WorkflowId",
                table: "Transitions",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Workflows_WorkflowId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Statuses_Workflows_WorkflowId",
                table: "Statuses");

            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_Workflows_WorkflowId",
                table: "Transitions");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Transitions_WorkflowId",
                table: "Transitions");

            migrationBuilder.DropIndex(
                name: "IX_Statuses_WorkflowId",
                table: "Statuses");

            migrationBuilder.DropIndex(
                name: "IX_Projects_WorkflowId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "Transitions");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "Projects");
        }
    }
}
