using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestForQuotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "requests_for_quotation",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_requisition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    response_deadline = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_requests_for_quotation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rfq_invited_vendors",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_for_quotation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_invited_vendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rfq_invited_vendors_requests_for_quotation_request_for_quot~",
                        column: x => x.request_for_quotation_id,
                        principalSchema: "procurement",
                        principalTable: "requests_for_quotation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfq_lines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_requisition_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    request_for_quotation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rfq_lines_requests_for_quotation_request_for_quotation_id",
                        column: x => x.request_for_quotation_id,
                        principalSchema: "procurement",
                        principalTable: "requests_for_quotation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfq_vendor_quote_lines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quoted_unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    request_for_quotation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_vendor_quote_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rfq_vendor_quote_lines_requests_for_quotation_request_for_q~",
                        column: x => x.request_for_quotation_id,
                        principalSchema: "procurement",
                        principalTable: "requests_for_quotation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rfq_invited_vendors_request_for_quotation_id",
                schema: "procurement",
                table: "rfq_invited_vendors",
                column: "request_for_quotation_id");

            migrationBuilder.CreateIndex(
                name: "IX_rfq_lines_request_for_quotation_id",
                schema: "procurement",
                table: "rfq_lines",
                column: "request_for_quotation_id");

            migrationBuilder.CreateIndex(
                name: "IX_rfq_vendor_quote_lines_request_for_quotation_id",
                schema: "procurement",
                table: "rfq_vendor_quote_lines",
                column: "request_for_quotation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfq_invited_vendors",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "rfq_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "rfq_vendor_quote_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "requests_for_quotation",
                schema: "procurement");
        }
    }
}
