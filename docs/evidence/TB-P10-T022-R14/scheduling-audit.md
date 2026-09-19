# Scheduling Audit

## Existing time windows

| Area | Pattern |
|---|---|
| `AuthoredPrice` | `ValidFrom` / `ValidTo?` UTC |
| `PromotionDefinition` | `EffectiveFrom` / `EffectiveTo?` UTC; status Draft/Active/Expired |
| Landing publish | Status Draft/Published (not time-scheduled publish table) |

## Background jobs found

Hosted services (examples): `UnpaidOrderExpiryHostedService`, `CartExpiryHostedService`, `PaymentReconciliationHostedService`, `OutboxDispatcherHostedService`, `LanguageBootstrapHostedService`. **No Quartz / Hangfire.** No campaign activation job.

## Countdown / timer

No stored per-second countdown. Any UI timer must be **derived** from `EndAt - UtcNow` on read.

## Recommendation for Amazing

- Persist `StartAt` / `EndAt` (UTC) on campaign.
- Active = query-derived: `Status=Published/Active AND StartAt <= now AND (EndAt IS NULL OR EndAt > now)` OR materialize status via rare job — **prefer query-derived** for foundation (matches Price/Promotion windows).
- Teasing = `StartAt > now` (and allowed visibility flag).
- Expired = `EndAt <= now` (filter out of active storefront).
- Optional lightweight sweeper later to flip status for admin lists — **not required** for correct storefront if queries use time predicates.
- Cache invalidation on campaign write + price/stock events; no timer tick writes.
