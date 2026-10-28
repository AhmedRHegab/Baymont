using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baymont.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    FromAddress = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ReceivedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ProcessedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    PermitsCreated = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitCode = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UnitId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GuestNames = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    PersonsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: true),
                    CheckInDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CheckOutDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PeriodEstimated = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewedBy = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ReviewedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    FinalDepartureAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    SourceFrom = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SourceSubject = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    SourceFileName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SourceMessageId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    SourceSentAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reservations_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reservation_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReservationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnteredAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ExitedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    IsFinal = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reservation_movements_reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_MessageId",
                table: "inbox_messages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_ProcessedAt",
                table: "inbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_movements_ReservationId",
                table: "reservation_movements",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_CheckInDate",
                table: "reservations",
                column: "CheckInDate");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_Status",
                table: "reservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_UnitCode",
                table: "reservations",
                column: "UnitCode");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_UnitId",
                table: "reservations",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "reservation_movements");

            migrationBuilder.DropTable(
                name: "reservations");
        }
    }
}
