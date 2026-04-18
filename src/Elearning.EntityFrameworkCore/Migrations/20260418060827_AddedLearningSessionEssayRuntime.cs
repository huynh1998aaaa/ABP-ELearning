using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elearning.Migrations
{
    /// <inheritdoc />
    public partial class AddedLearningSessionEssayRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EssayAnswerText",
                table: "AppLearningSessionAnswers",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppLearningSessionQuestionEssayAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningSessionQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SampleAnswer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Rubric = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MaxWords = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLearningSessionQuestionEssayAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLearningSessionQuestionEssayAnswers_AppLearningSessionQuestions_LearningSessionQuestionId",
                        column: x => x.LearningSessionQuestionId,
                        principalTable: "AppLearningSessionQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessionQuestionEssayAnswers_LearningSessionQuestionId",
                table: "AppLearningSessionQuestionEssayAnswers",
                column: "LearningSessionQuestionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLearningSessionQuestionEssayAnswers");

            migrationBuilder.DropColumn(
                name: "EssayAnswerText",
                table: "AppLearningSessionAnswers");
        }
    }
}
