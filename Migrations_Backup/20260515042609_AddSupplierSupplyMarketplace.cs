using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pfe.ecom.api.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierSupplyMarketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AvailableQuantity = table.Column<int>(type: "int", nullable: false),
                    MinimumQuantity = table.Column<int>(type: "int", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierOffers_AspNetUsers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplyRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierOfferId = table.Column<int>(type: "int", nullable: false),
                    DealerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_AspNetUsers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_SupplierOffers_SupplierOfferId",
                        column: x => x.SupplierOfferId,
                        principalTable: "SupplierOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffers_ProviderId",
                table: "SupplierOffers",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_DealerId",
                table: "SupplyRequests",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_SupplierOfferId",
                table: "SupplyRequests",
                column: "SupplierOfferId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplyRequests");

            migrationBuilder.DropTable(
                name: "SupplierOffers");
        }
    }
}
