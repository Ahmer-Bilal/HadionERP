using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Construction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIpcBillingAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "linked_ar_invoice_id",
                schema: "construction",
                table: "ipcs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "receivable_account_id",
                schema: "construction",
                table: "ipcs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revenue_account_id",
                schema: "construction",
                table: "ipcs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tax_code_id",
                schema: "construction",
                table: "ipcs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "vat_account_id",
                schema: "construction",
                table: "ipcs",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "linked_ar_invoice_id",
                schema: "construction",
                table: "ipcs");

            migrationBuilder.DropColumn(
                name: "receivable_account_id",
                schema: "construction",
                table: "ipcs");

            migrationBuilder.DropColumn(
                name: "revenue_account_id",
                schema: "construction",
                table: "ipcs");

            migrationBuilder.DropColumn(
                name: "tax_code_id",
                schema: "construction",
                table: "ipcs");

            migrationBuilder.DropColumn(
                name: "vat_account_id",
                schema: "construction",
                table: "ipcs");
        }
    }
}
