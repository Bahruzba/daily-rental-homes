using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyRentalHomes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalHomeAvailabilityBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rental_home_availability_blocks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rental_home_id = table.Column<long>(type: "bigint", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rental_home_availability_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_rental_home_availability_blocks_rental_homes_rental_home_id",
                        column: x => x.rental_home_id,
                        principalTable: "rental_homes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rental_home_availability_blocks_rental_home_id",
                table: "rental_home_availability_blocks",
                column: "rental_home_id");

            migrationBuilder.CreateIndex(
                name: "IX_rental_home_availability_blocks_rental_home_id_start_date_end_date",
                table: "rental_home_availability_blocks",
                columns: new[] { "rental_home_id", "start_date", "end_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rental_home_availability_blocks");
        }
    }
}
