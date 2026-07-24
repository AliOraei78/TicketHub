using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguredAppDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Tickets_TicketId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tickets_TicketId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Workflows_WorkflowId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Statuses_Workflows_WorkflowId",
                table: "Statuses");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Comments_CommentId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Tickets_TicketId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Transitions_TransitionId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Priorities_PriorityId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Projects_ProjectId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_UserId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_Workflows_WorkflowId",
                table: "Transitions");

            migrationBuilder.DropIndex(
                name: "IX_Transitions_WorkflowId",
                table: "Transitions");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_CommentId",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_Statuses_WorkflowId",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "Transitions");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "Statuses");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivateAt",
                table: "Transitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PriorityId",
                table: "Tickets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "TicketHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TransitionId",
                table: "TicketHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TicketId",
                table: "TicketHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CommentText",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FromStatusId",
                table: "TicketHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromStatusName",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketTitle",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ToStatusId",
                table: "TicketHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToStatusName",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransitionTitle",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkFlowName",
                table: "TicketHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "TicketId",
                table: "Comments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "TicketHistoryId",
                table: "Comments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowStatuses",
                columns: table => new
                {
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStatuses", x => new { x.WorkflowId, x.StatusId });
                    table.ForeignKey(
                        name: "FK_WorkflowStatuses_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowStatuses_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                columns: table => new
                {
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    TransitionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => new { x.WorkflowId, x.TransitionId });
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_Transitions_TransitionId",
                        column: x => x.TransitionId,
                        principalTable: "Transitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transitions_FromState",
                table: "Transitions",
                column: "FromState");

            migrationBuilder.CreateIndex(
                name: "IX_Transitions_ToState",
                table: "Transitions",
                column: "ToState");

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_FromStatusId",
                table: "TicketHistories",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_ParentHistoryId",
                table: "TicketHistories",
                column: "ParentHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_ToStatusId",
                table: "TicketHistories",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_UserId",
                table: "TicketHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TicketHistoryId",
                table: "Comments",
                column: "TicketHistoryId",
                unique: true,
                filter: "[TicketHistoryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStatuses_StatusId",
                table: "WorkflowStatuses",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_TransitionId",
                table: "WorkflowTransitions",
                column: "TransitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Tickets_TicketId",
                table: "Attachments",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_TicketHistories_TicketHistoryId",
                table: "Comments",
                column: "TicketHistoryId",
                principalTable: "TicketHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Tickets_TicketId",
                table: "Comments",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Workflows_WorkflowId",
                table: "Projects",
                column: "WorkflowId",
                principalTable: "Workflows",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Statuses_FromStatusId",
                table: "TicketHistories",
                column: "FromStatusId",
                principalTable: "Statuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Statuses_ToStatusId",
                table: "TicketHistories",
                column: "ToStatusId",
                principalTable: "Statuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_TicketHistories_ParentHistoryId",
                table: "TicketHistories",
                column: "ParentHistoryId",
                principalTable: "TicketHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Tickets_TicketId",
                table: "TicketHistories",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Transitions_TransitionId",
                table: "TicketHistories",
                column: "TransitionId",
                principalTable: "Transitions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Users_UserId",
                table: "TicketHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Priorities_PriorityId",
                table: "Tickets",
                column: "PriorityId",
                principalTable: "Priorities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Projects_ProjectId",
                table: "Tickets",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_UserId",
                table: "Tickets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Tickets_TicketId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_TicketHistories_TicketHistoryId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tickets_TicketId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Workflows_WorkflowId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Statuses_FromStatusId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Statuses_ToStatusId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_TicketHistories_ParentHistoryId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Tickets_TicketId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Transitions_TransitionId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Users_UserId",
                table: "TicketHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Priorities_PriorityId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Projects_ProjectId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_UserId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_Statuses_FromState",
                table: "Transitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transitions_Statuses_ToState",
                table: "Transitions");

            migrationBuilder.DropTable(
                name: "WorkflowStatuses");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions");

            migrationBuilder.DropIndex(
                name: "IX_Transitions_FromState",
                table: "Transitions");

            migrationBuilder.DropIndex(
                name: "IX_Transitions_ToState",
                table: "Transitions");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_FromStatusId",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_ParentHistoryId",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_ToStatusId",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_UserId",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TicketHistoryId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ActivateAt",
                table: "Transitions");

            migrationBuilder.DropColumn(
                name: "CommentText",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "FromStatusId",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "FromStatusName",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "TicketTitle",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "ToStatusId",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "ToStatusName",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "TransitionTitle",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "WorkFlowName",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "TicketHistoryId",
                table: "Comments");

            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "Transitions",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Tickets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PriorityId",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "TicketHistories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TransitionId",
                table: "TicketHistories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TicketId",
                table: "TicketHistories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowId",
                table: "Statuses",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TicketId",
                table: "Comments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transitions_WorkflowId",
                table: "Transitions",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_CommentId",
                table: "TicketHistories",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_WorkflowId",
                table: "Statuses",
                column: "WorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Tickets_TicketId",
                table: "Attachments",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Tickets_TicketId",
                table: "Comments",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_TicketHistories_Comments_CommentId",
                table: "TicketHistories",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Tickets_TicketId",
                table: "TicketHistories",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Transitions_TransitionId",
                table: "TicketHistories",
                column: "TransitionId",
                principalTable: "Transitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Priorities_PriorityId",
                table: "Tickets",
                column: "PriorityId",
                principalTable: "Priorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Projects_ProjectId",
                table: "Tickets",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_UserId",
                table: "Tickets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
