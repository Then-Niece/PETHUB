using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PETHUB.Migrations
{
    /// <inheritdoc />
    public partial class AddPetFeedPawAndType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PawCount",
                table: "PetFeeds",
                newName: "Type");

            migrationBuilder.CreateTable(
                name: "PetFeedPaws",
                columns: table => new
                {
                    PetFeedPawId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PetFeedId = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DatePawed = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetFeedPaws", x => x.PetFeedPawId);
                    table.ForeignKey(
                        name: "FK_PetFeedPaws_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PetFeedPaws_PetFeeds_PetFeedId",
                        column: x => x.PetFeedId,
                        principalTable: "PetFeeds",
                        principalColumn: "PetFeedId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PetFeedPaws_MemberId",
                table: "PetFeedPaws",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PetFeedPaws_PetFeedId",
                table: "PetFeedPaws",
                column: "PetFeedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetFeedPaws");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "PetFeeds",
                newName: "PawCount");
        }
    }
}
