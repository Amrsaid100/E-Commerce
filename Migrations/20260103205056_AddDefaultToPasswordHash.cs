using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultToPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
       name: "PasswordHash",
       table: "Users",
       type: "nvarchar(max)",
       nullable: false,
       defaultValue: "",
       oldClrType: typeof(string),
       oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
        name: "PasswordHash",
        table: "Users",
        type: "nvarchar(max)",
        nullable: false,
        oldClrType: typeof(string),
        oldType: "nvarchar(max)",
        oldDefaultValue: "");
        }
    }
}
