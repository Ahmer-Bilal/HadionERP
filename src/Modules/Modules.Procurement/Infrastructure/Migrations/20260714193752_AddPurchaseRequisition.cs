using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchase_requisitions",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    required_by_date = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_purchase_requisitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_lines",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    estimated_unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    line_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    purchase_requisition_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_requisition_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_purchase_requisitions_purchase_r~",
                        column: x => x.purchase_requisition_id,
                        principalSchema: "procurement",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_lines_purchase_requisition_id",
                schema: "procurement",
                table: "purchase_requisition_lines",
                column: "purchase_requisition_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_requisition_lines",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "purchase_requisitions",
                schema: "procurement");
        }
    }
}
