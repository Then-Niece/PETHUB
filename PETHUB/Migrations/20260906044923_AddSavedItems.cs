using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PETHUB.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedPetFeeds_MemberId",
                table: "SavedPetFeeds");

            migrationBuilder.CreateTable(
                name: "SavedListings",
                columns: table => new
                {
                    SavedListingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ListingId = table.Column<int>(type: "int", nullable: false),
                    DateSaved = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedListings", x => x.SavedListingId);
                    table.ForeignKey(
                        name: "FK_SavedListings_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedListings_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "ListingId");
                });

            migrationBuilder.CreateTable(
                name: "SavedLostFounds",
                columns: table => new
                {
                    SavedLostFoundId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LostFoundId = table.Column<int>(type: "int", nullable: false),
                    DateSaved = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedLostFounds", x => x.SavedLostFoundId);
                    table.ForeignKey(
                        name: "FK_SavedLostFounds_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedLostFounds_LostFounds_LostFoundId",
                        column: x => x.LostFoundId,
                        principalTable: "LostFounds",
                        principalColumn: "LostFoundId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedPetFeeds_MemberId_PetFeedId",
                table: "SavedPetFeeds",
                columns: new[] { "MemberId", "PetFeedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedListings_ListingId",
                table: "SavedListings",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedListings_MemberId_ListingId",
                table: "SavedListings",
                columns: new[] { "MemberId", "ListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedLostFounds_LostFoundId",
                table: "SavedLostFounds",
                column: "LostFoundId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedLostFounds_MemberId_LostFoundId",
                table: "SavedLostFounds",
                columns: new[] { "MemberId", "LostFoundId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedListings");

            migrationBuilder.DropTable(
                name: "SavedLostFounds");

            migrationBuilder.DropIndex(
                name: "IX_SavedPetFeeds_MemberId_PetFeedId",
                table: "SavedPetFeeds");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPetFeeds_MemberId",
                table: "SavedPetFeeds",
                column: "MemberId");
        }
    }
}
