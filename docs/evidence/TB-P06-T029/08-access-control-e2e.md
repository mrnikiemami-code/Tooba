# 08 — Access Control E2E (TB-P06-T029)

Host APIs + FE shells · no direct DB mutation · commercial gate re-probe + ACCEPTED inheritance from TB-P06-T024-R2.

## Identity (dev)

| Role | Id |
| --- | --- |
| Admin actor | `01a036c2-970e-7000-8eb7-94bf5cc2d8db` (from `/v1/admin/dev-context`) |
| Seller user | `01a03628-3f68-7000-844d-99f1cadb54b0` |
| Seller party | `01a030d1-40cb-7000-8abe-6d31739956c5` |

## Probed this session (T029)

| Check | Result |
| --- | --- |
| Seller `GET /v1/seller/access-control/roles` | **200** |
| Admin ACL surface (roles/capabilities path) | **200** |
| FE Seller ACL | http://localhost:3000/vendor-panel/access-control?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5 · **200** (`03-navigation-integrity.md`) |
| FE Admin ACL | http://localhost:3000/admin/access-control · **200** |

## Inherited ACCEPTED proof (T024-R2 / T024-R1) — not re-executed end-to-end this session

| Capability | Prior evidence |
| --- | --- |
| Admin seller ceiling | T024-R2 demo-preview + ceiling seed |
| Seller custom role + real Category ScopeEditor | T024-R1 `02-real-category-scope-selector.md` |
| Restricted employee: Mobile allowed / Books denied | T024-R1 `14-mobile-vs-books-real-e2e.md` (Host.Tests + foundation) |
| Nav / action capability projection | T024-R1 `10-navigation-capability-projection.md`, `11-action-capability-projection.md` |
| Direct API deny on out-of-scope order | T024-R1 order list/detail scope proofs |

## Verdict

ACL commercial surfaces **LIVE** and open. Full Mobile/Books scoped-employee walkthrough remains **proven under ACCEPTED T024-R\***; this gate confirms Host role list + FE ACL routes still healthy.
