using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clients.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clients");

            migrationBuilder.CreateTable(
                name: "clients",
                schema: "clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    AccountBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActivityLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clients_CreatedAtUtc",
                schema: "clients",
                table: "clients",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_clients_Name",
                schema: "clients",
                table: "clients",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_clients_Status",
                schema: "clients",
                table: "clients",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clients",
                schema: "clients");
        }
    }
}
