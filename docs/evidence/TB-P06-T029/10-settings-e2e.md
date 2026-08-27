# 10 — Settings E2E (TB-P06-T029)

Commercial gate re-check of settings surfaces. Save/reload semantics inherited from ACCEPTED **TB-P06-T027**.

## Working FE routes (authoritative for preview)

| Surface | URL | HTTP (T029 probe) |
| --- | --- | --- |
| Customer | http://localhost:3000/customer-panel/settings | **200** (`00-route-http-probe.txt`) |
| Customer profile | http://localhost:3000/customer-panel/profile | **200** |
| Seller | http://localhost:3000/vendor-panel/settings | **200** (use `?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5`) |
| Admin | http://localhost:3000/admin/settings | LIVE in inventory / shell |

## Host / BFF notes (honest)

| Path class | Observation |
| --- | --- |
| Seller Host | `GET/PUT /v1/seller/settings` — **200** path used in T027; seller settings remain the clearest Host module surface |
| Customer Host | Profile `GET/PUT /v1/customer/profile` + locale `GET/PUT /v1/customer/preferences` (T027) — not a monolithic `/v1/customer/settings` dump |
| Admin Host | Operator profile only (T027); no invented global platform settings dump |
| Raw `/v1/*/settings` probes | Customer/admin-style catch-all Host paths may **404**; do **not** treat as FE broken — FE uses the panel routes above and T027 BFF/module wiring |

## Inherited ACCEPTED (T027) — not reinvented

| Check | Prior result |
| --- | --- |
| Customer save/reload (profile + locale) | PASS |
| Seller save/reload (store fields) | PASS |
| Admin operator profile save/reload | PASS |
| Unsupported toggles (theme/password/notification fake) | Hidden / deferred |
| Seller foreign deny + employee without `seller.settings.*` | 403 PASS |

## Unsupported toggles

Remain hidden on commercial path (T027 decision). No new fake settings controls introduced in T029.

## Verdict

Settings commercial path **LIVE** via FE panel routes; Host semantics unchanged from T027 ACCEPT.
