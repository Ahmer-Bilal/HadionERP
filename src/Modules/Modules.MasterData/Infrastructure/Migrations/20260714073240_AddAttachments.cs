using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "masterdata",
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
                name: "attachment_contents",
                schema: "masterdata",
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
                        principalSchema: "masterdata",
                        principalTable: "attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_business_object_type_business_object_id",
                schema: "masterdata",
                table: "attachments",
                columns: new[] { "business_object_type", "business_object_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachment_contents",
                schema: "masterdata");

            migrationBuilder.DropTable(
                name: "attachments",
                schema: "masterdata");
        }
    }
}
