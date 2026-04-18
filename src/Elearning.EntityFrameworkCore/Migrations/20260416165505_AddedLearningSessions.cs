using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elearning.Migrations
{
    /// <inheritdoc />
    public partial class AddedLearningSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLearningSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsPremiumContent = table.Column<bool>(type: "bit", nullable: false),
                    ShowExplanation = table.Column<bool>(type: "bit", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    AnsweredCount = table.Column<int>(type: "int", nullable: false),
                    TotalQuestionCount = table.Column<int>(type: "int", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_AppLearningSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLearningSessionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QuestionTypeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_AppLearningSessionQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLearningSessionQuestions_AppLearningSessions_LearningSessionId",
                        column: x => x.LearningSessionId,
                        principalTable: "AppLearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppLearningSessionAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningSessionQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedOptionIdsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsAnswered = table.Column<bool>(type: "bit", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AppLearningSessionAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLearningSessionAnswers_AppLearningSessionQuestions_LearningSessionQuestionId",
                        column: x => x.LearningSessionQuestionId,
                        principalTable: "AppLearningSessionQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppLearningSessionAnswers_AppLearningSessions_LearningSessionId",
                        column: x => x.LearningSessionId,
                        principalTable: "AppLearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppLearningSessionQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningSessionQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalQuestionOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AppLearningSessionQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLearningSessionQuestionOptions_AppLearningSessionQuestions_LearningSessionQuestionId",
                        column: x => x.LearningSessionQuestionId,
                        principalTable: "AppLearningSessionQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessionAnswers_LearningSessionId",
                table: "AppLearningSessionAnswers",
                column: "LearningSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessionAnswers_LearningSessionQuestionId",
                table: "AppLearningSessionAnswers",
                column: "LearningSessionQuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessionQuestionOptions_LearningSessionQuestionId",
                table: "AppLearningSessionQuestionOptions",
                column: "LearningSessionQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessionQuestionOptions_LearningSessionQuestionId_SortOrder",
                table: "AppLearningSessionQuestionOptions",
                columns: new[] { "LearningSessionQuestionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessionQuestions_LearningSessionId",
                table: "AppLearningSessionQuestions",
                column: "LearningSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessionQuestions_LearningSessionId_SortOrder",
                table: "AppLearningSessionQuestions",
                columns: new[] { "LearningSessionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessions_StartedAt",
                table: "AppLearningSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessions_UserId",
                table: "AppLearningSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLearningSessions_UserId_SourceKind_SourceId_Status",
                table: "AppLearningSessions",
                columns: new[] { "UserId", "SourceKind", "SourceId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLearningSessionAnswers");

            migrationBuilder.DropTable(
                name: "AppLearningSessionQuestionOptions");

            migrationBuilder.DropTable(
                name: "AppLearningSessionQuestions");

            migrationBuilder.DropTable(
                name: "AppLearningSessions");
        }
    }
}
