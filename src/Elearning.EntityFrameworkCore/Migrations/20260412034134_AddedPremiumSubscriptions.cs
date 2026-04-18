using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elearning.Migrations
{
    /// <inheritdoc />
    public partial class AddedPremiumSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppPremiumPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PlanType = table.Column<int>(type: "int", nullable: false),
                    DurationMonths = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AppPremiumPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUserPremiumSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PremiumPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivationNumber = table.Column<int>(type: "int", nullable: false),
                    ActivatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CancelledTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_AppUserPremiumSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserPremiumSubscriptions_AppPremiumPlans_PremiumPlanId",
                        column: x => x.PremiumPlanId,
                        principalTable: "AppPremiumPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppPremiumPlans_Code",
                table: "AppPremiumPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPremiumSubscriptions_ActivatedTime",
                table: "AppUserPremiumSubscriptions",
                column: "ActivatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPremiumSubscriptions_EndTime",
                table: "AppUserPremiumSubscriptions",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPremiumSubscriptions_PremiumPlanId",
                table: "AppUserPremiumSubscriptions",
                column: "PremiumPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPremiumSubscriptions_Status",
                table: "AppUserPremiumSubscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPremiumSubscriptions_UserId",
                table: "AppUserPremiumSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPremiumSubscriptions_UserId_ActivationNumber",
                table: "AppUserPremiumSubscriptions",
                columns: new[] { "UserId", "ActivationNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserPremiumSubscriptions");

            migrationBuilder.DropTable(
                name: "AppPremiumPlans");
        }
    }
}
