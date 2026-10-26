using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baymont.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ContractorName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Assignment = table.Column<string>(type: "TEXT", nullable: true),
                    WorkStartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "contractor_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EntryReason = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    EntryPermit = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ExitDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExitReason = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ExitPermit = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ContractorId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractor_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contractor_entries_contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contractor_equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    MovementPermit = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ContractorId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractor_equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contractor_equipment_contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contractor_materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    StorageLocation = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ContractorId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractor_materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contractor_materials_contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contractor_vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlateNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PlateNumberNormalized = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsSiteServiceCar = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ContractorId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractor_vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contractor_vehicles_contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contractor_workers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Housing = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IdPhotoPath = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CriminalRecordPhotoPath = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ContractorId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractor_workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contractor_workers_contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "contractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contractor_entries_ContractorId",
                table: "contractor_entries",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_equipment_ContractorId",
                table: "contractor_equipment",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_materials_ContractorId",
                table: "contractor_materials",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_vehicles_ContractorId",
                table: "contractor_vehicles",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_vehicles_PlateNumberNormalized",
                table: "contractor_vehicles",
                column: "PlateNumberNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_workers_ContractorId",
                table: "contractor_workers",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_contractors_CompanyName",
                table: "contractors",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_contractors_ContractorName",
                table: "contractors",
                column: "ContractorName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractor_entries");

            migrationBuilder.DropTable(
                name: "contractor_equipment");

            migrationBuilder.DropTable(
                name: "contractor_materials");

            migrationBuilder.DropTable(
                name: "contractor_vehicles");

            migrationBuilder.DropTable(
                name: "contractor_workers");

            migrationBuilder.DropTable(
                name: "contractors");
        }
    }
}
