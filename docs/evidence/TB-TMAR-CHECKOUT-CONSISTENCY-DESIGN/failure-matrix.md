# Failure matrix

## inventory_reserve
- participant: Inventory
- before commit: TX aborts; no order/cart write
- after local commit: N/A inside ambient TX until Complete
- timeout: abort + ReleaseAcquiredAsync
- duplicate: idempotency key on reserve client key cc-{cart}-{line}
- retry: safe if release/dedupe
- partial: ReleaseAcquiredAsync
- compensation: ReleaseAsync
- manual: false
- user-visible: inventory.supply.unavailable / reservation.conflict

## order_persist
- participant: Order
- before commit: abort; release reserves
- after local commit: still before Complete — ambient rollback if !Complete
- timeout: abort
- duplicate: checkout.conflict → winner reconcile
- retry: return existing
- partial: release reserves
- compensation: ReleaseAcquiredAsync
- manual: false
- user-visible: checkout.conflict

## cart_convert
- participant: Cart
- before commit: order write may have SaveChanges; if Convert fails before Complete ambient TX should rollback Order+Inventory if enlisted
- after local commit: if Convert committed then Complete — durable triple
- timeout: risk of partial if enlistment fails (extraction risk)
- duplicate: ReconcileCartConversionAsync
- retry: reconcile
- partial: CRITICAL shared-ACID debt
- compensation: convert reconcile / cancel
- manual: true
- user-visible: error or existing checkout

## pricing_revalidation
- participant: Pricing
- before commit: reject PRICE_CHANGED; no TX
- after local commit: n/a
- timeout: reject
- duplicate: n/a
- retry: client refresh cart
- partial: n/a
- compensation: none
- manual: false
- user-visible: PRICE_CHANGED

## promotion_eval
- participant: Promotion
- before commit: fail quote; no TX
- after local commit: n/a
- timeout: reject
- duplicate: n/a
- retry: yes
- partial: n/a
- compensation: none
- manual: false
- user-visible: promotion error

## payment_initiate
- participant: Payment/Wallet
- before commit: order already exists unpaid
- after local commit: payment attempt row
- timeout: unpaid expiry policies
- duplicate: payment idempotency / webhook inbox
- retry: careful
- partial: pending payment
- compensation: cancel/expire unpaid; refund if captured
- manual: true
- user-visible: payment pending/failed

## event_publish
- participant: Outbox
- before commit: n/a at submit (module SaveChanges + interceptor)
- after local commit: at-least-once dispatch
- timeout: dispatcher retry
- duplicate: consumer inbox where present
- retry: dispatcher
- partial: delayed fulfillment
- compensation: replay
- manual: true
- user-visible: delayed ship

## compensation_failure
- participant: Inventory release
- before commit: n/a
- after local commit: orphan Held reservation until expiry/manual
- timeout: held until ExpiresAt
- duplicate: release idempotent-ish
- retry: yes
- partial: stock locked
- compensation: ReleaseExpiredBatch / manual
- manual: true
- user-visible: stock unavailable elsewhere
