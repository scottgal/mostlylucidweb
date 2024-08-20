using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mostlylucid.Migrations
{
    /// <inheritdoc />
    public partial class GIN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                schema: "Mostlylucid",
                table: "Categories",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Title_PlainTextContent",
                schema: "Mostlylucid",
                table: "BlogPosts",
                columns: new[] { "Title", "PlainTextContent" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                schema: "Mostlylucid",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_Title_PlainTextContent",
                schema: "Mostlylucid",
                table: "BlogPosts");
        }
    }
}
