using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StoryCoffee.Infrastructure.Data;

#nullable disable

namespace StoryCoffee.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260611090000_AddCustomerAccountNumbers")]
    public partial class AddCustomerAccountNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT "Id", (300 + row_number() OVER (ORDER BY "CreatedAt", "BusinessName", "Id"))::text AS "AccountNumber"
                    FROM customers
                    WHERE "AccountNumber" IS NULL OR "AccountNumber" = ''
                )
                UPDATE customers
                SET "AccountNumber" = numbered."AccountNumber"
                FROM numbered
                WHERE customers."Id" = numbered."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "AccountNumber",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_AccountNumber",
                table: "customers",
                column: "AccountNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customers_AccountNumber",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "customers");
        }
    }
}
