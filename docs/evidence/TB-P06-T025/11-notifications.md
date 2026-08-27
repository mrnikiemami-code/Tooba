# 11 — Notifications on admin reply

Task: TB-P06-T025

## Rule

On Admin **public** reply (`isInternalNote=false`): create Notification to Customer or Seller requester with deep-link:

- Customer: `/customer-panel/tickets/{ticketId}`
- Seller: `/vendor-panel/tickets/{ticketId}`

Internal notes: **no** notification.

Admin inbox recipient: **skip** if no model (do not fake).

## Implementation preference

Outbox integration event from Support → Notification handler → `INotificationDirectory.CreateIfAbsentAsync` (Returns pattern). Direct directory inject acceptable if sibling worker chose that.

## Allowlist

`NotificationTargetRoutes` must accept `/customer-panel/tickets/...` and `/vendor-panel/tickets/...` prefixes (already under `/customer-panel` / `/vendor-panel`).

Helpers `CustomerTicket` / `SellerTicket` optional when contracts land.
