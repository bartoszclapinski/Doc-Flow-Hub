using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocFlowHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentVersionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentCategories_DocumentCategories_ParentCategoryId",
                table: "DocumentCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_AspNetUsers_UserId",
                table: "DocumentVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_Documents_DocumentId",
                table: "DocumentVersions");

            migrationBuilder.DropIndex(
                name: "IX_DocumentVersions_UserId",
                table: "DocumentVersions");

            migrationBuilder.RenameColumn(
                name: "ParentCategoryId",
                table: "DocumentCategories",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentCategories_ParentCategoryId",
                table: "DocumentCategories",
                newName: "IX_DocumentCategories_ParentId");

            migrationBuilder.AlterColumn<string>(
                name: "FileHash",
                table: "DocumentVersions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<int>(
                name: "DocumentId",
                table: "DocumentVersions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ChangeSummary",
                table: "DocumentVersions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "DocumentVersions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "DocumentVersions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoragePath",
                table: "DocumentVersions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "DocumentVersions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Documents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentCategories_DocumentCategories_ParentId",
                table: "DocumentCategories",
                column: "ParentId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersions_Documents_DocumentId",
                table: "DocumentVersions",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentCategories_DocumentCategories_ParentId",
                table: "DocumentCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_Documents_DocumentId",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "StoragePath",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "DocumentVersions");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "DocumentCategories",
                newName: "ParentCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentCategories_ParentId",
                table: "DocumentCategories",
                newName: "IX_DocumentCategories_ParentCategoryId");

            migrationBuilder.AlterColumn<string>(
                name: "FileHash",
                table: "DocumentVersions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "DocumentId",
                table: "DocumentVersions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ChangeSummary",
                table: "DocumentVersions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Documents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_UserId",
                table: "DocumentVersions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentCategories_DocumentCategories_ParentCategoryId",
                table: "DocumentCategories",
                column: "ParentCategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersions_AspNetUsers_UserId",
                table: "DocumentVersions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersions_Documents_DocumentId",
                table: "DocumentVersions",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id");
        }
    }
}
