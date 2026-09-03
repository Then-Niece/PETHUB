using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PETHUB.Migrations
{
    /// <inheritdoc />
    public partial class CreateAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Creates the AuditLogs table.
            // This is the only schema change needed because Appeals and
            // UserReports.AdminActionReason already exist in the database.
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    // Auto-incrementing primary key for each audit log record.
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    // Stores the Identity ID of the member or administrator.
                    UserId = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    // Stores the user's role when the event occurred.
                    Role = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    // Stores the general event being recorded.
                    // Examples: Logged In, Logged Out, Profile Updated.
                    Action = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    // Stores optional additional information about the event.
                    Description = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true),

                    // Stores the exact date and time of the event.
                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                }, 
                constraints: table =>
                {
                    // Sets Id as the primary key for AuditLogs.
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes only AuditLogs if this migration is rolled back.
            // Existing Appeals and UserReports data remain untouched.
            migrationBuilder.DropTable(
                name: "AuditLogs");
        }
    }
}