begin;
update pricing.prices set amount=876543, updated_at=now() where offer_id='457e375f-ceec-4b66-b81e-3829c4e15893' and status='Active';
delete from pricing.offer_price_changes where operation_id='ffffffff-ffff-4fff-8fff-fffffffffff1';
delete from pricing.outbox_messages where payload::text like '%ffffffff-ffff-4fff-8fff-fffffffffff1%';
commit;
