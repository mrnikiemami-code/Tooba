# R3 reservation lifecycle discovery — TB-P09-T022-R3

## Problem
Cart-style Inventory holds use `ExpiresAt` TTL. After payment success, those holds stayed TTL-eligible, so `ReleaseExpiredHoldsAsync` could release stock for already-paid orders.

## Ownership
| Concern | Owner |
| --- | --- |
| Clear cart TTL on paid hold | Inventory domain `CommitForPaidOrder` |
| Directory API | `IInventoryDirectory.CommitReservationForPaidOrderAsync` |
| Primary payment wiring | `OrderPaymentBridge.ApplyVerifiedSuccessAsync` |
| Secondary safety | `FulfillmentDirectory.EnsureCreatedForPaidCheckoutAsync` (idempotent) |
| Expiry worker | Unchanged SQL: `expires_at IS NOT NULL` |

## Non-goals
- Consolidated Package shipping/UI unchanged
- No T023
- Do not resurrect Released/Consumed
