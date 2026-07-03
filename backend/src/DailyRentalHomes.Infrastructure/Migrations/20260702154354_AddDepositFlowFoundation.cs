using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyRentalHomes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositFlowFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "payment_cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_reupload",
                table: "booking_deposits",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "review_note",
                table: "booking_deposits",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "booking_deposits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "reviewed_by_user_id",
                table: "booking_deposits",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "uploaded_at",
                table: "booking_deposits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposits_reviewed_by_user_id",
                table: "booking_deposits",
                column: "reviewed_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_booking_deposits_users_reviewed_by_user_id",
                table: "booking_deposits",
                column: "reviewed_by_user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_booking_deposits_users_reviewed_by_user_id",
                table: "booking_deposits");

            migrationBuilder.DropIndex(
                name: "IX_booking_deposits_reviewed_by_user_id",
                table: "booking_deposits");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "payment_cards");

            migrationBuilder.DropColumn(
                name: "allow_reupload",
                table: "booking_deposits");

            migrationBuilder.DropColumn(
                name: "review_note",
                table: "booking_deposits");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "booking_deposits");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                table: "booking_deposits");

            migrationBuilder.DropColumn(
                name: "uploaded_at",
                table: "booking_deposits");
        }
    }
}
