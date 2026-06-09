using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Todos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "todos");

            migrationBuilder.CreateTable(
                name: "todo_items",
                schema: "todos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todo_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_todo_items_DueDate",
                schema: "todos",
                table: "todo_items",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_todo_items_Status",
                schema: "todos",
                table: "todo_items",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "todo_items",
                schema: "todos");
        }
    }
}
