using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RWMS.Migrations
{
    /// <inheritdoc />
    public partial class LinkDeliveryToDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Deliveries");

            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DriverId",
                table: "Deliveries",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_DriverId",
                table: "Deliveries",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_Users_DriverId",
                table: "Deliveries",
                column: "DriverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_Users_DriverId",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_DriverId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "EmailNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Deliveries");

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Deliveries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
