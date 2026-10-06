using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RWMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplyItemCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "SupplyItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "SupplyItems");
        }
    }
}
