using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugStoreWebSiteData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHomepageToCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Collections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnHomePage",
                table: "Collections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "ShowOnHomePage",
                table: "Collections");
        }
    }
}
