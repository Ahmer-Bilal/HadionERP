using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tax_codes",
                schema: "masterdata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_code_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tax_code_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tax_code_name_arabic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_tax_codes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tax_codes_tax_code_code",
                schema: "masterdata",
                table: "tax_codes",
                column: "tax_code_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tax_codes",
                schema: "masterdata");
        }
    }
}
