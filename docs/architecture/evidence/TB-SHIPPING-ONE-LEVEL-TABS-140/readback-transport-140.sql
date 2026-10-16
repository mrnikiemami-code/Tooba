-- TB-SHIPPING-ONE-LEVEL-TABS-140 transport readback (read-only). Messaging DB: tooba_140_messaging on 127.0.0.1:55461.
\pset footer off
select 'T1_queues=' || count(*) from transport.queue;
select 'T2_error_queues=' || count(*) from transport.queue where name like '%\_error%' escape '\';
select 'T3_messages=' || count(*) from transport.message;
select 'T4_deliveries=' || count(*) from transport.message_delivery;
select 'T5_pending_delivery=' || m.message_id || '|corr=' || m.correlation_id || '|sent=' || m.sent_time || '|enqueued=' || d.enqueue_time || '|delivery_count=' || d.delivery_count || '|consumer=' || coalesce(d.consumer_id::text, 'none') || '|eventType=' || coalesce((m.body::json ->> 'eventType'), '-') || '|eventId=' || coalesce((m.body::json ->> 'eventId'), '-') from transport.message m join transport.message_delivery d on d.transport_message_id = m.transport_message_id;
