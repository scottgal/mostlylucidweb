using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mostlylucid.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Mostlylucid");

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "Mostlylucid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                schema: "Mostlylucid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlogPosts",
                schema: "Mostlylucid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    PlainTextContent = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "text", nullable: false),
                    WordCount = table.Column<int>(type: "integer", nullable: false),
                    LanguageId = table.Column<int>(type: "integer", nullable: false),
                    PublishedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlogPosts_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "Mostlylucid",
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlogPostCategory",
                schema: "Mostlylucid",
                columns: table => new
                {
                    BlogPostId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPostCategory", x => new { x.BlogPostId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_BlogPostCategory_BlogPosts_BlogPostId",
                        column: x => x.BlogPostId,
                        principalSchema: "Mostlylucid",
                        principalTable: "BlogPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlogPostCategory_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "Mostlylucid",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                schema: "Mostlylucid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Moderated = table.Column<bool>(type: "boolean", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    BlogPostId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_BlogPosts_BlogPostId",
                        column: x => x.BlogPostId,
                        principalSchema: "Mostlylucid",
                        principalTable: "BlogPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlogPostCategory_CategoryId",
                schema: "Mostlylucid",
                table: "BlogPostCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_ContentHash",
                schema: "Mostlylucid",
                table: "BlogPosts",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_LanguageId",
                schema: "Mostlylucid",
                table: "BlogPosts",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_PublishedDate",
                schema: "Mostlylucid",
                table: "BlogPosts",
                column: "PublishedDate");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Slug_LanguageId",
                schema: "Mostlylucid",
                table: "BlogPosts",
                columns: new[] { "Slug", "LanguageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BlogPostId",
                schema: "Mostlylucid",
                table: "Comments",
                column: "BlogPostId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Date",
                schema: "Mostlylucid",
                table: "Comments",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Moderated",
                schema: "Mostlylucid",
                table: "Comments",
                column: "Moderated");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_Slug",
                schema: "Mostlylucid",
                table: "Comments",
                column: "Slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogPostCategory",
                schema: "Mostlylucid");

            migrationBuilder.DropTable(
                name: "Comments",
                schema: "Mostlylucid");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "Mostlylucid");

            migrationBuilder.DropTable(
                name: "BlogPosts",
                schema: "Mostlylucid");

            migrationBuilder.DropTable(
                name: "Languages",
                schema: "Mostlylucid");
        }
    }
}
