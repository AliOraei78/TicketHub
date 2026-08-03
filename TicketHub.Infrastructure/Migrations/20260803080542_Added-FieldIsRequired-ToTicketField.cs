using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedFieldIsRequiredToTicketField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "TicketFields",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "TicketFields");
        }
    }
}
