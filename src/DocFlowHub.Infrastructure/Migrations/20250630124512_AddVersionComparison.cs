using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocFlowHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionComparison : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VersionComparisons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    FromVersionId = table.Column<int>(type: "int", nullable: false),
                    ToVersionId = table.Column<int>(type: "int", nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DetailedChanges = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    Significance = table.Column<int>(type: "int", nullable: false),
                    AIModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float(3)", precision: 3, scale: 2, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessingTimeMs = table.Column<int>(type: "int", nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionComparisons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VersionComparisons_DocumentId",
                table: "VersionComparisons",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_VersionComparisons_DocumentId_FromVersionId_ToVersionId",
                table: "VersionComparisons",
                columns: new[] { "DocumentId", "FromVersionId", "ToVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersionComparisons_FromVersionId",
                table: "VersionComparisons",
                column: "FromVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VersionComparisons_GeneratedAt",
                table: "VersionComparisons",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VersionComparisons_Significance",
                table: "VersionComparisons",
                column: "Significance");

            migrationBuilder.CreateIndex(
                name: "IX_VersionComparisons_ToVersionId",
                table: "VersionComparisons",
                column: "ToVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VersionComparisons");
        }
    }
}
