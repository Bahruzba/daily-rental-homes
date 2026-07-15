using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyRentalHomes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaWhatsAppDeliveryStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "delivered_at",
                table: "outbound_messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_delivery_status",
                table: "outbound_messages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "provider_status_updated_at",
                table: "outbound_messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "read_at",
                table: "outbound_messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbound_messages_provider_message_id",
                table: "outbound_messages",
                column: "provider_message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbound_messages_provider_message_id",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "delivered_at",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "provider_delivery_status",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "provider_status_updated_at",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "read_at",
                table: "outbound_messages");
        }
    }
}
