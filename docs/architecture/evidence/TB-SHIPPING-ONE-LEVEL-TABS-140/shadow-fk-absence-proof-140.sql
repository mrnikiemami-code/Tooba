-- TB-SHIPPING-ONE-LEVEL-TABS-140: proof that the migrated schema has NO shadow column, i.e. the
-- exact write EF Core attempted with the un-ignored read-only navigation must fail on PostgreSQL.
-- This is an intentionally failing statement; it inserts nothing and mutates nothing.
select 'M1_column_present_in_migrated_schema=' || count(*) from information_schema.columns where table_schema = 'fulfillment' and table_name = 'shipping_groups' and column_name = 'fulfillment_unit_fulfillment_id';
insert into fulfillment.shipping_groups (fulfillment_unit_fulfillment_id) values (null);
