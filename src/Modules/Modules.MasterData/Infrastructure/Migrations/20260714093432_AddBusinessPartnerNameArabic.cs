using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.MasterData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessPartnerNameArabic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name_arabic",
                schema: "masterdata",
                table: "business_partners",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name_arabic",
                schema: "masterdata",
                table: "business_partners");
        }
    }
}
