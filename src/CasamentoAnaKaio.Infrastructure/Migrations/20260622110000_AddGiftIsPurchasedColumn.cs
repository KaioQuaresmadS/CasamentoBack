using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasamentoAnaKaio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftIsPurchasedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPurchased",
                table: "Gifts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPurchased",
                table: "Gifts");
        }
    }
}
