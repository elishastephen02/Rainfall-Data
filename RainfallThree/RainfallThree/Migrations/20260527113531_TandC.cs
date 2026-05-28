using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RainfallThree.Migrations
{
    /// <inheritdoc />
    public partial class TandC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedTermsVersion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAcceptedTerms",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedOn",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedTermsVersion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HasAcceptedTerms",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedOn",
                table: "AspNetUsers");
        }
    }
}
