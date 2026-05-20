using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pfe.ecom.api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryCompanyPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Wilaya = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryCompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryBranches_AspNetUsers_DeliveryCompanyId",
                        column: x => x.DeliveryCompanyId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscountPercent = table.Column<int>(type: "int", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryCompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryOffers_AspNetUsers_DeliveryCompanyId",
                        column: x => x.DeliveryCompanyId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Wilaya = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AddressPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OfficePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeliveryCompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryPrices_AspNetUsers_DeliveryCompanyId",
                        column: x => x.DeliveryCompanyId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryBranches_DeliveryCompanyId",
                table: "DeliveryBranches",
                column: "DeliveryCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOffers_DeliveryCompanyId",
                table: "DeliveryOffers",
                column: "DeliveryCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPrices_DeliveryCompanyId_Wilaya",
                table: "DeliveryPrices",
                columns: new[] { "DeliveryCompanyId", "Wilaya" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryBranches");

            migrationBuilder.DropTable(
                name: "DeliveryOffers");

            migrationBuilder.DropTable(
                name: "DeliveryPrices");
        }
    }
}
