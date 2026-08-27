# 20 — Notification authorization / isolation (TB-P06-T023)

## Controls

| Check | Mechanism |
|---|---|
| Customer own list | Session/actor scoped `RecipientPartyId` |
| Customer foreign mark/delete | Directory filter → false → HTTP 404 |
| Seller own list | `SellerPanelAccess` + seller PartyId |
| Seller foreign mark/delete | Same → 404 |
| Cross-seller event | Projector creates per owning seller only; test asserts seller B empty |
| Target ownership | Event-derived ids + allowlist; no body recipient override |
| Admin inbox | **Not implemented** (not required) |

## Test coverage

`NotificationFoundationTests`:

- Static endpoint / allowlist / module boundary
- Idempotent create + mark-read + cross-seller isolation
- Payment-succeeded projection path (checkout → customer + sellers) when Docker available

## Claim

Isolation is structural (recipient keys + unique index). No cross-tenant notification table sharing beyond commerce connection isolation pattern.
