using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RainfallThree.Migrations
{
    /// <inheritdoc />
    public partial class Latest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

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
                    _5_min = table.Column<double>(type: "float", nullable: true),
                    _10_min = table.Column<double>(type: "float", nullable: true),
                    _15_min = table.Column<double>(type: "float", nullable: true),
                    _30_min = table.Column<double>(type: "float", nullable: true),
                    _60_min = table.Column<double>(type: "float", nullable: true),
                    _120_min = table.Column<double>(type: "float", nullable: true),
                    _1440_min = table.Column<double>(type: "float", nullable: true),
                    _4320_min = table.Column<double>(type: "float", nullable: true),
                    _10080_min = table.Column<double>(type: "float", nullable: true),
                    Source_Sheet = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rainfall_sheet");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
