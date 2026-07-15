using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.ProjectManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "projectmanagement");

            migrationBuilder.CreateTable(
                name: "number_range_counters",
                schema: "projectmanagement",
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
                name: "projects",
                schema: "projectmanagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    project_name_arabic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                schema: "projectmanagement",
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
                name: "wbs_elements",
                schema: "projectmanagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_arabic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    parent_wbs_element_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_planning_element = table.Column<bool>(type: "boolean", nullable: false),
                    is_account_assignment_element = table.Column<bool>(type: "boolean", nullable: false),
                    is_billing_element = table.Column<bool>(type: "boolean", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wbs_elements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wbs_elements_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "projectmanagement",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wbs_elements_project_id",
                schema: "projectmanagement",
                table: "wbs_elements",
                column: "project_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "number_range_counters",
                schema: "projectmanagement");

            migrationBuilder.DropTable(
                name: "wbs_elements",
                schema: "projectmanagement");

            migrationBuilder.DropTable(
                name: "workflow_instances",
                schema: "projectmanagement");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "projectmanagement");
        }
    }
}
