using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spottify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "download_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QueryText = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Artist = table.Column<string>(type: "TEXT", nullable: true),
                    AlbumTitle = table.Column<string>(type: "TEXT", nullable: true),
                    MusicBrainzReleaseGroupId = table.Column<string>(type: "TEXT", nullable: true),
                    MusicBrainzReleaseId = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedReleaseTitle = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedNzbUrl = table.Column<string>(type: "TEXT", nullable: true),
                    DownloaderJobId = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_download_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_download_requests_MusicBrainzReleaseGroupId",
                table: "download_requests",
                column: "MusicBrainzReleaseGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_download_requests_Status",
                table: "download_requests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "download_requests");
        }
    }
}
