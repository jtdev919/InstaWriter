using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstaWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BlobUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ThumbnailUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Owner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PillarName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContentIdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContentDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_ContentDrafts_ContentDraftId",
                        column: x => x.ContentDraftId,
                        principalTable: "ContentDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Assets_ContentIdeas_ContentIdeaId",
                        column: x => x.ContentIdeaId,
                        principalTable: "ContentIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ContentDraftId",
                table: "Assets",
                column: "ContentDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ContentIdeaId",
                table: "Assets",
                column: "ContentIdeaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assets");
        }
    }
}
