# 09 — Seller notification API (TB-P06-T023)

Same host file as customer: `NotificationEndpoints.cs`.

## Endpoints

| Method | Path |
|---|---|
| GET | `/v1/seller/notifications` |
| GET | `/v1/seller/notifications/unread-count` |
| POST | `/v1/seller/notifications/{id}/read` |
| POST | `/v1/seller/notifications/read-all` |
| DELETE | `/v1/seller/notifications/{id}` |

## Behavior

- Current seller only via `SellerPanelAccess.RequireAuthorizedAsync`
- Filter: `RecipientKind.Seller` + authorized `sellerPartyId`
- Pagination, newest-first, locale-safe copy (same as customer)
- Real unread count; foreign notification → 404 (no mutation)
- Soft-delete dismiss mirrors customer

## Isolation

- Tenant/seller isolation from panel authorization + recipient PartyId
- Cross-seller: covered in `NotificationFoundationTests` (seller A row invisible / unreadable as seller B)
