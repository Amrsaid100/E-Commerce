using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce.Migrations
{
    /// <inheritdoc />
    public partial class AddFooterSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "ShopSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "ShopSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterTagline",
                table: "ShopSettings",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "ShopSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneDisplay",
                table: "ShopSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TikTokUrl",
                table: "ShopSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsApp",
                table: "ShopSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ShopSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ContactEmail", "FacebookUrl", "FooterTagline", "InstagramUrl", "PhoneDisplay", "TikTokUrl", "UpdatedAt", "WhatsApp" },
                values: new object[] { null, null, "Your premium shopping destination", null, "+20 101 1944466", null, new DateTime(2026, 3, 1, 16, 2, 10, 17, DateTimeKind.Utc).AddTicks(9151), "201011944466" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "FooterTagline",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "PhoneDisplay",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "TikTokUrl",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "WhatsApp",
                table: "ShopSettings");

            migrationBuilder.UpdateData(
                table: "ShopSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 1, 1, 1, 52, 658, DateTimeKind.Utc).AddTicks(6356));
        }
    }
}
