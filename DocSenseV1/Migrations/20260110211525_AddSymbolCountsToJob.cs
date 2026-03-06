using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocSenseV1.Migrations
{
    /// <inheritdoc />
    public partial class AddSymbolCountsToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "resultSymbolsCount",
                schema: "public",
                table: "Job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "symbolsCount",
                schema: "public",
                table: "Job",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "resultSymbolsCount",
                schema: "public",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "symbolsCount",
                schema: "public",
                table: "Job");
        }
    }
}
