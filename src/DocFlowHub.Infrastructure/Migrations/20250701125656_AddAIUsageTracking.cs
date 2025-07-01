using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocFlowHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAIUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIUsageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AIModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(10,6)", nullable: false),
                    ResponseTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    InputSize = table.Column<int>(type: "int", nullable: false),
                    OutputSize = table.Column<int>(type: "int", nullable: false),
                    ServedFromCache = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QualitySetting = table.Column<double>(type: "float", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIUsageLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AIUsageLogs_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_AIModel",
                table: "AIUsageLogs",
                column: "AIModel");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_CreatedAt",
                table: "AIUsageLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_DocumentId",
                table: "AIUsageLogs",
                column: "DocumentId",
                filter: "[DocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_OperationType",
                table: "AIUsageLogs",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_UserId",
                table: "AIUsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_UserId_CreatedAt",
                table: "AIUsageLogs",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIUsageLogs");
        }
    }
}
