using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCostCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cost_centers",
                schema: "masterdata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    cost_center_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cost_center_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cost_center_name_arabic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    parent_cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_postable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_cost_centers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cost_centers_cost_centers_parent_cost_center_id",
                        column: x => x.parent_cost_center_id,
                        principalSchema: "masterdata",
                        principalTable: "cost_centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cost_centers_cost_center_code",
                schema: "masterdata",
                table: "cost_centers",
                column: "cost_center_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cost_centers_parent_cost_center_id",
                schema: "masterdata",
                table: "cost_centers",
                column: "parent_cost_center_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cost_centers",
                schema: "masterdata");
        }
    }
}
