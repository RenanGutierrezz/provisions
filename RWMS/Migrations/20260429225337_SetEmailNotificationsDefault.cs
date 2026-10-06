using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RWMS.Migrations
{
    /// <inheritdoc />
    public partial class SetEmailNotificationsDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Users] SET [EmailNotificationsEnabled] = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
