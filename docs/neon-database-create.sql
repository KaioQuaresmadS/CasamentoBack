CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Gifts" (
    "Id" uuid NOT NULL,
    "Name" character varying(140) NOT NULL,
    "Description" character varying(600) NOT NULL,
    "ImageUrl" character varying(1000) NOT NULL,
    "Price" numeric(10,2) NOT NULL,
    "ReservedPercent" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Gifts" PRIMARY KEY ("Id")
);

CREATE TABLE "GuestConfirmations" (
    "Id" uuid NOT NULL,
    "FullName" character varying(160) NOT NULL,
    "Phone" character varying(30) NOT NULL,
    "GuestsCount" integer NOT NULL,
    "WillAttend" boolean NOT NULL,
    "Notes" character varying(600),
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_GuestConfirmations" PRIMARY KEY ("Id")
);

CREATE TABLE "Roles" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "RoleType" integer NOT NULL,
    "Description" character varying(500) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Email" character varying(256) NOT NULL,
    "Name" character varying(160) NOT NULL,
    "PasswordHash" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastLoginAt" timestamp with time zone,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "GiftContributions" (
    "Id" uuid NOT NULL,
    "GiftId" uuid NOT NULL,
    "ContributorName" character varying(160) NOT NULL,
    "ContributorPhone" character varying(30) NOT NULL,
    "Mode" character varying(30) NOT NULL,
    "QuotaQuantity" integer NOT NULL,
    "Amount" numeric(10,2) NOT NULL,
    "PaymentStatus" character varying(30) NOT NULL,
    "PixKey" character varying(160) NOT NULL,
    "QrCodePayload" character varying(1000) NOT NULL,
    "ProviderPaymentId" character varying(120) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "PaidAt" timestamp with time zone,
    CONSTRAINT "PK_GiftContributions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GiftContributions_Gifts_GiftId" FOREIGN KEY ("GiftId") REFERENCES "Gifts" ("Id") ON DELETE CASCADE
);

CREATE TABLE "UserRoles" (
    "RolesId" uuid NOT NULL,
    "UsersId" uuid NOT NULL,
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("RolesId", "UsersId"),
    CONSTRAINT "FK_UserRoles_Roles_RolesId" FOREIGN KEY ("RolesId") REFERENCES "Roles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserRoles_Users_UsersId" FOREIGN KEY ("UsersId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Payments" (
    "Id" uuid NOT NULL,
    "GiftContributionId" uuid NOT NULL,
    "Amount" numeric(10,2) NOT NULL,
    "MercadoPagoPaymentId" character varying(120) NOT NULL,
    "PreferenceId" character varying(120) NOT NULL,
    "InitPoint" character varying(1000) NOT NULL,
    "SandboxInitPoint" character varying(1000) NOT NULL,
    "ExternalReference" character varying(160) NOT NULL,
    "PaymentMethod" character varying(40) NOT NULL,
    "PayerName" character varying(160) NOT NULL,
    "PayerEmail" character varying(256) NOT NULL,
    "Status" character varying(40) NOT NULL,
    "PixQrCode" character varying(4000) NOT NULL,
    "PixCopyPaste" character varying(4000) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Payments_GiftContributions_GiftContributionId" FOREIGN KEY ("GiftContributionId") REFERENCES "GiftContributions" ("Id") ON DELETE CASCADE
);

INSERT INTO "Gifts" ("Id", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "Price", "ReservedPercent")
VALUES ('11111111-1111-1111-1111-111111111111', TIMESTAMPTZ '2026-05-18T00:00:00+00:00', 'Para começar a casa nova com refeições bem cuidadas.', 'https://images.unsplash.com/photo-1584990347449-ae6e1f0da4a9?auto=format&fit=crop&w=900&q=80', TRUE, 'Jogo de panelas', 420.0, 35);
INSERT INTO "Gifts" ("Id", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "Price", "ReservedPercent")
VALUES ('22222222-2222-2222-2222-222222222222', TIMESTAMPTZ '2026-05-18T00:00:00+00:00', 'Uma lembrança para nosso primeiro jantar depois do casamento.', 'https://images.unsplash.com/photo-1543353071-10c8ba85a904?auto=format&fit=crop&w=900&q=80', TRUE, 'Jantar especial', 280.0, 60);
INSERT INTO "Gifts" ("Id", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "Price", "ReservedPercent")
VALUES ('33333333-3333-3333-3333-333333333333', TIMESTAMPTZ '2026-05-18T00:00:00+00:00', 'Ajude com uma parte da nossa viagem e dos passeios.', 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=900&q=80', TRUE, 'Cota lua de mel', 900.0, 20);
INSERT INTO "Gifts" ("Id", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "Price", "ReservedPercent")
VALUES ('44444444-4444-4444-4444-444444444444', TIMESTAMPTZ '2026-05-18T00:00:00+00:00', 'Para os cafés da manhã e visitas na casa nova.', 'https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?auto=format&fit=crop&w=900&q=80', TRUE, 'Cafeteira', 360.0, 45);

INSERT INTO "Roles" ("Id", "CreatedAt", "Description", "IsActive", "Name", "RoleType")
VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', TIMESTAMPTZ '2026-05-19T00:00:00+00:00', 'Acesso administrativo completo.', TRUE, 'Admin', 1);
INSERT INTO "Roles" ("Id", "CreatedAt", "Description", "IsActive", "Name", "RoleType")
VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', TIMESTAMPTZ '2026-05-19T00:00:00+00:00', 'Usuario autenticado.', TRUE, 'User', 2);
INSERT INTO "Roles" ("Id", "CreatedAt", "Description", "IsActive", "Name", "RoleType")
VALUES ('cccccccc-cccc-cccc-cccc-cccccccccccc', TIMESTAMPTZ '2026-05-19T00:00:00+00:00', 'Convidado com acesso publico.', TRUE, 'Guest', 3);

CREATE INDEX "IX_GiftContributions_GiftId" ON "GiftContributions" ("GiftId");

CREATE UNIQUE INDEX "IX_Payments_ExternalReference" ON "Payments" ("ExternalReference");

CREATE INDEX "IX_Payments_GiftContributionId" ON "Payments" ("GiftContributionId");

CREATE INDEX "IX_Payments_MercadoPagoPaymentId" ON "Payments" ("MercadoPagoPaymentId");

CREATE INDEX "IX_UserRoles_UsersId" ON "UserRoles" ("UsersId");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260520124636_InitialCreate', '10.0.8');

COMMIT;

START TRANSACTION;
ALTER TABLE "Payments" ALTER COLUMN "PixQrCode" TYPE text;

ALTER TABLE "Payments" ALTER COLUMN "PixCopyPaste" TYPE text;

ALTER TABLE "Payments" ADD "QrCode" text NOT NULL DEFAULT '';

ALTER TABLE "Payments" ADD "QrCodeBase64" text NOT NULL DEFAULT '';

ALTER TABLE "Payments" ADD "TicketUrl" character varying(1000) NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260618120000_AddPixPaymentFields', '10.0.8');

COMMIT;

START TRANSACTION;
ALTER TABLE "Payments" ADD "Barcode" character varying(2000) NOT NULL DEFAULT '';

ALTER TABLE "Payments" ADD "LinhaDigitavel" character varying(2000) NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260619120000_AddBoletoPaymentFields', '10.0.8');

COMMIT;

START TRANSACTION;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260619145919_UpdateDatabaseForNeon', '10.0.8');

COMMIT;

