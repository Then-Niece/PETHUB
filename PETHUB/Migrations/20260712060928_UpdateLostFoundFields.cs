using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PETHUB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLostFoundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Breed",
                table: "LostFounds",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientContact",
                table: "LostFounds",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "LostFounds",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LostDate",
                table: "LostFounds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MemberId",
                table: "LostFounds",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PetType",
                table: "LostFounds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                table: "LostFounds",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Breed",
                table: "LostFounds");

            migrationBuilder.DropColumn(
                name: "ClientContact",
                table: "LostFounds");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "LostFounds");

            migrationBuilder.DropColumn(
                name: "LostDate",
                table: "LostFounds");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "LostFounds");

            migrationBuilder.DropColumn(
                name: "PetType",
                table: "LostFounds");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "LostFounds");
        }
    }
}
