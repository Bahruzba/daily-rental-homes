using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyRentalHomes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingCancellationRequestDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "decided_at",
                table: "booking_cancellation_requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "decided_by_user_id",
                table: "booking_cancellation_requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "decision_note",
                table: "booking_cancellation_requests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "decided_at",
                table: "booking_cancellation_requests");

            migrationBuilder.DropColumn(
                name: "decided_by_user_id",
                table: "booking_cancellation_requests");

            migrationBuilder.DropColumn(
                name: "decision_note",
                table: "booking_cancellation_requests");
        }
    }
}
