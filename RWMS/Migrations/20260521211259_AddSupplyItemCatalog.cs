using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RWMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplyItemCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnList",
                table: "SupplyItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnList",
                table: "SupplyItems");
        }
    }
}
