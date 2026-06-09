using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DashboardCharts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dashboard");

            migrationBuilder.CreateTable(
                name: "chart_definitions",
                schema: "dashboard",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    DatasourceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    XField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    YField = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Aggregation = table.Column<string>(type: "text", nullable: false),
                    ColorsJson = table.Column<string>(type: "jsonb", nullable: false),
                    FiltersJson = table.Column<string>(type: "jsonb", nullable: true),
                    LayoutOrder = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chart_definitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chart_definitions_LayoutOrder",
                schema: "dashboard",
                table: "chart_definitions",
                column: "LayoutOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chart_definitions",
                schema: "dashboard");
        }
    }
}
