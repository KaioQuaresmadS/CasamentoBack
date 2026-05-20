using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasamentoAnaKaio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMercadoPagoPaymentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add MercadoPagoPaymentId column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPaymentId",
                table: "Payments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            // Add PreferenceId column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "PreferenceId",
                table: "Payments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            // Add InitPoint column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "InitPoint",
                table: "Payments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            // Add SandboxInitPoint column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "SandboxInitPoint",
                table: "Payments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            // Add ExternalReference column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "ExternalReference",
                table: "Payments",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            // Add PaymentMethod column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Payments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            // Add PayerName column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "PayerName",
                table: "Payments",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            // Add PayerEmail column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "PayerEmail",
                table: "Payments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            // Add Status column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Payments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Pending");

            // Add PixQrCode column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "PixQrCode",
                table: "Payments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            // Add PixCopyPaste column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "PixCopyPaste",
                table: "Payments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            // Add UpdatedAt column if it doesn't exist
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Create indexes for performance
            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExternalReference",
                table: "Payments",
                column: "ExternalReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MercadoPagoPaymentId",
                table: "Payments",
                column: "MercadoPagoPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GiftContributionId",
                table: "Payments",
                column: "GiftContributionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes in reverse order
            migrationBuilder.DropIndex(
                name: "IX_Payments_GiftContributionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_MercadoPagoPaymentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ExternalReference",
                table: "Payments");

            // Drop columns in reverse order
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PixCopyPaste",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PixQrCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayerEmail",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayerName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExternalReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SandboxInitPoint",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InitPoint",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PreferenceId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "MercadoPagoPaymentId",
                table: "Payments");
        }
    }
}
