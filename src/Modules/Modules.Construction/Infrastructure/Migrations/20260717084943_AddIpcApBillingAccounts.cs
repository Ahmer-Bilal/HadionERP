using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Construction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIpcApBillingAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "expense_account_id",
                schema: "construction",
                table: "ipcs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_ap_invoice_id",
                schema: "construction",
                table: "ipcs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payable_account_id",
                schema: "construction",
                table: "ipcs",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expense_account_id",
                schema: "construction",
                table: "ipcs");

            migrationBuilder.DropColumn(
                name: "linked_ap_invoice_id",
                schema: "construction",
                table: "ipcs");

            migrationBuilder.DropColumn(
                name: "payable_account_id",
                schema: "construction",
                table: "ipcs");
        }
    }
}
