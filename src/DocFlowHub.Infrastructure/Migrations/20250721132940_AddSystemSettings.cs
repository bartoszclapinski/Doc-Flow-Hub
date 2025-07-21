using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DocFlowHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "String"),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequiresRestart = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "DataType", "DefaultValue", "Description", "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 1, "AI", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "String", "gpt-4o-mini", "Default OpenAI model for AI operations", "AI.OpenAI.Model", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "gpt-4o-mini" },
                    { 2, "AI", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Number", "4000", "Maximum tokens per AI request", "AI.OpenAI.MaxTokens", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "4000" },
                    { 3, "AI", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Number", "1000", "Daily AI usage limit per user", "AI.Usage.DailyLimit", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "1000" }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "DataType", "DefaultValue", "Description", "Key", "RequiresRestart", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 4, "Security", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Number", "60", "Session timeout in minutes", "Security.Session.TimeoutMinutes", true, new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "60" },
                    { 5, "Security", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean", "true", "Require digit in passwords", "Security.Password.RequireDigit", true, new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "true" },
                    { 6, "Security", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Number", "8", "Minimum password length", "Security.Password.RequiredLength", true, new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "8" }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "DataType", "DefaultValue", "Description", "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[] { 7, "Performance", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Number", "30", "Default cache expiration in minutes", "Performance.Cache.DefaultExpirationMinutes", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "30" });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "DataType", "DefaultValue", "Description", "Key", "RequiresRestart", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[] { 8, "Performance", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Number", "50", "Maximum file upload size in MB", "Performance.Upload.MaxFileSizeMB", true, new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "50" });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "DataType", "DefaultValue", "Description", "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 9, "System", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean", "true", "Allow public user registration", "System.Registration.AllowPublicRegistration", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "true" },
                    { 10, "System", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Boolean", "false", "Enable maintenance mode", "System.Maintenance.MaintenanceMode", new DateTime(2025, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "false" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Category",
                table: "SystemSettings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
