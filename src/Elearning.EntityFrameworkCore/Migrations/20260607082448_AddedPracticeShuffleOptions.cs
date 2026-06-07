using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elearning.Migrations
{
    /// <inheritdoc />
    public partial class AddedPracticeShuffleOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShuffleOptions",
                table: "AppPracticeSets",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShuffleOptions",
                table: "AppPracticeSets");
        }
    }
}
