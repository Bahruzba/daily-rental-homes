using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyRentalHomes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationOutboxFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payload_json",
                table: "outbound_messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_name",
                table: "outbound_messages",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "recipient_user_id",
                table: "outbound_messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_at",
                table: "outbound_messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "outbound_messages",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "type_code",
                table: "outbound_messages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_recipient_user_id",
                table: "outbound_messages",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_scheduled_at",
                table: "outbound_messages",
                column: "scheduled_at");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_type_code",
                table: "outbound_messages",
                column: "type_code");

            migrationBuilder.AddForeignKey(
                name: "FK_outbound_messages_users_recipient_user_id",
                table: "outbound_messages",
                column: "recipient_user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_outbound_messages_users_recipient_user_id",
                table: "outbound_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbound_messages_recipient_user_id",
                table: "outbound_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbound_messages_scheduled_at",
                table: "outbound_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbound_messages_type_code",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "payload_json",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "recipient_name",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "recipient_user_id",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "title",
                table: "outbound_messages");

            migrationBuilder.DropColumn(
                name: "type_code",
                table: "outbound_messages");
        }
    }
}
