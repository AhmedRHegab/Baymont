using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baymont.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "staff_members",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "staff_members",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GovernorateCode",
                table: "staff_members",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdPhotoPath",
                table: "staff_members",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "staff_members",
                type: "TEXT",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "contractor_workers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "contractor_workers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GovernorateCode",
                table: "contractor_workers",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "contractor_workers",
                type: "TEXT",
                maxLength: 14,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reservation_guests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReservationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    NationalId = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Gender = table.Column<int>(type: "INTEGER", nullable: true),
                    GovernorateCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    IdPhotoPath = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation_guests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reservation_guests_reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_staff_members_NationalId",
                table: "staff_members",
                column: "NationalId");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_workers_NationalId",
                table: "contractor_workers",
                column: "NationalId");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_guests_NationalId",
                table: "reservation_guests",
                column: "NationalId");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_guests_ReservationId_NationalId",
                table: "reservation_guests",
                columns: new[] { "ReservationId", "NationalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservation_guests");

            migrationBuilder.DropIndex(
                name: "IX_staff_members_NationalId",
                table: "staff_members");

            migrationBuilder.DropIndex(
                name: "IX_contractor_workers_NationalId",
                table: "contractor_workers");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "staff_members");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "staff_members");

            migrationBuilder.DropColumn(
                name: "GovernorateCode",
                table: "staff_members");

            migrationBuilder.DropColumn(
                name: "IdPhotoPath",
                table: "staff_members");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "staff_members");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "contractor_workers");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "contractor_workers");

            migrationBuilder.DropColumn(
                name: "GovernorateCode",
                table: "contractor_workers");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "contractor_workers");
        }
    }
}
