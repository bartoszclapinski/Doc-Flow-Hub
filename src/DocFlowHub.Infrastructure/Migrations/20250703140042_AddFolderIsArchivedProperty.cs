using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocFlowHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderIsArchivedProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Folders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Folders");
        }
    }
}
