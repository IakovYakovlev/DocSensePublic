using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocSenseV1.Migrations
{
    /// <inheritdoc />
    public partial class AddStatisticsToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "executionTimeMs",
                schema: "public",
                table: "Job",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "failedChunksCount",
                schema: "public",
                table: "Job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "successfulChunksCount",
                schema: "public",
                table: "Job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "totalChunks",
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
                name: "executionTimeMs",
                schema: "public",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "failedChunksCount",
                schema: "public",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "successfulChunksCount",
                schema: "public",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "totalChunks",
                schema: "public",
                table: "Job");
        }
    }
}
