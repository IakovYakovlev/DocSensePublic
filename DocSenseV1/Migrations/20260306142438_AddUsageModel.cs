using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocSenseV1.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 3,
                column: "LimitRequests",
                value: 10000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 3,
                column: "LimitRequests",
                value: 100);
        }
    }
}
