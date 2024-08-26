using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mostlylucid.Migrations
{
    /// <inheritdoc />
    public partial class Comments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_blogposts_blog_post_id",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_blog_post_id",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_date",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_moderated",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_slug",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "date",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "moderated",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "slug",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "mostlylucid",
                table: "comments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "blog_post_id",
                schema: "mostlylucid",
                table: "comments",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "avatar",
                schema: "mostlylucid",
                table: "comments",
                newName: "html_content");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                schema: "mostlylucid",
                table: "comments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "ParentCommentId",
                schema: "mostlylucid",
                table: "comments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "author",
                schema: "mostlylucid",
                table: "comments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                schema: "mostlylucid",
                table: "comments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "post_id",
                schema: "mostlylucid",
                table: "comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "comment_closures",
                schema: "mostlylucid",
                columns: table => new
                {
                    ancestor_id = table.Column<int>(type: "integer", nullable: false),
                    descendant_id = table.Column<int>(type: "integer", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comment_closures", x => new { x.ancestor_id, x.descendant_id });
                    table.ForeignKey(
                        name: "FK_comment_closures_comments_ancestor_id",
                        column: x => x.ancestor_id,
                        principalSchema: "mostlylucid",
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comment_closures_comments_descendant_id",
                        column: x => x.descendant_id,
                        principalSchema: "mostlylucid",
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_comments_author",
                schema: "mostlylucid",
                table: "comments",
                column: "author");

            migrationBuilder.CreateIndex(
                name: "IX_comments_ParentCommentId",
                schema: "mostlylucid",
                table: "comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_comments_post_id",
                schema: "mostlylucid",
                table: "comments",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_comment_closures_descendant_id",
                schema: "mostlylucid",
                table: "comment_closures",
                column: "descendant_id");

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
                name: "FK_comments_comments_ParentCommentId",
                schema: "mostlylucid",
                table: "comments",
                column: "ParentCommentId",
                principalSchema: "mostlylucid",
                principalTable: "comments",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_blogposts_post_id",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_comments_comments_ParentCommentId",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropTable(
                name: "comment_closures",
                schema: "mostlylucid");

            migrationBuilder.DropIndex(
                name: "IX_comments_author",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_ParentCommentId",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_post_id",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "author",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "post_id",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "mostlylucid",
                table: "comments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "mostlylucid",
                table: "comments",
                newName: "blog_post_id");

            migrationBuilder.RenameColumn(
                name: "html_content",
                schema: "mostlylucid",
                table: "comments",
                newName: "avatar");

            migrationBuilder.AlterColumn<string>(
                name: "content",
                schema: "mostlylucid",
                table: "comments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "date",
                schema: "mostlylucid",
                table: "comments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "mostlylucid",
                table: "comments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "moderated",
                schema: "mostlylucid",
                table: "comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "mostlylucid",
                table: "comments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "slug",
                schema: "mostlylucid",
                table: "comments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_comments_blog_post_id",
                schema: "mostlylucid",
                table: "comments",
                column: "blog_post_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_date",
                schema: "mostlylucid",
                table: "comments",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_comments_moderated",
                schema: "mostlylucid",
                table: "comments",
                column: "moderated");

            migrationBuilder.CreateIndex(
                name: "IX_comments_slug",
                schema: "mostlylucid",
                table: "comments",
                column: "slug");

            migrationBuilder.AddForeignKey(
                name: "FK_comments_blogposts_blog_post_id",
                schema: "mostlylucid",
                table: "comments",
                column: "blog_post_id",
                principalSchema: "mostlylucid",
                principalTable: "blogposts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
