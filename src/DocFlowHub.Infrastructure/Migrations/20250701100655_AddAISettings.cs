using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocFlowHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAISettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AISettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PreferredModel = table.Column<int>(type: "int", nullable: false),
                    CustomApiKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UseCustomApiKey = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SummarizationEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    VersionComparisonEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AutoSummarizeOnUpload = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    QualityPreference = table.Column<double>(type: "float(3)", precision: 3, scale: 2, nullable: false, defaultValue: 0.69999999999999996),
                    MaxTokensPerOperation = table.Column<int>(type: "int", nullable: false, defaultValue: 2000),
                    ComparisonSensitivity = table.Column<double>(type: "float(3)", precision: 3, scale: 2, nullable: false, defaultValue: 0.5),
                    EnableCaching = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CacheDurationHours = table.Column<int>(type: "int", nullable: false, defaultValue: 24),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISettings", x => x.Id);
                    table.CheckConstraint("CK_AISettings_CacheDurationHours", "[CacheDurationHours] >= 1 AND [CacheDurationHours] <= 168");
                    table.CheckConstraint("CK_AISettings_ComparisonSensitivity", "[ComparisonSensitivity] >= 0.0 AND [ComparisonSensitivity] <= 1.0");
                    table.CheckConstraint("CK_AISettings_MaxTokensPerOperation", "[MaxTokensPerOperation] >= 100 AND [MaxTokensPerOperation] <= 10000");
                    table.CheckConstraint("CK_AISettings_QualityPreference", "[QualityPreference] >= 0.0 AND [QualityPreference] <= 1.0");
                    table.ForeignKey(
                        name: "FK_AISettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AISettings_CreatedAt",
                table: "AISettings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AISettings_IsActive",
                table: "AISettings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AISettings_UserId",
                table: "AISettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AISettings");
        }
    }
}
