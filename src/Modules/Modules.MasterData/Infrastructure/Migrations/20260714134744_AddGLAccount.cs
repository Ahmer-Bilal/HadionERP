using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGLAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gl_accounts",
                schema: "masterdata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_name_arabic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    account_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parent_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_postable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    doc_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    extension_data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gl_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gl_accounts_gl_accounts_parent_account_id",
                        column: x => x.parent_account_id,
                        principalSchema: "masterdata",
                        principalTable: "gl_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gl_accounts_account_code",
                schema: "masterdata",
                table: "gl_accounts",
                column: "account_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gl_accounts_parent_account_id",
                schema: "masterdata",
                table: "gl_accounts",
                column: "parent_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gl_accounts",
                schema: "masterdata");
        }
    }
}
