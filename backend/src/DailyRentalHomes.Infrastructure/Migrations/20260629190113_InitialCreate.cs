using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyRentalHomes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "amenities",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    icon_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amenities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "booking_statuses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "otp_codes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    phone_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    code_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    try_count = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    full_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    phone_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    role = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_cards",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    broker_user_id = table.Column<long>(type: "bigint", nullable: false),
                    card_holder_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    pan_masked = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_cards", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_cards_users_broker_user_id",
                        column: x => x.broker_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rental_homes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    broker_user_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    district = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    daily_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    room_count = table.Column<int>(type: "int", nullable: false),
                    guest_count = table.Column<int>(type: "int", nullable: false),
                    is_published = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rental_homes", x => x.id);
                    table.ForeignKey(
                        name: "FK_rental_homes_users_broker_user_id",
                        column: x => x.broker_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rental_home_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_user_id = table.Column<long>(type: "bigint", nullable: true),
                    customer_full_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    customer_phone_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    guest_count = table.Column<int>(type: "int", nullable: false),
                    daily_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_bookings_booking_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "booking_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bookings_rental_homes_rental_home_id",
                        column: x => x.rental_home_id,
                        principalTable: "rental_homes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bookings_users_customer_user_id",
                        column: x => x.customer_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "related_contacts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rental_home_id = table.Column<long>(type: "bigint", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    contact_type = table.Column<int>(type: "int", nullable: false),
                    notify_enabled = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_related_contacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_related_contacts_rental_homes_rental_home_id",
                        column: x => x.rental_home_id,
                        principalTable: "rental_homes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rental_home_amenities",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rental_home_id = table.Column<long>(type: "bigint", nullable: false),
                    amenity_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rental_home_amenities", x => x.id);
                    table.ForeignKey(
                        name: "FK_rental_home_amenities_amenities_amenity_id",
                        column: x => x.amenity_id,
                        principalTable: "amenities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rental_home_amenities_rental_homes_rental_home_id",
                        column: x => x.rental_home_id,
                        principalTable: "rental_homes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_dates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_dates", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_dates_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_deposits",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    deadline_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    payment_card_id = table.Column<long>(type: "bigint", nullable: true),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_deposits", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_deposits_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_deposits_payment_cards_payment_card_id",
                        column: x => x.payment_card_id,
                        principalTable: "payment_cards",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "booking_status_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    old_status_id = table.Column<long>(type: "bigint", nullable: true),
                    new_status_id = table.Column<long>(type: "bigint", nullable: false),
                    changed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_status_history_booking_statuses_new_status_id",
                        column: x => x.new_status_id,
                        principalTable: "booking_statuses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_booking_status_history_booking_statuses_old_status_id",
                        column: x => x.old_status_id,
                        principalTable: "booking_statuses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_booking_status_history_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_status_history_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "media_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rental_home_id = table.Column<long>(type: "bigint", nullable: true),
                    booking_deposit_id = table.Column<long>(type: "bigint", nullable: true),
                    file_type = table.Column<int>(type: "int", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    file_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_media_files_booking_deposits_booking_deposit_id",
                        column: x => x.booking_deposit_id,
                        principalTable: "booking_deposits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_media_files_rental_homes_rental_home_id",
                        column: x => x.rental_home_id,
                        principalTable: "rental_homes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "outbound_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    channel = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    to = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    provider_message_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    booking_id = table.Column<long>(type: "bigint", nullable: true),
                    booking_deposit_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_outbound_messages_booking_deposits_booking_deposit_id",
                        column: x => x.booking_deposit_id,
                        principalTable: "booking_deposits",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_outbound_messages_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_amenities_name",
                table: "amenities",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_dates_booking_id_date",
                table: "booking_dates",
                columns: new[] { "booking_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposits_booking_id",
                table: "booking_deposits",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposits_payment_card_id",
                table: "booking_deposits",
                column: "payment_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_deposits_status",
                table: "booking_deposits",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status_history_booking_id",
                table: "booking_status_history",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status_history_changed_by_user_id",
                table: "booking_status_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status_history_new_status_id",
                table: "booking_status_history",
                column: "new_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status_history_old_status_id",
                table: "booking_status_history",
                column: "old_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_statuses_code",
                table: "booking_statuses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_customer_user_id",
                table: "bookings",
                column: "customer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_rental_home_id",
                table: "bookings",
                column: "rental_home_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_status_id",
                table: "bookings",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_media_files_booking_deposit_id",
                table: "media_files",
                column: "booking_deposit_id");

            migrationBuilder.CreateIndex(
                name: "IX_media_files_file_type",
                table: "media_files",
                column: "file_type");

            migrationBuilder.CreateIndex(
                name: "IX_media_files_rental_home_id",
                table: "media_files",
                column: "rental_home_id");

            migrationBuilder.CreateIndex(
                name: "IX_otp_codes_expires_at",
                table: "otp_codes",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_otp_codes_phone_number",
                table: "otp_codes",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_booking_deposit_id",
                table: "outbound_messages",
                column: "booking_deposit_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_booking_id",
                table: "outbound_messages",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_channel",
                table: "outbound_messages",
                column: "channel");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_status",
                table: "outbound_messages",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_payment_cards_broker_user_id",
                table: "payment_cards",
                column: "broker_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_related_contacts_contact_type",
                table: "related_contacts",
                column: "contact_type");

            migrationBuilder.CreateIndex(
                name: "IX_related_contacts_rental_home_id",
                table: "related_contacts",
                column: "rental_home_id");

            migrationBuilder.CreateIndex(
                name: "IX_rental_home_amenities_amenity_id",
                table: "rental_home_amenities",
                column: "amenity_id");

            migrationBuilder.CreateIndex(
                name: "IX_rental_home_amenities_rental_home_id_amenity_id",
                table: "rental_home_amenities",
                columns: new[] { "rental_home_id", "amenity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rental_homes_broker_user_id",
                table: "rental_homes",
                column: "broker_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_rental_homes_city",
                table: "rental_homes",
                column: "city");

            migrationBuilder.CreateIndex(
                name: "IX_users_phone_number",
                table: "users",
                column: "phone_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_dates");

            migrationBuilder.DropTable(
                name: "booking_status_history");

            migrationBuilder.DropTable(
                name: "media_files");

            migrationBuilder.DropTable(
                name: "otp_codes");

            migrationBuilder.DropTable(
                name: "outbound_messages");

            migrationBuilder.DropTable(
                name: "related_contacts");

            migrationBuilder.DropTable(
                name: "rental_home_amenities");

            migrationBuilder.DropTable(
                name: "booking_deposits");

            migrationBuilder.DropTable(
                name: "amenities");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "payment_cards");

            migrationBuilder.DropTable(
                name: "booking_statuses");

            migrationBuilder.DropTable(
                name: "rental_homes");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
