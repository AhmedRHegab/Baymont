using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baymont.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "demo_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_demo_records_EntityType",
                table: "demo_records",
                column: "EntityType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demo_records");
        }
    }
}
