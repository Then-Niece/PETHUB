using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PETHUB.Migrations
{
    /// <inheritdoc />
    public partial class RenameLostFoundUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LostFounds_AspNetUsers_UserID",
                table: "LostFounds");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "LostFounds",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LostFounds_UserID",
                table: "LostFounds",
                newName: "IX_LostFounds_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LostFounds_AspNetUsers_UserId",
                table: "LostFounds",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LostFounds_AspNetUsers_UserId",
                table: "LostFounds");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "LostFounds",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_LostFounds_UserId",
                table: "LostFounds",
                newName: "IX_LostFounds_UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_LostFounds_AspNetUsers_UserID",
                table: "LostFounds",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
