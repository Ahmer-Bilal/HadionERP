using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSourceDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_document_id",
                schema: "finance",
                table: "ar_invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_document_type",
                schema: "finance",
                table: "ar_invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_document_id",
                schema: "finance",
                table: "ap_invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_document_type",
                schema: "finance",
                table: "ap_invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source_document_id",
                schema: "finance",
                table: "ar_invoices");

            migrationBuilder.DropColumn(
                name: "source_document_type",
                schema: "finance",
                table: "ar_invoices");

            migrationBuilder.DropColumn(
                name: "source_document_id",
                schema: "finance",
                table: "ap_invoices");

            migrationBuilder.DropColumn(
                name: "source_document_type",
                schema: "finance",
                table: "ap_invoices");
        }
    }
}
