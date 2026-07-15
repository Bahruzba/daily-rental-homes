using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyRentalHomes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositDeadlineExtensionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deadline_extended_at",
                table: "booking_deposits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "deadline_extended_by_user_id",
                table: "booking_deposits",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deadline_extension_reason",
                table: "booking_deposits",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deadline_extended_at",
                table: "booking_deposits");

            migrationBuilder.DropColumn(
                name: "deadline_extended_by_user_id",
                table: "booking_deposits");

            migrationBuilder.DropColumn(
                name: "deadline_extension_reason",
                table: "booking_deposits");
        }
    }
}
