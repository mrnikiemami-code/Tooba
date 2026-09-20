# Outbox / Inbox readiness

## Outbox registrations present
Order, Cart, Inventory, Payment, Offer, Pricing, Tax, Promotion, Fulfillment, Settlement, Catalog, Party, Identity, Returns, Notification, AccessControl, PlatformProbe (module OutboxRegistration pattern).

## Inbox / dedupe present (examples)
- PaymentWebhookInbox
- OrderPaymentInbox
- FulfillmentPaymentInbox
- Settlement payment/refund inbox
- Inventory ReturnRestockInbox

## Gaps before extraction
- No general Inbox for all checkout workflow commands
- Submit path relies on shared TransactionScope enlistment rather than Outbox choreography between Order/Cart/Inventory
- Exactly-once NOT guaranteed; at-least-once + consumer idempotency must be assumed
- Need durable Checkout process state + command idempotency store before removing shared TX
