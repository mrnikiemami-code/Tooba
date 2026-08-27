# 01 — Runtime before locale routing (TB-P06-T016)

## Predecessor

| Field | Value |
|---|---|
| Task | TB-P06-T016 Locale-Prefixed Public Routing End-to-End |
| Predecessor commit | `bcf0bc33cbbbb2d13e91ba4125e7485f0cc88b30` (T015) |
| Branch | `main` |
| Pipeline | BRIDGE-WAKE-V1 / `tooba-main` |
| Bridge UUID | `da642f46-9fed-4147-ba31-5e674ae53865` |

## Baseline probes (pre-ship)

| Probe | Result |
|---|---|
| `GET http://127.0.0.1:5088/health/live` | 200 |
| `GET http://127.0.0.1:5088/health/ready` | 200 |
| Tooba Home `http://127.0.0.1:3000/` | 200 (unprefixed; cookie locale only) |
| Blog `/blogs` | 200 unprefixed |
| PDP `/products/{slug}` | 200 unprefixed |
| Shopeiva `http://127.0.0.1:3001/` | 200 |
| Public `/{locale}/...` routes | MISSING pre-Task |
| Middleware locale rewrite | MISSING pre-Task |
| Sitemap locale-prefixed entries | MISSING pre-Task |
| hreflang alternates | NOT emitted (T014 deferred) |

## Notes

- T014 delivered cookie `tooba_locale` + root `lang`/`dir` RTL/LTR foundation.
- T015 composition live; public URLs still unprefixed → SEO duplicate risk once second locale published.
- Panels (`/admin`, `/customer-panel`, `/vendor-panel`) intentionally unprefixed (account-scoped, not SEO-public).
