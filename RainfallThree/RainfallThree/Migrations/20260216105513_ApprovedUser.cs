using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RainfallThree.Migrations
{
    /// <inheritdoc />
    public partial class ApprovedUser : Migration
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
            
        }
    }
}
