-- ============================================================================
-- INCREMENTAL MIGRATION FOR PRODUCTION - PostgreSQL Render
-- ============================================================================
-- File: MIGRATION_AddMercadoPagoPaymentColumns.sql
-- Purpose: Add missing columns to Payments table from Mercado Pago integration
-- Target Database: PostgreSQL (Render)
-- Migration Name: AddMercadoPagoPaymentColumns (20260519120000)
--
-- SAFETY NOTES:
-- 1. ✅ SAFE: Only adds columns, does not modify existing data
-- 2. ✅ SAFE: Uses "IF NOT EXISTS" checks to prevent errors if already applied
-- 3. ✅ SAFE: Adds appropriate indexes for query performance
-- 4. ⚠️ RECOMMENDED: Backup database before running in production
-- 5. ⚠️ RECOMMENDED: Test in development environment first
-- ============================================================================

BEGIN; -- Start transaction for safety

-- Step 1: Add missing columns to Payments table
-- These are the Mercado Pago payment integration columns

-- Add MercadoPagoPaymentId column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'MercadoPagoPaymentId'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "MercadoPagoPaymentId" character varying(120) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column MercadoPagoPaymentId added';
    ELSE
        RAISE NOTICE 'Column MercadoPagoPaymentId already exists';
    END IF;
END $$;

-- Add PreferenceId column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'PreferenceId'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "PreferenceId" character varying(120) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column PreferenceId added';
    ELSE
        RAISE NOTICE 'Column PreferenceId already exists';
    END IF;
END $$;

-- Add InitPoint column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'InitPoint'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "InitPoint" character varying(1000) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column InitPoint added';
    ELSE
        RAISE NOTICE 'Column InitPoint already exists';
    END IF;
END $$;

-- Add SandboxInitPoint column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'SandboxInitPoint'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "SandboxInitPoint" character varying(1000) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column SandboxInitPoint added';
    ELSE
        RAISE NOTICE 'Column SandboxInitPoint already exists';
    END IF;
END $$;

-- Add ExternalReference column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'ExternalReference'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "ExternalReference" character varying(160) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column ExternalReference added';
    ELSE
        RAISE NOTICE 'Column ExternalReference already exists';
    END IF;
END $$;

-- Add PaymentMethod column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'PaymentMethod'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "PaymentMethod" character varying(40) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column PaymentMethod added';
    ELSE
        RAISE NOTICE 'Column PaymentMethod already exists';
    END IF;
END $$;

-- Add PayerName column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'PayerName'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "PayerName" character varying(160) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column PayerName added';
    ELSE
        RAISE NOTICE 'Column PayerName already exists';
    END IF;
END $$;

-- Add PayerEmail column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'PayerEmail'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "PayerEmail" character varying(256) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column PayerEmail added';
    ELSE
        RAISE NOTICE 'Column PayerEmail already exists';
    END IF;
END $$;

-- Add Status column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'Status'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "Status" character varying(40) NOT NULL DEFAULT 'Pending';
        RAISE NOTICE 'Column Status added';
    ELSE
        RAISE NOTICE 'Column Status already exists';
    END IF;
END $$;

-- Add PixQrCode column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'PixQrCode'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "PixQrCode" character varying(4000) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column PixQrCode added';
    ELSE
        RAISE NOTICE 'Column PixQrCode already exists';
    END IF;
END $$;

-- Add PixCopyPaste column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'PixCopyPaste'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "PixCopyPaste" character varying(4000) NOT NULL DEFAULT '';
        RAISE NOTICE 'Column PixCopyPaste added';
    ELSE
        RAISE NOTICE 'Column PixCopyPaste already exists';
    END IF;
END $$;

-- Add UpdatedAt column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Payments' AND column_name = 'UpdatedAt'
    ) THEN
        ALTER TABLE "Payments" ADD COLUMN "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
        RAISE NOTICE 'Column UpdatedAt added';
    ELSE
        RAISE NOTICE 'Column UpdatedAt already exists';
    END IF;
END $$;

-- Step 2: Create indexes for performance (if not exist)

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'Payments' AND indexname = 'IX_Payments_ExternalReference'
    ) THEN
        CREATE UNIQUE INDEX "IX_Payments_ExternalReference" ON "Payments" ("ExternalReference");
        RAISE NOTICE 'Index IX_Payments_ExternalReference created';
    ELSE
        RAISE NOTICE 'Index IX_Payments_ExternalReference already exists';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'Payments' AND indexname = 'IX_Payments_MercadoPagoPaymentId'
    ) THEN
        CREATE INDEX "IX_Payments_MercadoPagoPaymentId" ON "Payments" ("MercadoPagoPaymentId");
        RAISE NOTICE 'Index IX_Payments_MercadoPagoPaymentId created';
    ELSE
        RAISE NOTICE 'Index IX_Payments_MercadoPagoPaymentId already exists';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'Payments' AND indexname = 'IX_Payments_GiftContributionId'
    ) THEN
        CREATE INDEX "IX_Payments_GiftContributionId" ON "Payments" ("GiftContributionId");
        RAISE NOTICE 'Index IX_Payments_GiftContributionId created';
    ELSE
        RAISE NOTICE 'Index IX_Payments_GiftContributionId already exists';
    END IF;
END $$;

-- Step 3: Record the migration in __EFMigrationsHistory (for EF Core tracking)
-- This ensures EF Core knows the migration has been applied

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260519120000_AddMercadoPagoPaymentColumns', '10.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" 
    WHERE "MigrationId" = '20260519120000_AddMercadoPagoPaymentColumns'
);

COMMIT; -- Commit all changes

-- ============================================================================
-- VERIFICATION QUERIES (Run these to confirm everything worked)
-- ============================================================================

-- List all columns in Payments table with data types
-- SELECT column_name, data_type, is_nullable 
-- FROM information_schema.columns 
-- WHERE table_name = 'Payments' 
-- ORDER BY ordinal_position;

-- Check if ExternalReference column exists and is properly configured
-- SELECT column_name, data_type, character_maximum_length 
-- FROM information_schema.columns 
-- WHERE table_name = 'Payments' AND column_name = 'ExternalReference';

-- Verify all indexes exist
-- SELECT indexname FROM pg_indexes WHERE tablename = 'Payments' ORDER BY indexname;

-- Check migration history
-- SELECT * FROM "__EFMigrationsHistory" 
-- WHERE "MigrationId" LIKE '%AddMercadoPago%' 
-- ORDER BY "MigrationId";

-- ============================================================================
-- ROLLBACK (if needed - only run if something went wrong)
-- ============================================================================
-- BEGIN;
-- DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260519120000_AddMercadoPagoPaymentColumns';
-- DROP INDEX IF EXISTS "IX_Payments_GiftContributionId";
-- DROP INDEX IF EXISTS "IX_Payments_MercadoPagoPaymentId";
-- DROP INDEX IF EXISTS "IX_Payments_ExternalReference";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "UpdatedAt";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "PixCopyPaste";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "PixQrCode";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "Status";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "PayerEmail";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "PayerName";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "PaymentMethod";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "ExternalReference";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "SandboxInitPoint";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "InitPoint";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "PreferenceId";
-- ALTER TABLE "Payments" DROP COLUMN IF EXISTS "MercadoPagoPaymentId";
-- COMMIT;
-- ============================================================================
