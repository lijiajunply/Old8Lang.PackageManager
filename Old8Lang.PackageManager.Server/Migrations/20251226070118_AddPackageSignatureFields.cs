using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Old8Lang.PackageManager.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageSignatureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSigned",
                table: "Packages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedAt",
                table: "Packages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedBy",
                table: "Packages",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpiresAt", "Key" },
                values: new object[] { new DateTime(2025, 12, 26, 7, 1, 18, 549, DateTimeKind.Utc).AddTicks(4810), new DateTime(2026, 12, 26, 7, 1, 18, 549, DateTimeKind.Utc).AddTicks(4940), "Yoy1VBMKArGjgKYeoJ4pZn5q1gbIDwPP-S4cPEyfy1s=" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSigned",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "SignedBy",
                table: "Packages");

            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpiresAt", "Key" },
                values: new object[] { new DateTime(2025, 12, 23, 10, 53, 43, 715, DateTimeKind.Utc).AddTicks(9460), new DateTime(2026, 12, 23, 10, 53, 43, 715, DateTimeKind.Utc).AddTicks(9560), "z7IpXApoGZo2lqXztuhvNTfTjdvXLoNY7RqKNrA3Mfo=" });
        }
    }
}
