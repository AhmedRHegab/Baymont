using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baymont.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "overnight_stays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StayDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Shift = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overnight_stays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_overnight_stays_owners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "owners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_overnight_stays_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "shift_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShiftDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    AssignedWork = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Company = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    PlateNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ExitAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_entries_staff_members_StaffMemberId",
                        column: x => x.StaffMemberId,
                        principalTable: "staff_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_overnight_stays_OwnerId",
                table: "overnight_stays",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_overnight_stays_StayDate_Shift",
                table: "overnight_stays",
                columns: new[] { "StayDate", "Shift" });

            migrationBuilder.CreateIndex(
                name: "IX_overnight_stays_UnitId",
                table: "overnight_stays",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_assignments_ShiftDate_Kind",
                table: "shift_assignments",
                columns: new[] { "ShiftDate", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_entries_EntryAt",
                table: "staff_entries",
                column: "EntryAt");

            migrationBuilder.CreateIndex(
                name: "IX_staff_entries_StaffMemberId",
                table: "staff_entries",
                column: "StaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_members_Name",
                table: "staff_members",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "overnight_stays");

            migrationBuilder.DropTable(
                name: "shift_assignments");

            migrationBuilder.DropTable(
                name: "staff_entries");

            migrationBuilder.DropTable(
                name: "staff_members");
        }
    }
}
