using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasamentoAnaKaio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyPaymentCompatibilityColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Payments"
                ADD COLUMN IF NOT EXISTS "MercadoPagoStatus" character varying(40) NOT NULL DEFAULT '';
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Payments"
                ADD COLUMN IF NOT EXISTS "PaidAt" timestamp with time zone NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Payments"
                DROP COLUMN IF EXISTS "PaidAt";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Payments"
                DROP COLUMN IF EXISTS "MercadoPagoStatus";
                """);
        }
    }
}
