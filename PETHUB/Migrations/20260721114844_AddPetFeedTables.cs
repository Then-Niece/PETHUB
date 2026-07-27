using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PETHUB.Migrations
{
    /// <inheritdoc />
    public partial class AddPetFeedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PetFeeds",
                columns: table => new
                {
                    PetFeedId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PawCount = table.Column<int>(type: "int", nullable: false),
                    AdminId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetFeeds", x => x.PetFeedId);
                    table.ForeignKey(
                        name: "FK_PetFeeds_AspNetUsers_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PetFeedComments",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatePosted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PetFeedId = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetFeedComments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_PetFeedComments_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PetFeedComments_PetFeeds_PetFeedId",
                        column: x => x.PetFeedId,
                        principalTable: "PetFeeds",
                        principalColumn: "PetFeedId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PetFeedImages",
                columns: table => new
                {
                    PetFeedImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PetFeedId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetFeedImages", x => x.PetFeedImageId);
                    table.ForeignKey(
                        name: "FK_PetFeedImages_PetFeeds_PetFeedId",
                        column: x => x.PetFeedId,
                        principalTable: "PetFeeds",
                        principalColumn: "PetFeedId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedPetFeeds",
                columns: table => new
                {
                    SavedPetFeedId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PetFeedId = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPetFeeds", x => x.SavedPetFeedId);
                    table.ForeignKey(
                        name: "FK_SavedPetFeeds_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedPetFeeds_PetFeeds_PetFeedId",
                        column: x => x.PetFeedId,
                        principalTable: "PetFeeds",
                        principalColumn: "PetFeedId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PetFeedComments_MemberId",
                table: "PetFeedComments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PetFeedComments_PetFeedId",
                table: "PetFeedComments",
                column: "PetFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_PetFeedImages_PetFeedId",
                table: "PetFeedImages",
                column: "PetFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_PetFeeds_AdminId",
                table: "PetFeeds",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPetFeeds_MemberId",
                table: "SavedPetFeeds",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPetFeeds_PetFeedId",
                table: "SavedPetFeeds",
                column: "PetFeedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetFeedComments");

            migrationBuilder.DropTable(
                name: "PetFeedImages");

            migrationBuilder.DropTable(
                name: "SavedPetFeeds");

            migrationBuilder.DropTable(
                name: "PetFeeds");
        }
    }
}
