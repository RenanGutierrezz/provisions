using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RWMS.Migrations
{
    /// <inheritdoc />
    public partial class SimplifySupplyItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedUnitCost",
                table: "SupplyItems");

            migrationBuilder.DropColumn(
                name: "IsFulfilled",
                table: "SupplyItems");

            migrationBuilder.DropColumn(
                name: "QuantityOrdered",
                table: "SupplyItems");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "SupplyItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedUnitCost",
                table: "SupplyItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFulfilled",
                table: "SupplyItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuantityOrdered",
                table: "SupplyItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "SupplyItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
