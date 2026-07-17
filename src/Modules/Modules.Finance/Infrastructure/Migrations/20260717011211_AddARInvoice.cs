using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddARInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ar_invoices",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    revenue_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receivable_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    vat_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    linked_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_ar_invoices", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ar_invoices",
                schema: "finance");
        }
    }
}
