using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mostlylucid.Migrations
{
    /// <inheritdoc />
    public partial class ParentCommentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_comments_ParentCommentId",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "ParentCommentId",
                schema: "mostlylucid",
                table: "comments",
                newName: "parent_comment_id");

            migrationBuilder.RenameIndex(
                name: "IX_comments_ParentCommentId",
                schema: "mostlylucid",
                table: "comments",
                newName: "IX_comments_parent_comment_id");

            migrationBuilder.AddForeignKey(
                name: "FK_comments_comments_parent_comment_id",
                schema: "mostlylucid",
                table: "comments",
                column: "parent_comment_id",
                principalSchema: "mostlylucid",
                principalTable: "comments",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_comments_parent_comment_id",
                schema: "mostlylucid",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "parent_comment_id",
                schema: "mostlylucid",
                table: "comments",
                newName: "ParentCommentId");

            migrationBuilder.RenameIndex(
                name: "IX_comments_parent_comment_id",
                schema: "mostlylucid",
                table: "comments",
                newName: "IX_comments_ParentCommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_comments_comments_ParentCommentId",
                schema: "mostlylucid",
                table: "comments",
                column: "ParentCommentId",
                principalSchema: "mostlylucid",
                principalTable: "comments",
                principalColumn: "id");
        }
    }
}
