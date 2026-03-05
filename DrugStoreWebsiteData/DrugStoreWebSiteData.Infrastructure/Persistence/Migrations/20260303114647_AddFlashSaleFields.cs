using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugStoreWebSiteData.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSaleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountEndDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaleSold",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SaleStock",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountEndDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SaleSold",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SaleStock",
                table: "Products");
        }
    }
}
