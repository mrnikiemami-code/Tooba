# 04 — Domain model (Support)

Task: TB-P06-T025

## Schema

`support` (EF default schema; no cross-module SQL JOINs).

## Aggregates

### Ticket

| Field | Notes |
|-------|-------|
| TicketId | `UuidV7.New()` |
| RequesterKind | Customer \| Seller \| Admin |
| RequesterActorUserId | required |
| RequesterPartyId | optional |
| SellerPartyId | optional; seller scope key |
| Subject | required |
| Category | Order \| Payment \| Return \| Product \| Other |
| Priority | Low \| Normal \| High |
| Status | Open \| InProgress \| WaitingForCustomer \| WaitingForSeller \| Resolved \| Closed |
| AssignedOperatorActorUserId | optional |
| RelatedEntityType / RelatedEntityId | opaque; Order validated via Order Application gateway |
| CreatedAt / UpdatedAt / ClosedAt / LastMessageAt | timestamps |

**TenantId:** not on entity — CommerceContext / DB resolver (Party/Returns pattern).

### TicketMessage

| Field | Notes |
|-------|-------|
| MessageId | UuidV7 |
| TicketId | FK within support schema |
| AuthorKind | Customer \| Seller \| Admin \| System |
| AuthorActorUserId | required |
| Body | max 4000 |
| CreatedAt | |
| IsInternalNote | never returned on Customer/Seller APIs |

## Policy sketches

- Customer: own tickets only (`RequesterActorUserId`).
- Seller: scoped to `SellerPartyId` + `support.*` capability projection for nav; Host authz via SellerPanelAccess + ownership.
- Admin: `support.view` / `support.manage`; internal notes visible.
- Customer close: Open/Resolved → Closed; reopen Closed → Open.
- Related Order: Application contract only (no JOIN).
