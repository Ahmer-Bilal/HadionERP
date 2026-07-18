using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Construction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetentionReleaseAndContractRetentionPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "retention_percentage",
                schema: "construction",
                table: "contracts",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "retention_releases",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commercial_document_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    commercial_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount_released = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    trigger_event = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    revenue_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    receivable_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expense_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payable_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vat_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    linked_ar_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    linked_ap_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_retention_releases", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "retention_releases",
                schema: "construction");

            migrationBuilder.DropColumn(
                name: "retention_percentage",
                schema: "construction",
                table: "contracts");
        }
    }
}
