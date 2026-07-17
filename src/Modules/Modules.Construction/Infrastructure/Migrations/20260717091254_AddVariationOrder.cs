using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Construction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariationOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "variation_orders",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commercial_document_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    commercial_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_variation_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "variation_order_lines",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    commercial_document_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description_arabic = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    wbs_element_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_delta = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    variation_order_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variation_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_variation_order_lines_variation_orders_variation_order_id",
                        column: x => x.variation_order_id,
                        principalSchema: "construction",
                        principalTable: "variation_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_variation_order_lines_variation_order_id",
                schema: "construction",
                table: "variation_order_lines",
                column: "variation_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "variation_order_lines",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "variation_orders",
                schema: "construction");
        }
    }
}
