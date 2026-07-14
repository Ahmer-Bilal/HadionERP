using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Procurement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "procurement");

            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_object_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    business_object_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "number_range_counters",
                schema: "procurement",
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
                name: "vendor_prequalifications",
                schema: "procurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_partner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    trade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_vendor_prequalifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                schema: "procurement",
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
                name: "attachment_contents",
                schema: "procurement",
                columns: table => new
                {
                    attachment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachment_contents", x => x.attachment_id);
                    table.ForeignKey(
                        name: "FK_attachment_contents_attachments_attachment_id",
                        column: x => x.attachment_id,
                        principalSchema: "procurement",
                        principalTable: "attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_business_object_type_business_object_id",
                schema: "procurement",
                table: "attachments",
                columns: new[] { "business_object_type", "business_object_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachment_contents",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "number_range_counters",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "vendor_prequalifications",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "workflow_instances",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "attachments",
                schema: "procurement");
        }
    }
}
