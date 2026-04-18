using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elearning.Migrations
{
    /// <inheritdoc />
    public partial class AddedLearningSessionMatchingRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchingAnswerJson",
                table: "AppLearningSessionAnswers",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppLearningSessionQuestionMatchingPairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningSessionQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalQuestionMatchingPairId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeftText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RightText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AppLearningSessionQuestionMatchingPairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLearningSessionQuestionMatchingPairs_AppLearningSessionQuestions_LearningSessionQuestionId",
                        column: x => x.LearningSessionQuestionId,
                        principalTable: "AppLearningSessionQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessionQuestionMatchingPairs_LearningSessionQuestionId",
                table: "AppLearningSessionQuestionMatchingPairs",
                column: "LearningSessionQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessionQuestionMatchingPairs_LearningSessionQuestionId_SortOrder",
                table: "AppLearningSessionQuestionMatchingPairs",
                columns: new[] { "LearningSessionQuestionId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLearningSessionQuestionMatchingPairs");

            migrationBuilder.DropColumn(
                name: "MatchingAnswerJson",
                table: "AppLearningSessionAnswers");
        }
    }
}
