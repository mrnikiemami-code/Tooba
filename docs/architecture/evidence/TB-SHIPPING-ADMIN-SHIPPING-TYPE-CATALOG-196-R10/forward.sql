START TRANSACTION;

CREATE INDEX ix_outbox_messages_pending ON fulfillment.outbox_messages (occurred_at) WHERE processed_at IS NULL AND dead_lettered_at IS NULL;

INSERT INTO fulfillment.__ef_migrations_history (migration_id, product_version)
VALUES ('20261022182208_AddFulfillmentOutboxPendingIndex', '8.0.11');

COMMIT;


