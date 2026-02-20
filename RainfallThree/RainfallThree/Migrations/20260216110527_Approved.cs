using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RainfallThree.Migrations
{
    /// <inheritdoc />
    public partial class Approved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rainfall_sheet");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rainfall_sheet",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false),
                    LATDEG = table.Column<byte>(type: "tinyint", nullable: false),
                    LATMIN = table.Column<byte>(type: "tinyint", nullable: false),
                    LONGDEG = table.Column<byte>(type: "tinyint", nullable: false),
                    LONGMIN = table.Column<byte>(type: "tinyint", nullable: false),
                    ReturnPeriod = table.Column<short>(type: "smallint", nullable: false),
                    Source_Sheet = table.Column<short>(type: "smallint", nullable: false),
                    _10080_min = table.Column<double>(type: "float", nullable: true),
                    _10_min = table.Column<double>(type: "float", nullable: true),
                    _120_min = table.Column<double>(type: "float", nullable: true),
                    _1440_min = table.Column<double>(type: "float", nullable: true),
                    _15_min = table.Column<double>(type: "float", nullable: true),
                    _30_min = table.Column<double>(type: "float", nullable: true),
                    _4320_min = table.Column<double>(type: "float", nullable: true),
                    _5_min = table.Column<double>(type: "float", nullable: true),
                    _60_min = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                });
        }
    }
}
