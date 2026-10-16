-- TB-SHIPPING-ONE-LEVEL-TABS-141 — read-only schema provenance readback on the DISPOSABLE scratch tenant DB.
-- No writes. Runs against 127.0.0.1:55462 / tooba_141_scratch only.

-- A. Namespaces created by the isolated Development Host startup auto-migration path (phase 1 of
--    Host/Development/DevelopmentSchemaMigrator.MigrateOrderedAsync, i.e. every registered
--    IModuleSchemaMigrator). "settlement" and "returns" are absent because neither module registers one.
select 'A.schemas' as section, nspname as item
from pg_namespace
where nspname not like 'pg_%' and nspname <> 'information_schema'
order by 1, 2;

-- B. Number of Settlement-owned tables reachable at that point (0 == the TB-138 finding reproduced).
select 'B.settlement_tables_before_activation' as section,
       count(*)::text as item
from information_schema.tables
where table_schema = 'settlement';

-- C. After the module's explicit activation (Settlement.Infrastructure migrator applied through the
--    module's own ToobaNpgsql.ConfigureModuleContext seam, history table settlement.__ef_migrations_history).
select 'C.settlement_migration_history' as section, migration_id as item
from settlement.__ef_migrations_history
order by 1, 2;

select 'D.settlement_tables_after_activation' as section,
       count(*)::text as item
from information_schema.tables
where table_schema = 'settlement';

-- E. The per-group pending ledger shape: identity + accepted customer charge are NOT NULL; every
--    commercial/funding/tax column is NULLABLE and therefore stores NULL, never a silent zero.
select 'E.' || column_name || ' nullable=' || is_nullable as section, data_type as item
from information_schema.columns
where table_schema = 'settlement' and table_name = 'shipping_group_fees'
order by 1, 2;

-- F. Rows in the pending ledger (must be 0: nothing was fabricated, no commercial row was written).
select 'F.shipping_group_fees_rows' as section, count(*)::text as item
from settlement.shipping_group_fees;

-- G. No seller settlement ledger / payout row was created by schema activation.
select 'G.settlement_entries_rows' as section, count(*)::text as item from settlement.settlement_entries;
select 'G.payout_requests_rows' as section, count(*)::text as item from settlement.payout_requests;
