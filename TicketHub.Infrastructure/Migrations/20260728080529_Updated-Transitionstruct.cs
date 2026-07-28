using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedTransitionstruct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkflowStatuses",
                table: "WorkflowStatuses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransitionRoles",
                table: "TransitionRoles");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "WorkflowStatuses",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<Guid>(
                name: "NodeId",
                table: "WorkflowStatuses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "FromNodeId",
                table: "Transitions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ToNodeId",
                table: "Transitions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "TransitionRoles",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkflowStatuses",
                table: "WorkflowStatuses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransitionRoles",
                table: "TransitionRoles",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStatuses_WorkflowId",
                table: "WorkflowStatuses",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_TransitionRoles_TransitionId",
                table: "TransitionRoles",
                column: "TransitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkflowStatuses",
                table: "WorkflowStatuses");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStatuses_WorkflowId",
                table: "WorkflowStatuses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransitionRoles",
                table: "TransitionRoles");

            migrationBuilder.DropIndex(
                name: "IX_TransitionRoles_TransitionId",
                table: "TransitionRoles");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "WorkflowStatuses");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "WorkflowStatuses");

            migrationBuilder.DropColumn(
                name: "FromNodeId",
                table: "Transitions");

            migrationBuilder.DropColumn(
                name: "ToNodeId",
                table: "Transitions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TransitionRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkflowStatuses",
                table: "WorkflowStatuses",
                columns: new[] { "WorkflowId", "StatusId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransitionRoles",
                table: "TransitionRoles",
                columns: new[] { "TransitionId", "RoleId" });
        }
    }
}
