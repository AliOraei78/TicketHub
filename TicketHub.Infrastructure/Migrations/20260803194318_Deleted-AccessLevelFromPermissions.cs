using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeletedAccessLevelFromPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "Permissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "Permissions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
