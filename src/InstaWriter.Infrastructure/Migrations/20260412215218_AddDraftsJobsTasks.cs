using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstaWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftsJobsTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentIdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(2200)", maxLength: 2200, nullable: false),
                    Script = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    CarouselCopyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HashtagSet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoverText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ComplianceScore = table.Column<double>(type: "float", nullable: true),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentDrafts_ContentIdeas_ContentIdeaId",
                        column: x => x.ContentIdeaId,
                        principalTable: "ContentIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Owner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaskType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublishJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelAccountId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlannedPublishDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalContainerId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalMediaId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublishJobs_ContentDrafts_ContentDraftId",
                        column: x => x.ContentDraftId,
                        principalTable: "ContentDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentDrafts_ContentIdeaId",
                table: "ContentDrafts",
                column: "ContentIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_PublishJobs_ContentDraftId",
                table: "PublishJobs",
                column: "ContentDraftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublishJobs");

            migrationBuilder.DropTable(
                name: "TaskItems");

            migrationBuilder.DropTable(
                name: "ContentDrafts");
        }
    }
}
