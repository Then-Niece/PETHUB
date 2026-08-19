using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PETHUB.Migrations
{
    /// <inheritdoc />
    public partial class FixUserDeletionRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_AspNetUsers_MemberId",
                table: "Listings");

            migrationBuilder.DropForeignKey(
                name: "FK_LostFounds_AspNetUsers_UserId",
                table: "LostFounds");

            migrationBuilder.DropForeignKey(
                name: "FK_PetFeedComments_AspNetUsers_MemberId",
                table: "PetFeedComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PetFeedPaws_AspNetUsers_MemberId",
                table: "PetFeedPaws");

            migrationBuilder.DropForeignKey(
                name: "FK_PetFeeds_AspNetUsers_AdminId",
                table: "PetFeeds");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedPetFeeds_AspNetUsers_MemberId",
                table: "SavedPetFeeds");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReports_Listings_ListingId",
                table: "UserReports");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReports_LostFounds_LostFoundId",
                table: "UserReports");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");


            migrationBuilder.AlterColumn<string>(
                name: "AdminId",
                table: "PetFeeds",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_AspNetUsers_MemberId",
                table: "Listings",
                column: "MemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LostFounds_AspNetUsers_UserId",
                table: "LostFounds",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PetFeedComments_AspNetUsers_MemberId",
                table: "PetFeedComments",
                column: "MemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PetFeedPaws_AspNetUsers_MemberId",
                table: "PetFeedPaws",
                column: "MemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PetFeeds_AspNetUsers_AdminId",
                table: "PetFeeds",
                column: "AdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPetFeeds_AspNetUsers_MemberId",
                table: "SavedPetFeeds",
                column: "MemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReports_Listings_ListingId",
                table: "UserReports",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "ListingId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReports_LostFounds_LostFoundId",
                table: "UserReports",
                column: "LostFoundId",
                principalTable: "LostFounds",
                principalColumn: "LostFoundId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_AspNetUsers_MemberId",
                table: "Listings");

            migrationBuilder.DropForeignKey(
                name: "FK_LostFounds_AspNetUsers_UserId",
                table: "LostFounds");

            migrationBuilder.DropForeignKey(
                name: "FK_PetFeedComments_AspNetUsers_MemberId",
                table: "PetFeedComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PetFeedPaws_AspNetUsers_MemberId",
                table: "PetFeedPaws");

            migrationBuilder.DropForeignKey(
                name: "FK_PetFeeds_AspNetUsers_AdminId",
                table: "PetFeeds");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedPetFeeds_AspNetUsers_MemberId",
                table: "SavedPetFeeds");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReports_Listings_ListingId",
                table: "UserReports");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReports_LostFounds_LostFoundId",
                table: "UserReports");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");


            migrationBuilder.AlterColumn<string>(
                name: "AdminId",
                table: "PetFeeds",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_AspNetUsers_MemberId",
                table: "Listings",
                column: "MemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LostFounds_AspNetUsers_UserId",
                table: "LostFounds",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PetFeedComments_AspNetUsers_MemberId",
                table: "PetFeedComments",
                column: "MemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PetFeedPaws_AspNetUsers_MemberId",
                table: "PetFeedPaws",
                column: "MemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PetFeeds_AspNetUsers_AdminId",
                table: "PetFeeds",
                column: "AdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPetFeeds_AspNetUsers_MemberId",
                table: "SavedPetFeeds",
                column: "MemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReports_Listings_ListingId",
                table: "UserReports",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "ListingId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserReports_LostFounds_LostFoundId",
                table: "UserReports",
                column: "LostFoundId",
                principalTable: "LostFounds",
                principalColumn: "LostFoundId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

        }
    }
}
