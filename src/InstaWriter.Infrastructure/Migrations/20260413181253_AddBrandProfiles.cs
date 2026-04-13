using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstaWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrandProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VoiceGuide = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ToneGuide = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CTAStyle = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DisclaimerRules = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DefaultHashtagSets = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandProfiles");
        }
    }
}
