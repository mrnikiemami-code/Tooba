START TRANSACTION;

DROP INDEX fulfillment.ix_outbox_messages_pending;

DELETE FROM fulfillment.__ef_migrations_history
WHERE migration_id = '20261022182208_AddFulfillmentOutboxPendingIndex';

COMMIT;


