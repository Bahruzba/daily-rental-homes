using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyRentalHomes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationRetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "delivery_attempt_count",
                table: "outbound_messages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_attempt_at",
                table: "outbound_messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at",
                table: "outbound_messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_next_attempt_at",
                table: "outbound_messages",
                column: "next_attempt_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbound_messages_next_attempt_at",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "delivery_attempt_count",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "last_attempt_at",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "outbound_messages");
        }
    }
}
