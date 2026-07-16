using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Construction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subcontracts",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subcontractor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retention_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    mobilization_advance_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    defects_liability_period_months = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_subcontracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "back_charges",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    date_incurred = table.Column<DateOnly>(type: "date", nullable: false),
                    subcontract_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_back_charges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_back_charges_subcontracts_subcontract_id",
                        column: x => x.subcontract_id,
                        principalSchema: "construction",
                        principalTable: "subcontracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subcontract_lines",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description_arabic = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    wbs_element_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subcontract_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subcontract_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subcontract_lines_subcontracts_subcontract_id",
                        column: x => x.subcontract_id,
                        principalSchema: "construction",
                        principalTable: "subcontracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_back_charges_subcontract_id",
                schema: "construction",
                table: "back_charges",
                column: "subcontract_id");

            migrationBuilder.CreateIndex(
                name: "IX_subcontract_lines_subcontract_id",
                schema: "construction",
                table: "subcontract_lines",
                column: "subcontract_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "back_charges",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "subcontract_lines",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "subcontracts",
                schema: "construction");
        }
    }
}
