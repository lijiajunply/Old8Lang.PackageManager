using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Old8Lang.PackageManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedDataWithStaticValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpiresAt", "Key" },
                values: new object[] { new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "dev-api-key-12345678901234567890abcdefghijklmnopqrstuvwxyz" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpiresAt", "Key" },
                values: new object[] { new DateTime(2025, 12, 26, 7, 51, 3, 16, DateTimeKind.Utc).AddTicks(580), new DateTime(2026, 12, 26, 7, 51, 3, 16, DateTimeKind.Utc).AddTicks(670), "qcPkH8yIChiIiXv5ThV2vwAZHbQQOeFKcmznf0dXLh4=" });
        }
    }
}
