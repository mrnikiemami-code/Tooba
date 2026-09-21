# Notification–Order parity — TB-TMAR-NEXT-MODULE-BATCH-003-R1

## Verdict

**No notification-recipient logic changed.** Only the public type home moved from `Tooba.Order.Application` → `Tooba.Order.Contracts.Notifications`.

## Preserved behaviors (NotificationProjector)

| Behavior | Status |
|---|---|
| `GetByCheckoutIdAsync` lookup semantics | PRESERVED (stub + bridge contract identical) |
| `GetBySellerOrderIdAsync` lookup semantics | PRESERVED (bridge still resolves via checkout) |
| customer recipient = `PlacedByUserId` | PRESERVED |
| seller recipient iteration over `Sellers` | PRESERVED |
| fallback seller = exact SellerOrderId match else `Sellers.FirstOrDefault()` | PRESERVED |
| checkout/seller route derivation via caller factories | PRESERVED |
| `EnrichBuyer` no-op payload enrichment | PRESERVED |
| SourceEventId / SourceType pass-through | PRESERVED |

## OrderNotificationBridge

Method bodies unchanged aside from Contracts namespace import. Same EF AsNoTracking queries and snapshot construction.

## Focused tests

- `ProjectFromCheckout_creates_customer_and_seller_recipients`
- `ProjectFromSellerOrder_preserves_fallback_and_target_behavior` (exact match + missing-seller fallback)

Notification-recipient logic change count: **0**
