using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasamentoAnaKaio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncPaymentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ExternalReference",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PreferenceId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_GiftContributions_ProviderPaymentId",
                table: "GiftContributions");

            migrationBuilder.DropColumn(
                name: "MercadoPagoStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalReference",
                table: "Payments",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExternalReference",
                table: "Payments",
                column: "ExternalReference",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_GiftContributions_GiftContributionId",
                table: "Payments",
                column: "GiftContributionId",
                principalTable: "GiftContributions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_GiftContributions_GiftContributionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ExternalReference",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalReference",
                table: "Payments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoStatus",
                table: "Payments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExternalReference",
                table: "Payments",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PreferenceId",
                table: "Payments",
                column: "PreferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftContributions_ProviderPaymentId",
                table: "GiftContributions",
                column: "ProviderPaymentId");
        }
    }
}
