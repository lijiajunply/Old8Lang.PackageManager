using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Old8Lang.PackageManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageQualityScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageQualityScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PackageEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    QualityScore = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    CompletenessScore = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    StabilityScore = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    MaintenanceScore = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    SecurityScore = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    CommunityScore = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    DocumentationScore = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageQualityScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageQualityScores_Packages_PackageEntityId",
                        column: x => x.PackageEntityId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpiresAt", "Key" },
                values: new object[] { new DateTime(2025, 12, 26, 7, 51, 3, 16, DateTimeKind.Utc).AddTicks(580), new DateTime(2026, 12, 26, 7, 51, 3, 16, DateTimeKind.Utc).AddTicks(670), "qcPkH8yIChiIiXv5ThV2vwAZHbQQOeFKcmznf0dXLh4=" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageQualityScores_LastCalculatedAt",
                table: "PackageQualityScores",
                column: "LastCalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PackageQualityScores_PackageEntityId",
                table: "PackageQualityScores",
                column: "PackageEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageQualityScores_QualityScore",
                table: "PackageQualityScores",
                column: "QualityScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageQualityScores");

            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpiresAt", "Key" },
                values: new object[] { new DateTime(2025, 12, 26, 7, 1, 18, 549, DateTimeKind.Utc).AddTicks(4810), new DateTime(2026, 12, 26, 7, 1, 18, 549, DateTimeKind.Utc).AddTicks(4940), "Yoy1VBMKArGjgKYeoJ4pZn5q1gbIDwPP-S4cPEyfy1s=" });
        }
    }
}
