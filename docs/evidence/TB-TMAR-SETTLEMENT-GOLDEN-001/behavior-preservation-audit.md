# Behavior preservation audit

Preserved seller routes: balance, entries, statements, payout list, payout request + seller auth via Host adapter.

Preserved admin routes: balances, payout queue, payout grid query, process, retry + admin auth (including Marketplace Dev synthetic tenant).

Preserved: amount/currency, idempotency, payout state transitions, process/retry rules, seller display names (IPartyLookup), Pending|Failed grid default (Infrastructure engine), outbox/event behavior.

Grid Normalize moved to AdminPayoutGridQueryPolicy (module-owned) with same field whitelist seller/amount/status/created and default sort created desc.

Accidental behavior change: 0 intended.
Settlement-Behavior-Preservation: VERIFIED
Settlement-Grid-Ownership: MODULE_OWNED
