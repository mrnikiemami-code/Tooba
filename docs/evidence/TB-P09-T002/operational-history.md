# Operational history

Host `AdminOrderCompletenessComposer` merges checkout submitted, cancel, payment ops, fulfillment/shipment fields, returns+refunds, settlement accrual/adjustment, notes. Paged in memory (`page`/`pageSize`). Actor «توسط سیستم» / operator prefix. No new event store.
