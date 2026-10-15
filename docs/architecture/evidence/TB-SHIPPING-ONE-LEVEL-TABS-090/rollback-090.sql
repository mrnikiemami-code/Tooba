-- TB-SHIPPING-ONE-LEVEL-TABS-090 — rollback for the Development one-level backfill + migration.
-- Scope: fulfillment schema only. It never touches checkout/order/payment/settlement data and
-- never rewrites a legacy shipping service or option row.
--
-- Order matters. Step 1 removes ONLY the rows the backfill created (all seven are new rows; the
-- legacy catalog is untouched and is the operational read authority either way). Step 2 is the
-- migration down (drops the two new tables). Run step 2 only if the schema itself must be reverted.
--
-- Verified pre-state before any write (docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-090):
--   fulfillment.shipping_methods            = 0 rows
--   fulfillment.shipping_method_translations = 0 rows
--   fulfillment.shipping_services            = 5 rows (unchanged)
--   fulfillment.shipping_service_options     = 4 rows (unchanged)
-- Full logical backup: tooba_alpha-pre-090.dump (pg_dump -Fc, 2492453 bytes, 1190 TOC entries).
-- Schema-only backup: fulfillment-schema-pre-090.sql, order-cart-shipping-drafts-pre-090.sql.

-- Step 1 (DML rollback): remove the seven backfilled one-level methods and their translations.
BEGIN;

DELETE FROM fulfillment.shipping_method_translations
WHERE shipping_method_id IN (
    SELECT shipping_method_id FROM fulfillment.shipping_methods
    WHERE method_code IN (
        'post:express', 'post:standard', 'tipax:express', 'tipax:standard',
        'snapp_courier', 'store_courier', 'in_person'));

DELETE FROM fulfillment.shipping_methods
WHERE method_code IN (
    'post:express', 'post:standard', 'tipax:express', 'tipax:standard',
    'snapp_courier', 'store_courier', 'in_person');

-- Guard: no legacy row may have been created or removed by this rollback.
SELECT
    (SELECT count(*) FROM fulfillment.shipping_services) AS legacy_services,   -- must stay 5
    (SELECT count(*) FROM fulfillment.shipping_service_options) AS legacy_options; -- must stay 4

COMMIT;

-- Step 2 (DDL rollback, optional): revert the additive migration through the canonical runner,
-- never by hand:
--   Tooba.MigrationRunner.exe status --tenant store-alpha   (read back first)
--   revert via the project's EF migrator to 20260910170000_AddShippingServiceCatalog
--     (this is the DOWN path already proven on the isolated DB in TB-SHIPPING-ONE-LEVEL-TABS-083)
--   then Tooba.MigrationRunner.exe status --tenant store-alpha again.
--
-- Full-database restore path (only if a wider revert is ever required):
--   pg_restore --clean --if-exists --no-owner --no-privileges -d tooba_alpha tooba_alpha-pre-090.dump
