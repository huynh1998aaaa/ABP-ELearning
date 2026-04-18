using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elearning.Migrations
{
    /// <inheritdoc />
    public partial class AddedPracticeSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppPracticeSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    SelectionMode = table.Column<int>(type: "int", nullable: false),
                    TotalQuestionCount = table.Column<int>(type: "int", nullable: false),
                    ShuffleQuestions = table.Column<bool>(type: "bit", nullable: false),
                    ShowExplanation = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PublishedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AppPracticeSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPracticeQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PracticeSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AppPracticeQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPracticeQuestions_AppPracticeSets_PracticeSetId",
                        column: x => x.PracticeSetId,
                        principalTable: "AppPracticeSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppPracticeQuestions_AppQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "AppQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeQuestions_PracticeSetId_QuestionId",
                table: "AppPracticeQuestions",
                columns: new[] { "PracticeSetId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeQuestions_QuestionId",
                table: "AppPracticeQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeQuestions_SortOrder",
                table: "AppPracticeQuestions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeSets_AccessLevel",
                table: "AppPracticeSets",
                column: "AccessLevel");

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeSets_Code",
                table: "AppPracticeSets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeSets_IsActive",
                table: "AppPracticeSets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeSets_SortOrder",
                table: "AppPracticeSets",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_AppPracticeSets_Status",
                table: "AppPracticeSets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPracticeQuestions");

            migrationBuilder.DropTable(
                name: "AppPracticeSets");
        }
    }
}
