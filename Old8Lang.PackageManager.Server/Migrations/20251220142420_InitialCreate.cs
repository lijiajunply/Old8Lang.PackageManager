using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Old8Lang.PackageManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Scopes = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PackageId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    License = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ProjectUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DownloadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsListed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrerelease = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DependencyId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    VersionRange = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    TargetFramework = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PackageEntityId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageDependencies_Packages_PackageEntityId",
                        column: x => x.PackageEntityId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    PackageEntityId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageFiles_Packages_PackageEntityId",
                        column: x => x.PackageEntityId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PackageEntityId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageTags_Packages_PackageEntityId",
                        column: x => x.PackageEntityId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ApiKeys",
                columns: new[] { "Id", "CreatedAt", "Description", "ExpiresAt", "IsActive", "Key", "Name", "Scopes", "UsageCount" },
                values: new object[] { 1, new DateTime(2025, 12, 20, 14, 24, 20, 222, DateTimeKind.Utc).AddTicks(9500), "开发环境使用的默认 API 密钥", new DateTime(2026, 12, 20, 14, 24, 20, 222, DateTimeKind.Utc).AddTicks(9500), true, "I8p-vzOkGX6WVkCKh65GH9EXf0A9pgXHxv3s0D5TpVs=", "Development API Key", "package:read,package:write,admin:all", 0 });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ExpiresAt",
                table: "ApiKeys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Key",
                table: "ApiKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageDependencies_PackageEntityId",
                table: "PackageDependencies",
                column: "PackageEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageFiles_PackageEntityId",
                table: "PackageFiles",
                column: "PackageEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_DownloadCount",
                table: "Packages",
                column: "DownloadCount");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_PackageId",
                table: "Packages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_PackageId_Version",
                table: "Packages",
                columns: new[] { "PackageId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_PublishedAt",
                table: "Packages",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PackageTags_PackageEntityId",
                table: "PackageTags",
                column: "PackageEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "PackageDependencies");

            migrationBuilder.DropTable(
                name: "PackageFiles");

            migrationBuilder.DropTable(
                name: "PackageTags");

            migrationBuilder.DropTable(
                name: "Packages");
        }
    }
}
