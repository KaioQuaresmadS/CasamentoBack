using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CasamentoAnaKaio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ReservedPercent = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuestConfirmations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GuestsCount = table.Column<int>(type: "integer", nullable: false),
                    WillAttend = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestConfirmations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GiftContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContributorName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ContributorPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    QuotaQuantity = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PixKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    QrCodePayload = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftContributions_Gifts_GiftId",
                        column: x => x.GiftId,
                        principalTable: "Gifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GiftContributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MercadoPagoPaymentId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PreferenceId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    InitPoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SandboxInitPoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PayerName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PayerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PixQrCode = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PixCopyPaste = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_GiftContributions_GiftContributionId",
                        column: x => x.GiftContributionId,
                        principalTable: "GiftContributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Gifts",
                columns: new[] { "Id", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "Price", "ReservedPercent" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 5, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Para começar a casa nova com refeições bem cuidadas.", "https://images.unsplash.com/photo-1584990347449-ae6e1f0da4a9?auto=format&fit=crop&w=900&q=80", true, "Jogo de panelas", 420m, 35 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 5, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Uma lembrança para nosso primeiro jantar depois do casamento.", "https://images.unsplash.com/photo-1543353071-10c8ba85a904?auto=format&fit=crop&w=900&q=80", true, "Jantar especial", 280m, 60 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(2026, 5, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ajude com uma parte da nossa viagem e dos passeios.", "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=900&q=80", true, "Cota lua de mel", 900m, 20 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTimeOffset(new DateTime(2026, 5, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Para os cafés da manhã e visitas na casa nova.", "https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?auto=format&fit=crop&w=900&q=80", true, "Cafeteira", 360m, 45 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiftContributions_GiftId",
                table: "GiftContributions",
                column: "GiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExternalReference",
                table: "Payments",
                column: "ExternalReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GiftContributionId",
                table: "Payments",
                column: "GiftContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MercadoPagoPaymentId",
                table: "Payments",
                column: "MercadoPagoPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestConfirmations");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "GiftContributions");

            migrationBuilder.DropTable(
                name: "Gifts");
        }
    }
}
