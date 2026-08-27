# 02 — Backend WIP reconciliation (TB-P06-T019-R1)

## Preserved direction

| Capability | Status |
|---|---|
| Story Origin `Admin` \| `Seller` | Live in domain + EF |
| `SellerPartyId` ownership | Live; seller queries scoped |
| Submit for review | Domain + `SellerSubmitAsync` + `POST .../submit` |
| Approve / Reject | Domain + admin directory + `POST .../approve|reject` |
| Rejection reason | Required; max 500 |
| Review actor / timestamps | `Submitted*` / `Reviewed*` columns |
| Seller direct publish | Forbidden (domain `EnsurePublicationEligible`; no seller enable route) |
| Public eligibility | Admin origin **or** `ReviewStatus.Approved`, then `IsPubliclyVisible` Active/window |
| Seller own-data scoping | `SellerList` / `SellerGet` / mutations filter Origin+SellerPartyId |
| Admin review ops | Approve/Reject + pending list filter |
| No request-supplied seller identity authority | Seller routes use `SellerPanelAccess` (actor + SpiceDB); header is context only |

## Repairs applied in this slice

1. **EF migration** `AddStoryReviewOwnership` — columns: Origin, ReviewStatus, SellerPartyId, SubmittedByActorUserId, ReviewedByActorUserId, SubmittedAt, ReviewedAt, RejectionReason; indexes `(TenantId, ReviewStatus)`, `(TenantId, SellerPartyId)`. Snapshot/designer updated.
2. **`GetPublicStoriesAsync`** — SQL filter requires `(Origin == Admin || ReviewStatus == Approved)` **and** `Status == Active`; in-memory `IsPubliclyVisible` still enforces publication eligibility + StartAt/EndAt window.
3. **Tests** — seller draft/submitted/rejected not public; approved+activate public; seller cannot activate (domain + no API route); unauthorized seller 401; foreign seller isolation; reject requires reason; admin approve idempotent; seed seller titles excluded from public.

## Seed policy

- Keep admin Active storefront samples (`موبایل`, `بازی`, `English rail`).
- Seller samples: Draft + Submitted only (not public).

## Not in this backend slice

- Shared frontend Story management components / seller routes UI.
- Commit / push / Bridge Result (explicitly deferred by worker instruction for this subagent).
