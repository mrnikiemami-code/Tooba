# 07 — Recipient resolution (TB-P06-T023)

## Mechanism

Notification handlers never trust public request payloads for recipients.

`NotificationProjector` resolves ownership via Order Application bridge:

| Contract | Impl |
|---|---|
| `IOrderNotificationReader` | `OrderNotificationBridge` (`Order` schema only) |

Snapshot: `OrderNotificationRecipientSnapshot` — `CheckoutId`, `BuyerPartyId`, `PlacedByUserId`, `Sellers[]` (`SellerOrderId`, `SellerPartyId`).

## Customer

- Recipient key = `PlacedByUserId` (panel identity)
- Source: checkout by `CheckoutId` or via `SellerOrderId` → checkout
- Own events only; no foreign buyer injection from HTTP

## Seller

- One row per owning seller Party on the checkout / seller-order
- `payment.succeeded` → each `SellerOrder` seller
- Fulfillment / return / refund → seller of that `SellerOrderId` only
- Seller B never listed for Seller A’s order

## Story

- **DEFERRED** — no story review integration events wired; no invented story notifications

## HTTP identity

| Surface | Resolution |
|---|---|
| Customer APIs | `CurrentAuthenticatedSession.UserId` (Dev actor / guest only in Development/Testing) |
| Seller APIs | `SellerPanelAccess.RequireAuthorizedAsync` → seller PartyId |

Recipient ids are **not** accepted from request body.
