using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClosingActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "target_close_date",
                schema: "finance",
                table: "fiscal_periods",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "closing_activities",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_action_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_action_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_closing_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_closing_activities_fiscal_periods_fiscal_period_id",
                        column: x => x.fiscal_period_id,
                        principalSchema: "finance",
                        principalTable: "fiscal_periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "closing_activity_steps",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    linked_document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    linked_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    completed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closing_activity_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_closing_activity_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_closing_activity_steps_closing_activities_closing_activity_~",
                        column: x => x.closing_activity_id,
                        principalSchema: "finance",
                        principalTable: "closing_activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_closing_activities_fiscal_period_id",
                schema: "finance",
                table: "closing_activities",
                column: "fiscal_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_closing_activity_steps_closing_activity_id",
                schema: "finance",
                table: "closing_activity_steps",
                column: "closing_activity_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "closing_activity_steps",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "closing_activities",
                schema: "finance");

            migrationBuilder.DropColumn(
                name: "target_close_date",
                schema: "finance",
                table: "fiscal_periods");
        }
    }
}
