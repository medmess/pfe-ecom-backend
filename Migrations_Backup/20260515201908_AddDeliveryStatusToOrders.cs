using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pfe.ecom.api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryStatusToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryStatus",
                table: "Orders",
                column: "DeliveryStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "Orders");
        }
    }
}
