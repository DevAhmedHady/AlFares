using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "identity",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_permissions",
                schema: "identity",
                columns: table => new
                {
                    tenant_role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_permissions", x => new { x.tenant_role_id, x.permission_id });
                });

            migrationBuilder.CreateTable(
                name: "tenant_roles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    base_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_user_roles",
                schema: "identity",
                columns: table => new
                {
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_user_roles", x => new { x.tenant_user_id, x.tenant_role_id });
                });

            migrationBuilder.CreateTable(
                name: "tenant_users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                schema: "identity",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                schema: "identity",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                schema: "identity",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_roles_tenant_id_name",
                schema: "identity",
                table: "tenant_roles",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_users_tenant_id_user_id",
                schema: "identity",
                table: "tenant_users",
                columns: new[] { "tenant_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_slug",
                schema: "identity",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "tenant_permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "tenant_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "tenant_user_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "tenant_users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");
        }
    }
}
