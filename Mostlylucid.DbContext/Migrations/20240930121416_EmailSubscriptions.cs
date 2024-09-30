using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Mostlylucid.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class EmailSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blogpostcategory_blogposts_BlogPostId",
                schema: "mostlylucid",
                table: "blogpostcategory");

            migrationBuilder.DropForeignKey(
                name: "FK_blogpostcategory_categories_CategoryId",
                schema: "mostlylucid",
                table: "blogpostcategory");

            migrationBuilder.DropForeignKey(
                name: "FK_blogposts_languages_language_id",
                schema: "mostlylucid",
                table: "blogposts");

            migrationBuilder.DropForeignKey(
                name: "FK_comment_closures_comments_ancestor_id",
                schema: "mostlylucid",
                table: "comment_closures");

            migrationBuilder.DropForeignKey(
                name: "FK_comment_closures_comments_descendant_id",
                schema: "mostlylucid",
                table: "comment_closures");

            migrationBuilder.DropForeignKey(
                name: "FK_comments_blogposts_post_id",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_comments_comments_parent_comment_id",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_languages",
                schema: "mostlylucid",
                table: "languages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_comments",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_categories",
                schema: "mostlylucid",
                table: "categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_blogposts",
                schema: "mostlylucid",
                table: "blogposts");

            migrationBuilder.RenameTable(
                name: "languages",
                schema: "mostlylucid",
                newName: "Languages",
                newSchema: "mostlylucid");

            migrationBuilder.RenameTable(
                name: "comments",
                schema: "mostlylucid",
                newName: "Comments",
                newSchema: "mostlylucid");

            migrationBuilder.RenameTable(
                name: "categories",
                schema: "mostlylucid",
                newName: "Categories",
                newSchema: "mostlylucid");

            migrationBuilder.RenameTable(
                name: "blogposts",
                schema: "mostlylucid",
                newName: "BlogPosts",
                newSchema: "mostlylucid");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "mostlylucid",
                table: "Languages",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "mostlylucid",
                table: "Languages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "mostlylucid",
                table: "Comments",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "content",
                schema: "mostlylucid",
                table: "Comments",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "author",
                schema: "mostlylucid",
                table: "Comments",
                newName: "Author");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "mostlylucid",
                table: "Comments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "post_id",
                schema: "mostlylucid",
                table: "Comments",
                newName: "PostId");

            migrationBuilder.RenameColumn(
                name: "html_content",
                schema: "mostlylucid",
                table: "Comments",
                newName: "HtmlContent");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "mostlylucid",
                table: "Comments",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_comments_parent_comment_id",
                schema: "mostlylucid",
                table: "Comments",
                newName: "IX_Comments_parent_comment_id");

            migrationBuilder.RenameIndex(
                name: "IX_comments_author",
                schema: "mostlylucid",
                table: "Comments",
                newName: "IX_Comments_Author");

            migrationBuilder.RenameIndex(
                name: "IX_comments_post_id",
                schema: "mostlylucid",
                table: "Comments",
                newName: "IX_Comments_PostId");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "mostlylucid",
                table: "Categories",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "mostlylucid",
                table: "Categories",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_categories_name",
                schema: "mostlylucid",
                table: "Categories",
                newName: "IX_Categories_Name");

            migrationBuilder.RenameColumn(
                name: "title",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "slug",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "markdown",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "Markdown");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "word_count",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "WordCount");

            migrationBuilder.RenameColumn(
                name: "updated_date",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "UpdatedDate");

            migrationBuilder.RenameColumn(
                name: "search_vector",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "SearchVector");

            migrationBuilder.RenameColumn(
                name: "published_date",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "PublishedDate");

            migrationBuilder.RenameColumn(
                name: "plain_text_content",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "PlainTextContent");

            migrationBuilder.RenameColumn(
                name: "language_id",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "LanguageId");

            migrationBuilder.RenameColumn(
                name: "html_content",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "HtmlContent");

            migrationBuilder.RenameColumn(
                name: "content_hash",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "ContentHash");

            migrationBuilder.RenameIndex(
                name: "IX_blogposts_slug_language_id",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "IX_BlogPosts_Slug_LanguageId");

            migrationBuilder.RenameIndex(
                name: "IX_blogposts_search_vector",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "IX_BlogPosts_SearchVector");

            migrationBuilder.RenameIndex(
                name: "IX_blogposts_published_date",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "IX_BlogPosts_PublishedDate");

            migrationBuilder.RenameIndex(
                name: "IX_blogposts_language_id",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "IX_BlogPosts_LanguageId");

            migrationBuilder.RenameIndex(
                name: "IX_blogposts_content_hash",
                schema: "mostlylucid",
                table: "BlogPosts",
                newName: "IX_BlogPosts_ContentHash");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedDate",
                schema: "mostlylucid",
                table: "BlogPosts",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                schema: "mostlylucid",
                table: "BlogPosts",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "to_tsvector('english', coalesce(title, '') || ' ' || coalesce(plain_text_content, ''))",
                oldStored: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Languages",
                schema: "mostlylucid",
                table: "Languages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comments",
                schema: "mostlylucid",
                table: "Comments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                schema: "mostlylucid",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BlogPosts",
                schema: "mostlylucid",
                table: "BlogPosts",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "EmailSubscriptions",
                schema: "mostlylucid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubscriptionType = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", maxLength: 100, nullable: false),
                    LastSent = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    Day = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailSubscriptionSendLogs",
                schema: "mostlylucid",
                columns: table => new
                {
                    SubscriptionType = table.Column<string>(type: "varchar(24)", nullable: false),
                    LastSent = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSubscriptionSendLogs", x => x.SubscriptionType);
                });

            migrationBuilder.CreateTable(
                name: "EmailSubscription_Category",
                schema: "mostlylucid",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    EmailSubscriptionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSubscription_Category", x => new { x.CategoryId, x.EmailSubscriptionId });
                    table.ForeignKey(
                        name: "FK_EmailSubscription_Category_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "mostlylucid",
                        principalTable: "Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmailSubscription_Category_EmailSubscriptions_EmailSubscrip~",
                        column: x => x.EmailSubscriptionId,
                        principalSchema: "mostlylucid",
                        principalTable: "EmailSubscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSubscription_Category_EmailSubscriptionId",
                schema: "mostlylucid",
                table: "EmailSubscription_Category",
                column: "EmailSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSubscriptions_Email",
                schema: "mostlylucid",
                table: "EmailSubscriptions",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSubscriptions_Token",
                schema: "mostlylucid",
                table: "EmailSubscriptions",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_blogpostcategory_BlogPosts_BlogPostId",
                schema: "mostlylucid",
                table: "blogpostcategory",
                column: "BlogPostId",
                principalSchema: "mostlylucid",
                principalTable: "BlogPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_blogpostcategory_Categories_CategoryId",
                schema: "mostlylucid",
                table: "blogpostcategory",
                column: "CategoryId",
                principalSchema: "mostlylucid",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BlogPosts_Languages_LanguageId",
                schema: "mostlylucid",
                table: "BlogPosts",
                column: "LanguageId",
                principalSchema: "mostlylucid",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_comment_closures_Comments_ancestor_id",
                schema: "mostlylucid",
                table: "comment_closures",
                column: "ancestor_id",
                principalSchema: "mostlylucid",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_comment_closures_Comments_descendant_id",
                schema: "mostlylucid",
                table: "comment_closures",
                column: "descendant_id",
                principalSchema: "mostlylucid",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_BlogPosts_PostId",
                schema: "mostlylucid",
                table: "Comments",
                column: "PostId",
                principalSchema: "mostlylucid",
                principalTable: "BlogPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Comments_parent_comment_id",
                schema: "mostlylucid",
                table: "Comments",
                column: "parent_comment_id",
                principalSchema: "mostlylucid",
                principalTable: "Comments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blogpostcategory_BlogPosts_BlogPostId",
                schema: "mostlylucid",
                table: "blogpostcategory");

            migrationBuilder.DropForeignKey(
                name: "FK_blogpostcategory_Categories_CategoryId",
                schema: "mostlylucid",
                table: "blogpostcategory");

            migrationBuilder.DropForeignKey(
                name: "FK_BlogPosts_Languages_LanguageId",
                schema: "mostlylucid",
                table: "BlogPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_comment_closures_Comments_ancestor_id",
                schema: "mostlylucid",
                table: "comment_closures");

            migrationBuilder.DropForeignKey(
                name: "FK_comment_closures_Comments_descendant_id",
                schema: "mostlylucid",
                table: "comment_closures");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_BlogPosts_PostId",
                schema: "mostlylucid",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Comments_parent_comment_id",
                schema: "mostlylucid",
                table: "Comments");

            migrationBuilder.DropTable(
                name: "EmailSubscription_Category",
                schema: "mostlylucid");

            migrationBuilder.DropTable(
                name: "EmailSubscriptionSendLogs",
                schema: "mostlylucid");

            migrationBuilder.DropTable(
                name: "EmailSubscriptions",
                schema: "mostlylucid");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Languages",
                schema: "mostlylucid",
                table: "Languages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comments",
                schema: "mostlylucid",
                table: "Comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                schema: "mostlylucid",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BlogPosts",
                schema: "mostlylucid",
                table: "BlogPosts");

            migrationBuilder.RenameTable(
                name: "Languages",
                schema: "mostlylucid",
                newName: "languages",
                newSchema: "mostlylucid");

            migrationBuilder.RenameTable(
                name: "Comments",
                schema: "mostlylucid",
                newName: "comments",
                newSchema: "mostlylucid");

            migrationBuilder.RenameTable(
                name: "Categories",
                schema: "mostlylucid",
                newName: "categories",
                newSchema: "mostlylucid");

            migrationBuilder.RenameTable(
                name: "BlogPosts",
                schema: "mostlylucid",
                newName: "blogposts",
                newSchema: "mostlylucid");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "mostlylucid",
                table: "languages",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "mostlylucid",
                table: "languages",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "mostlylucid",
                table: "comments",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Content",
                schema: "mostlylucid",
                table: "comments",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Author",
                schema: "mostlylucid",
                table: "comments",
                newName: "author");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "mostlylucid",
                table: "comments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PostId",
                schema: "mostlylucid",
                table: "comments",
                newName: "post_id");

            migrationBuilder.RenameColumn(
                name: "HtmlContent",
                schema: "mostlylucid",
                table: "comments",
                newName: "html_content");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "mostlylucid",
                table: "comments",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_parent_comment_id",
                schema: "mostlylucid",
                table: "comments",
                newName: "IX_comments_parent_comment_id");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_Author",
                schema: "mostlylucid",
                table: "comments",
                newName: "IX_comments_author");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_PostId",
                schema: "mostlylucid",
                table: "comments",
                newName: "IX_comments_post_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "mostlylucid",
                table: "categories",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "mostlylucid",
                table: "categories",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Categories_Name",
                schema: "mostlylucid",
                table: "categories",
                newName: "IX_categories_name");

            migrationBuilder.RenameColumn(
                name: "Title",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Slug",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Markdown",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "markdown");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WordCount",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "word_count");

            migrationBuilder.RenameColumn(
                name: "UpdatedDate",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "updated_date");

            migrationBuilder.RenameColumn(
                name: "SearchVector",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "search_vector");

            migrationBuilder.RenameColumn(
                name: "PublishedDate",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "published_date");

            migrationBuilder.RenameColumn(
                name: "PlainTextContent",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "plain_text_content");

            migrationBuilder.RenameColumn(
                name: "LanguageId",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "language_id");

            migrationBuilder.RenameColumn(
                name: "HtmlContent",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "html_content");

            migrationBuilder.RenameColumn(
                name: "ContentHash",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "content_hash");

            migrationBuilder.RenameIndex(
                name: "IX_BlogPosts_Slug_LanguageId",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "IX_blogposts_slug_language_id");

            migrationBuilder.RenameIndex(
                name: "IX_BlogPosts_SearchVector",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "IX_blogposts_search_vector");

            migrationBuilder.RenameIndex(
                name: "IX_BlogPosts_PublishedDate",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "IX_blogposts_published_date");

            migrationBuilder.RenameIndex(
                name: "IX_BlogPosts_LanguageId",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "IX_blogposts_language_id");

            migrationBuilder.RenameIndex(
                name: "IX_BlogPosts_ContentHash",
                schema: "mostlylucid",
                table: "blogposts",
                newName: "IX_blogposts_content_hash");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_date",
                schema: "mostlylucid",
                table: "blogposts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "mostlylucid",
                table: "blogposts",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', coalesce(title, '') || ' ' || coalesce(plain_text_content, ''))",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))",
                oldStored: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_languages",
                schema: "mostlylucid",
                table: "languages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_comments",
                schema: "mostlylucid",
                table: "comments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_categories",
                schema: "mostlylucid",
                table: "categories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_blogposts",
                schema: "mostlylucid",
                table: "blogposts",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_blogpostcategory_blogposts_BlogPostId",
                schema: "mostlylucid",
                table: "blogpostcategory",
                column: "BlogPostId",
                principalSchema: "mostlylucid",
                principalTable: "blogposts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_blogpostcategory_categories_CategoryId",
                schema: "mostlylucid",
                table: "blogpostcategory",
                column: "CategoryId",
                principalSchema: "mostlylucid",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_blogposts_languages_language_id",
                schema: "mostlylucid",
                table: "blogposts",
                column: "language_id",
                principalSchema: "mostlylucid",
                principalTable: "languages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_comment_closures_comments_ancestor_id",
                schema: "mostlylucid",
                table: "comment_closures",
                column: "ancestor_id",
                principalSchema: "mostlylucid",
                principalTable: "comments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_comment_closures_comments_descendant_id",
                schema: "mostlylucid",
                table: "comment_closures",
                column: "descendant_id",
                principalSchema: "mostlylucid",
                principalTable: "comments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_comments_blogposts_post_id",
                schema: "mostlylucid",
                table: "comments",
                column: "post_id",
                principalSchema: "mostlylucid",
                principalTable: "blogposts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_comments_comments_parent_comment_id",
                schema: "mostlylucid",
                table: "comments",
                column: "parent_comment_id",
                principalSchema: "mostlylucid",
                principalTable: "comments",
                principalColumn: "id");
        }
    }
}
