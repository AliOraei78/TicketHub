using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedneedapproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedApproval",
                table: "Statuses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedApproval",
                table: "Statuses");
        }
    }
}
