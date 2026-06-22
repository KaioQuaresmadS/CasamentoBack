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
            migrationBuilder.Sql(
                """
                ALTER TABLE "Gifts"
                ADD COLUMN IF NOT EXISTS "IsPurchased" boolean NOT NULL DEFAULT false;
                """);
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
