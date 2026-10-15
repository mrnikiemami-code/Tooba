select 'migration_rows=' || count(*) from fulfillment.__ef_migrations_history where migration_id like '%AddShippingOneLevelCatalog%';
select 'orders=' || count(*) from "order".orders;
select 'fulfillments=' || count(*) from "order".fulfillments;
select 'checkouts=' || count(*) from "order".checkouts;

