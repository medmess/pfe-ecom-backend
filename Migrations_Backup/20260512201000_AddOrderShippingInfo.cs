using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pfe.ecom.api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippingInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderShippingInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Wilaya = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddressChoice = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DeliveryService = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DeliveryMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AgencySite = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderShippingInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderShippingInfos_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderShippingInfos_OrderId",
                table: "OrderShippingInfos",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderShippingInfos");
        }
    }
}
