using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PETHUB.Migrations
{
    /// <inheritdoc />
    public partial class AddRStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RStatus",
                table: "LostFounds",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RStatus",
                table: "LostFounds");
        }
    }
}
