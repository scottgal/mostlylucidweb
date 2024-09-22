using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mostlylucid.Migrations
{
    /// <inheritdoc />
    public partial class EmailSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSubscriptions",
                schema: "mostlylucid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubscriptionType = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSubscription_Category",
                schema: "mostlylucid");

            migrationBuilder.DropTable(
                name: "EmailSubscriptions",
                schema: "mostlylucid");
        }
    }
}
