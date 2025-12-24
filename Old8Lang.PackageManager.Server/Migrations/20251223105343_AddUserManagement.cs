using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Old8Lang.PackageManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Packages_PackageId_Version",
                table: "Packages");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Packages",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "old8lang");

            migrationBuilder.AddColumn<int>(
                name: "UserEntityId",
                table: "Packages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExternalDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DependencyType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PackageName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    VersionSpec = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IndexUrl = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExtraIndexUrl = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsDevDependency = table.Column<bool>(type: "INTEGER", nullable: false),
                    PackageEntityId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalDependencies_Packages_PackageEntityId",
                        column: x => x.PackageEntityId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LanguageMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Language = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    PackageEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    PackageEntityId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LanguageMetadata_Packages_PackageEntityId",
                        column: x => x.PackageEntityId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LanguageMetadata_Packages_PackageEntityId1",
                        column: x => x.PackageEntityId1,
                        principalTable: "Packages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Company = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PackageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDownloads = table.Column<long>(type: "INTEGER", nullable: false),
                    UsedStorage = table.Column<long>(type: "INTEGER", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EmailNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReceiveSecurityAlerts = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubjectId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    JwtId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevokedReason = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserActivityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserExternalLogins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SubjectId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProviderData = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleMappings_UserRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "UserRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleMappings_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleMappings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequestCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpiresAt", "Key" },
                values: new object[] { new DateTime(2025, 12, 23, 10, 53, 43, 715, DateTimeKind.Utc).AddTicks(9460), new DateTime(2026, 12, 23, 10, 53, 43, 715, DateTimeKind.Utc).AddTicks(9560), "z7IpXApoGZo2lqXztuhvNTfTjdvXLoNY7RqKNrA3Mfo=" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Language",
                table: "Packages",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_PackageId_Version_Language",
                table: "Packages",
                columns: new[] { "PackageId", "Version", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_UserEntityId",
                table: "Packages",
                column: "UserEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDependencies_PackageEntityId",
                table: "ExternalDependencies",
                column: "PackageEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageMetadata_PackageEntityId_Language",
                table: "LanguageMetadata",
                columns: new[] { "PackageEntityId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LanguageMetadata_PackageEntityId1",
                table: "LanguageMetadata",
                column: "PackageEntityId1");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_JwtId",
                table: "RefreshTokens",
                column: "JwtId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_ActivityType",
                table: "UserActivityLogs",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_CreatedAt",
                table: "UserActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_UserId",
                table: "UserActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalLogins_Provider_ProviderKey",
                table: "UserExternalLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalLogins_UserId",
                table: "UserExternalLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMappings_AssignedByUserId",
                table: "UserRoleMappings",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMappings_RoleId",
                table: "UserRoleMappings",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMappings_UserId",
                table: "UserRoleMappings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleName",
                table: "UserRoles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Provider",
                table: "Users",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SubjectId",
                table: "Users",
                column: "SubjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ExpiresAt",
                table: "UserSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionId",
                table: "UserSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Packages_Users_UserEntityId",
                table: "Packages",
                column: "UserEntityId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Packages_Users_UserEntityId",
                table: "Packages");

            migrationBuilder.DropTable(
                name: "ExternalDependencies");

            migrationBuilder.DropTable(
                name: "LanguageMetadata");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserActivityLogs");

            migrationBuilder.DropTable(
                name: "UserExternalLogins");

            migrationBuilder.DropTable(
                name: "UserRoleMappings");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Packages_Language",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Packages_PackageId_Version_Language",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_Packages_UserEntityId",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "UserEntityId",
                table: "Packages");

            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpiresAt", "Key" },
                values: new object[] { new DateTime(2025, 12, 20, 14, 24, 20, 222, DateTimeKind.Utc).AddTicks(9500), new DateTime(2026, 12, 20, 14, 24, 20, 222, DateTimeKind.Utc).AddTicks(9500), "I8p-vzOkGX6WVkCKh65GH9EXf0A9pgXHxv3s0D5TpVs=" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_PackageId_Version",
                table: "Packages",
                columns: new[] { "PackageId", "Version" },
                unique: true);
        }
    }
}
