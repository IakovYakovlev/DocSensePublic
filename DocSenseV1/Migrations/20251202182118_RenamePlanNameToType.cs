using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocSenseV1.Migrations
{
    /// <inheritdoc />
    public partial class RenamePlanNameToType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Plans",
                newName: "Type");

            migrationBuilder.RenameIndex(
                name: "IX_Plans_Name",
                table: "Plans",
                newName: "IX_Plans_Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Plans",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_Plans_Type",
                table: "Plans",
                newName: "IX_Plans_Name");
        }
    }
}
