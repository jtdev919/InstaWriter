using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstaWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCarouselTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarouselTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TemplateType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PageCount = table.Column<int>(type: "int", nullable: false),
                    SlideLayoutsCsv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarouselTemplates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarouselTemplates");
        }
    }
}
