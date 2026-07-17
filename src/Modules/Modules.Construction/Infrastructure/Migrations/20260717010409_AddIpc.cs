using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Construction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIpc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ipcs",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commercial_document_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    commercial_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    measurement_sheet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    retention_percentage_applied = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    advance_payment_percentage_applied = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    other_deductions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_ipcs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ipc_lines",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    commercial_document_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity_this_period = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    quantity_to_date = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ipc_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ipc_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ipc_lines_ipcs_ipc_id",
                        column: x => x.ipc_id,
                        principalSchema: "construction",
                        principalTable: "ipcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ipc_lines_ipc_id",
                schema: "construction",
                table: "ipc_lines",
                column: "ipc_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ipc_lines",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "ipcs",
                schema: "construction");
        }
    }
}
