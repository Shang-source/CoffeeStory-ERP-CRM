using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoryCoffee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastProviderEventAt",
                table: "email_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastProviderEventType",
                table: "email_logs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "email_logs",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "email_logs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_delivery_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_delivery_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_delivery_events_email_logs_EmailLogId",
                        column: x => x.EmailLogId,
                        principalTable: "email_logs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_delivery_events_EmailLogId",
                table: "email_delivery_events",
                column: "EmailLogId");

            migrationBuilder.CreateIndex(
                name: "IX_email_delivery_events_Provider_ProviderEventId",
                table: "email_delivery_events",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_delivery_events_Provider_ProviderMessageId",
                table: "email_delivery_events",
                columns: new[] { "Provider", "ProviderMessageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_delivery_events");

            migrationBuilder.DropColumn(
                name: "LastProviderEventAt",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "LastProviderEventType",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "email_logs");
        }
    }
}
