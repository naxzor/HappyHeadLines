using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArticleDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_articles_PublishedAt",
                table: "articles");

            migrationBuilder.AddColumn<string>(
                name: "RegionScope",
                table: "articles",
                type: "text",
                nullable: false,
                defaultValue: "GLOBAL");

            migrationBuilder.CreateIndex(
                name: "IX_articles_RegionScope_PublishedAt",
                table: "articles",
                columns: new[] { "RegionScope", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_articles_RegionScope_PublishedAt",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "RegionScope",
                table: "articles");

            migrationBuilder.CreateIndex(
                name: "IX_articles_PublishedAt",
                table: "articles",
                column: "PublishedAt");
        }
    }
}
