using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoryCoffee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionBatchesAndInvoiceItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invoice_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_items_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "production_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductionPeriod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "production_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SkuSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    ProducedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_items_production_batches_ProductionBatchId",
                        column: x => x.ProductionBatchId,
                        principalTable: "production_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_production_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_items_InvoiceId",
                table: "invoice_items",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_production_batches_BatchNumber",
                table: "production_batches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_items_ProductId",
                table: "production_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_production_items_ProductionBatchId_ProductId",
                table: "production_items",
                columns: new[] { "ProductionBatchId", "ProductId" },
                unique: true);

            migrationBuilder.Sql("""
                insert into invoice_items ("Id", "InvoiceId", "Description", "Quantity", "UnitPrice", "LineTotal")
                select oi."Id", i."Id", oi."ProductNameSnapshot", oi."Quantity", oi."UnitPriceSnapshot", oi."LineTotal"
                from invoices i
                join order_items oi on oi."OrderId" = i."OrderId"
                where not exists (
                    select 1 from invoice_items ii where ii."InvoiceId" = i."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_items");

            migrationBuilder.DropTable(
                name: "production_items");

            migrationBuilder.DropTable(
                name: "production_batches");
        }
    }
}
