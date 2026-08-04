using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PETHUB.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Location",
                table: "LostFounds",
                newName: "Province");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Listings",
                newName: "Province");

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "LostFounds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "LostFounds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "LostFounds",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "Listings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "LostFounds");

            migrationBuilder.DropColumn(
                name: "City",
                table: "LostFounds");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "LostFounds");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "Listings");

            migrationBuilder.RenameColumn(
                name: "Province",
                table: "LostFounds",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "Province",
                table: "Listings",
                newName: "Location");
        }
    }
}
