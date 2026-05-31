using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeUserAPI.Migrations
{
    /// <inheritdoc />
    public partial class avlshr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AvailableShares",
                table: "Stocks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalShares",
                table: "Stocks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableShares",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "TotalShares",
                table: "Stocks");
        }
    }
}
