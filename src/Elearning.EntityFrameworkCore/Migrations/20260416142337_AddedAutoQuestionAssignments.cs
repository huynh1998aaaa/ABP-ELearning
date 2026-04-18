using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elearning.Migrations
{
    /// <inheritdoc />
    public partial class AddedAutoQuestionAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignmentSource",
                table: "AppPracticeQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssignmentSource",
                table: "AppExamQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppExamAutoQuestionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: true),
                    TargetCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AppExamAutoQuestionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExamAutoQuestionRules_AppExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "AppExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppExamAutoQuestionRules_AppQuestionTypes_QuestionTypeId",
                        column: x => x.QuestionTypeId,
                        principalTable: "AppQuestionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppPracticeAutoQuestionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PracticeSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: true),
                    TargetCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AppPracticeAutoQuestionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPracticeAutoQuestionRules_AppPracticeSets_PracticeSetId",
                        column: x => x.PracticeSetId,
                        principalTable: "AppPracticeSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppPracticeAutoQuestionRules_AppQuestionTypes_QuestionTypeId",
                        column: x => x.QuestionTypeId,
                        principalTable: "AppQuestionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppExamAutoQuestionRules_ExamId",
                table: "AppExamAutoQuestionRules",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExamAutoQuestionRules_ExamId_SortOrder",
                table: "AppExamAutoQuestionRules",
                columns: new[] { "ExamId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AppExamAutoQuestionRules_QuestionTypeId",
                table: "AppExamAutoQuestionRules",
                column: "QuestionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeAutoQuestionRules_PracticeSetId",
                table: "AppPracticeAutoQuestionRules",
                column: "PracticeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeAutoQuestionRules_PracticeSetId_SortOrder",
                table: "AppPracticeAutoQuestionRules",
                columns: new[] { "PracticeSetId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeAutoQuestionRules_QuestionTypeId",
                table: "AppPracticeAutoQuestionRules",
                column: "QuestionTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppExamAutoQuestionRules");

            migrationBuilder.DropTable(
                name: "AppPracticeAutoQuestionRules");

            migrationBuilder.DropColumn(
                name: "AssignmentSource",
                table: "AppPracticeQuestions");

            migrationBuilder.DropColumn(
                name: "AssignmentSource",
                table: "AppExamQuestions");
        }
    }
}
