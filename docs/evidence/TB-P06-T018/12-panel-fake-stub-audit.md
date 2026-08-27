# 12 — Panel fake / stub audit (TB-P06-T018)

## Audit targets

| Risk | Wave 1 disposition |
|---|---|
| Fake KPI on dashboards | Not introduced; existing live metrics retained |
| Static arrays posing as API data | Forbidden for selected surfaces |
| Fake wallet balances | Customer wallet hidden from nav/dashboard; no fake fill |
| Fake tickets | Hidden; capability shells only |
| Fake notification counts | Hidden; no badge invented |
| Fake coupons | Seller coupons hidden from nav |
| Fake reviews (seller) | Seller reviews hidden from nav |
| Fake settings save | Customer security/notification unavailable; seller no business-profile save; admin settings hidden |
| Button without API | Removed/hidden for selected gaps |
| Placeholder route in primary nav | Removed for deferred capabilities |

## Customer

| Surface | Before risk | After Wave 1 |
|---|---|---|
| Nav wallet/tickets/gift-cards/notifications | Advertised or stub-labeled | Hidden from primary nav |
| Dashboard wallet tile | Stub “به‌زودی” | Removed |
| Dashboard settings | Stub | Live |
| Settings security/notifications | Risk of fake save | Honest unavailable |

## Seller

| Surface | Before risk | After Wave 1 |
|---|---|---|
| Nav customers/coupons/reviews/tickets/gift-cards | Stub “به‌زودی” in primary nav | Hidden |
| Settings page | Capability shell / N/A | Live operational read |
| Dashboard settings N/A tile | Stub | Removed; live settings action |

## Admin

| Surface | Before risk | After Wave 1 |
|---|---|---|
| Nav settings | Advertised without module | Hidden |

## Remaining honest stubs (allowed)

Deep-link capability shells for deferred modules remain **intentionally** honest-unavailable. They must not invent balances, threads, or unread counts.
