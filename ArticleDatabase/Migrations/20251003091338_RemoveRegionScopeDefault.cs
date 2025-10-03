using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArticleDatabase.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRegionScopeDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RegionScope",
                table: "articles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "GLOBAL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RegionScope",
                table: "articles",
                type: "text",
                nullable: false,
                defaultValue: "GLOBAL",
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
