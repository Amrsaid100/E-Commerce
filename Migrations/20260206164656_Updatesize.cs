using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce.Migrations
{
    /// <inheritdoc />
    public partial class Updatesize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvailableSizes",
                table: "Categories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxSize",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinSize",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SizeType",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableSizes",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "MaxSize",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "MinSize",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SizeType",
                table: "Categories");
        }
    }
}
