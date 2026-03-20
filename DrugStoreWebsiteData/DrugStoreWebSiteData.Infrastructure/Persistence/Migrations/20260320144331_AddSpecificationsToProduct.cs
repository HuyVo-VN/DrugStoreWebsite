using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugStoreWebSiteData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specifications",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specifications",
                table: "Products");
        }
    }
}
