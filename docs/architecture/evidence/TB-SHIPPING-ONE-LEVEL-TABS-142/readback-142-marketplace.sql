-- TB-SHIPPING-ONE-LEVEL-TABS-142 — read-only scratch readback (Marketplace edition).
\echo === schemas_present ===
select nspname from pg_namespace where nspname not like 'pg_%' and nspname <> 'information_schema' order by 1;

\echo === settlement_migration_history ===
select migration_id from settlement.__ef_migrations_history order by migration_id;

\echo === settlement_tables ===
select table_name from information_schema.tables where table_schema = 'settlement' order by 1;

\echo === shipping_group_fees_columns ===
select column_name, data_type, is_nullable
from information_schema.columns
where table_schema = 'settlement' and table_name = 'shipping_group_fees'
order by ordinal_position;

\echo === shipping_group_fees_indexes ===
select indexname from pg_indexes where schemaname = 'settlement' and tablename = 'shipping_group_fees' order by 1;

\echo === shipping_group_fees_rowcount ===
select count(*) as rows from settlement.shipping_group_fees;

\echo === per_module_migration_history_presence ===
select n.nspname as schema_name,
       (select count(*) from information_schema.tables t
         where t.table_schema = n.nspname and t.table_name = '__ef_migrations_history') as ef_history_tables
from pg_namespace n
where n.nspname not like 'pg_%' and n.nspname <> 'information_schema'
order by 1;
