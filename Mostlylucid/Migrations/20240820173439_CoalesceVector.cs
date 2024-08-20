using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Mostlylucid.Migrations
{
    /// <inheritdoc />
    public partial class CoalesceVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_Title_PlainTextContent",
                schema: "Mostlylucid",
                table: "BlogPosts");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                schema: "Mostlylucid",
                table: "BlogPosts",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_SearchVector",
                schema: "Mostlylucid",
                table: "BlogPosts",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_SearchVector",
                schema: "Mostlylucid",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                schema: "Mostlylucid",
                table: "BlogPosts");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Title_PlainTextContent",
                schema: "Mostlylucid",
                table: "BlogPosts",
                columns: new[] { "Title", "PlainTextContent" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");
        }
    }
}
