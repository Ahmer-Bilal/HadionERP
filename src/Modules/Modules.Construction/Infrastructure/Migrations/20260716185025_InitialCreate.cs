using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Construction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "construction");

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payment_terms = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    advance_payment_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
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
                    table.PrimaryKey("PK_contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "number_range_counters",
                schema: "construction",
                columns: table => new
                {
                    range_key = table.Column<string>(type: "text", nullable: false),
                    company_id = table.Column<string>(type: "text", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_number_range_counters", x => new { x.range_key, x.company_id, x.fiscal_year });
                });

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                schema: "construction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    business_object_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_object_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    applicable_steps = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_step_index = table.Column<int>(type: "integer", nullable: false),
                    current_step_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by_step = table.Column<string>(type: "jsonb", nullable: false),
                    history = table.Column<string>(type: "jsonb", nullable: false),
                    required_approvers_by_step = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "boq_lines",
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
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boq_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_boq_lines_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "construction",
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_boq_lines_contract_id",
                schema: "construction",
                table: "boq_lines",
                column: "contract_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "boq_lines",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "number_range_counters",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "workflow_instances",
                schema: "construction");

            migrationBuilder.DropTable(
                name: "contracts",
                schema: "construction");
        }
    }
}
