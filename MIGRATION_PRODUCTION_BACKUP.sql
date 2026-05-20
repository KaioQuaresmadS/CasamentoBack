-- ============================================================================
-- MIGRATION BACKUP FOR PRODUCTION - PostgreSQL Render
-- ============================================================================
-- File: MIGRATION_PRODUCTION_BACKUP.sql
-- Purpose: Manual SQL script to apply migrations if automatic migration fails
-- Target Database: PostgreSQL (Render)
-- 
-- SAFETY WARNINGS:
-- 1. This script does NOT delete existing data
-- 2. It only adds new tables and columns if they don't exist
-- 3. Run in Production with caution during maintenance window
-- 4. Backup database BEFORE running this script
-- ============================================================================

-- Step 1: Check if Payments table exists
-- If it already exists, the CREATE TABLE will be skipped by application logic
CREATE TABLE IF NOT EXISTS "Payments" (
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
    CONSTRAINT "FK_Payments_GiftContributions_GiftContributionId" 
        FOREIGN KEY ("GiftContributionId") 
        REFERENCES "GiftContributions" ("Id") 
        ON DELETE CASCADE
);

-- Step 2: Create unique index on ExternalReference (if not exists)
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Payments_ExternalReference" 
    ON "Payments" ("ExternalReference");

-- Step 3: Create regular indexes (if not exist)
CREATE INDEX IF NOT EXISTS "IX_Payments_GiftContributionId" 
    ON "Payments" ("GiftContributionId");

CREATE INDEX IF NOT EXISTS "IX_Payments_MercadoPagoPaymentId" 
    ON "Payments" ("MercadoPagoPaymentId");

-- Step 4: Record the migration in __EFMigrationsHistory (for EF Core tracking)
-- This ensures EF Core knows the migration has been applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260520011348_InitialCreate', '10.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" 
    WHERE "MigrationId" = '20260520011348_InitialCreate'
);

-- ============================================================================
-- VERIFICATION QUERIES (Run these to confirm everything worked)
-- ============================================================================

-- Verify Payments table structure
-- SELECT * FROM information_schema.columns WHERE table_name = 'Payments' ORDER BY ordinal_position;

-- Verify column ExternalReference exists
-- SELECT * FROM information_schema.columns WHERE table_name = 'Payments' AND column_name = 'ExternalReference';

-- Verify indexes
-- SELECT * FROM pg_indexes WHERE tablename = 'Payments';

-- ============================================================================
-- ALTERNATIVE: If you need to manually add ExternalReference column to existing Payments table:
-- ============================================================================
-- ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "ExternalReference" character varying(160) NOT NULL DEFAULT '';
-- CREATE UNIQUE INDEX IF NOT EXISTS "IX_Payments_ExternalReference" ON "Payments" ("ExternalReference");
-- ============================================================================
