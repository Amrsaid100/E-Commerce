using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace E_Commerce.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernorateSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GovernorateId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCost",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Governorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShippingCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governorates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Governorates",
                columns: new[] { "Id", "NameAr", "NameEn", "ShippingCost" },
                values: new object[,]
                {
                    { 1, "القاهرة", "Cairo", 50.00m },
                    { 2, "الجيزة", "Giza", 50.00m },
                    { 3, "الإسكندرية", "Alexandria", 60.00m },
                    { 4, "الدقهلية", "Dakahlia", 70.00m },
                    { 5, "البحر الأحمر", "Red Sea", 100.00m },
                    { 6, "البحيرة", "Beheira", 70.00m },
                    { 7, "الفيوم", "Fayoum", 60.00m },
                    { 8, "الغربية", "Gharbia", 65.00m },
                    { 9, "الإسماعيلية", "Ismailia", 75.00m },
                    { 10, "المنوفية", "Monufia", 60.00m },
                    { 11, "المنيا", "Minya", 80.00m },
                    { 12, "القليوبية", "Qalyubia", 55.00m },
                    { 13, "الوادي الجديد", "New Valley", 120.00m },
                    { 14, "الشرقية", "Sharqia", 65.00m },
                    { 15, "سوهاج", "Sohag", 90.00m },
                    { 16, "جنوب سيناء", "South Sinai", 110.00m },
                    { 17, "كفر الشيخ", "Kafr El Sheikh", 70.00m },
                    { 18, "مطروح", "Matrouh", 100.00m },
                    { 19, "الأقصر", "Luxor", 95.00m },
                    { 20, "قنا", "Qena", 90.00m },
                    { 21, "أسوان", "Aswan", 100.00m },
                    { 22, "أسيوط", "Asyut", 85.00m },
                    { 23, "بني سويف", "Beni Suef", 70.00m },
                    { 24, "بورسعيد", "Port Said", 75.00m },
                    { 25, "دمياط", "Damietta", 75.00m },
                    { 26, "شمال سيناء", "North Sinai", 110.00m },
                    { 27, "السويس", "Suez", 70.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_GovernorateId",
                table: "Orders",
                column: "GovernorateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Governorates_GovernorateId",
                table: "Orders",
                column: "GovernorateId",
                principalTable: "Governorates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Governorates_GovernorateId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Governorates");

            migrationBuilder.DropIndex(
                name: "IX_Orders_GovernorateId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GovernorateId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingCost",
                table: "Orders");
        }
    }
}
