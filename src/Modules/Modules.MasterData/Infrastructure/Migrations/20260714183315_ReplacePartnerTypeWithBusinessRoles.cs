using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePartnerTypeWithBusinessRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_partner_roles",
                schema: "masterdata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    trade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    business_partner_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_partner_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_business_partner_roles_business_partners_business_partner_id",
                        column: x => x.business_partner_id,
                        principalSchema: "masterdata",
                        principalTable: "business_partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_partner_roles_business_partner_id",
                schema: "masterdata",
                table: "business_partner_roles",
                column: "business_partner_id");

            // Data migration: every existing partner_type value becomes an equivalent BusinessRole row
            // before the column is dropped, rather than silently losing it — Customer/Both -> Client,
            // Vendor/Both -> Supplier (a "Both" partner correctly ends up holding two roles, matching
            // what "Both" always meant).
            migrationBuilder.Sql(@"
                INSERT INTO masterdata.business_partner_roles (""Id"", role_type, business_partner_id)
                SELECT gen_random_uuid(), 'Client', ""Id"" FROM masterdata.business_partners
                WHERE partner_type IN ('Customer', 'Both');

                INSERT INTO masterdata.business_partner_roles (""Id"", role_type, business_partner_id)
                SELECT gen_random_uuid(), 'Supplier', ""Id"" FROM masterdata.business_partners
                WHERE partner_type IN ('Vendor', 'Both');
            ");

            migrationBuilder.DropColumn(
                name: "partner_type",
                schema: "masterdata",
                table: "business_partners");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_partner_roles",
                schema: "masterdata");

            migrationBuilder.AddColumn<string>(
                name: "partner_type",
                schema: "masterdata",
                table: "business_partners",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
