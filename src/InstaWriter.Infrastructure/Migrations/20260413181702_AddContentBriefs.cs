using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstaWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentBriefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContentBriefId",
                table: "ContentDrafts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContentBriefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentIdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Audience = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HookDirection = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    KeyMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CTA = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequiresOriginalMedia = table.Column<bool>(type: "bit", nullable: false),
                    RequiresManualApproval = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentBriefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentBriefs_ContentIdeas_ContentIdeaId",
                        column: x => x.ContentIdeaId,
                        principalTable: "ContentIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentDrafts_ContentBriefId",
                table: "ContentDrafts",
                column: "ContentBriefId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentBriefs_ContentIdeaId",
                table: "ContentBriefs",
                column: "ContentIdeaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentDrafts_ContentBriefs_ContentBriefId",
                table: "ContentDrafts",
                column: "ContentBriefId",
                principalTable: "ContentBriefs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentDrafts_ContentBriefs_ContentBriefId",
                table: "ContentDrafts");

            migrationBuilder.DropTable(
                name: "ContentBriefs");

            migrationBuilder.DropIndex(
                name: "IX_ContentDrafts_ContentBriefId",
                table: "ContentDrafts");

            migrationBuilder.DropColumn(
                name: "ContentBriefId",
                table: "ContentDrafts");
        }
    }
}
