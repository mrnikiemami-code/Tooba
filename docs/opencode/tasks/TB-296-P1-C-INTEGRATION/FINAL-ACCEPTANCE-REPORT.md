# TB-296 Phase 1 — FINAL ACCEPTANCE REPORT

Status: **BLOCKED** — not READY_FOR_ARCHITECT final acceptance, not self-authorized.
Mode: VERIFICATION ONLY. Product code changes: **0**. Branch: `opencode/iso-agents-002`.
Required HEAD: `8b6f54595e0efe519f500c41cffdb11172d2fceb` (product source identical to tested `84f3372a…`; the diff between them touches only `docs/opencode/tasks/TB-296-P1-C-INTEGRATION`).

## Environment and provenance
- Served Host: `http://127.0.0.1:5088`, binary `.tmp-p1c-final2-host/Tooba.Host.exe`, DLL SHA-256 `5669d7b08199b4cc8b39d886d496399441cacb7b3d19da251569460a9a767976` (unchanged across all runs).
- Frontend: `http://127.0.0.1:3000`, Next BUILD_ID `hMU29Ej5cfQxp1-pC9yp6`.
- Database: `tooba_alpha` (PostgreSQL 17, container `postgres-db`, connection reference `tenant-alpha`).
- Restorable backup: `C:\Users\User\AppData\Local\Temp\opencode\p1c-final-20261127173245.dump`, SHA-256 `589f8c2a0cb525dca1238deee8c05e81bfaf71cab8a19b0862f4b5af74e68166` (pg_restore --list exit 0).
- Test-only configuration changes (two-tenant channel contexts; Marketplace-on-tooba_alpha) were reverted; the running config SHA-256 is `396ab642647b0dd0210a31e854e26847a26823816669aeb46d65ec889bfce421`, byte-identical to the pre-test snapshot. `src/backend/Host/Tooba.Host/appsettings.Development.json` is unchanged.
- Database after all tests: offers 3323, offers_history 265, allocations 0, P1C-owned offers/carts/prices 0. Price rows moved 5223→5231 only via normal Host-startup demo re-quotes on pre-existing demo offers (timestamps coincide with the two controlled Host restarts); no test fixture row remains.

## G1 — Channel and Marketplace (2/2 previously NOT_RUN now PASS)
### R10-01-B — PASS (new executable evidence)
Two independently authorized DEV seller contexts on the **same** Host and **same** `tooba_alpha` DB:
- store-alpha (SalesChannel `Marketplace`, host `127.0.0.1`) and a reversible test-only store-beta (SalesChannel `Direct`, host `beta.localtest.me`, same `tenant-alpha` connection).
- Same seller+variant produced two distinct Draft offers (channels persisted exactly), each read back through the owner endpoint.
- A changed-channel request replaying the same creation intent (same requestId ⇒ same deterministic offer id) was rejected with `offer.create.intent_conflict` (4xx) and wrote no offer/history/outbox.
- Evidence: `finalacceptance-evidence/R10-01-B.json` (SHA-256 `a0f14c24…`). Config restored; store-beta now returns 404.

### OWN-10 — PASS (new executable evidence)
- Live **Marketplace** edition bound to the **same** `tooba_alpha` DB (Marketplace.ConnectionReference=tenant-alpha), real seller browser + HTTP + SQL.
- Existing platform-fulfilled demo offer: seller capability `canEditPhysicalInventory=false`, `canEditOfferedQuantity=true`; on `/fa/vendor-panel/products/<id>` at 360/768/1440 the shell is `rtl/fa-IR`, seven tabs, no horizontal overflow, offered-quantity input editable, no physical-reason control.
- `/en/` = ltr/en, `/ru/` = ltr/ru-RU; in-app language switch preserved an unsaved offered-quantity draft; captured seller API requests carried the authorized `X-Tooba-Seller-Party-Id` and `X-Tooba-Dev-Actor-User-Id`; independent SQL confirms no write.
- Invalid/disabled locale canonical handling is **reused** from R10-09-N/B (not re-executed).
- Evidence: `finalacceptance-evidence/OWN-10.json` (SHA-256 `069327ec…`). Edition/config restored to SingleStore.

## G2 — Transaction and concurrency integrity
### OWN-08 — PASS (strengthened)
Two simultaneous owner-revision races against the real Host:
- price race (same price revision, amounts 6000 / 7000): exactly one 200 winner (6000), one 409 `concurrency.conflict`; winner persisted; two durable price facts total (base + winner), no loser fact; price outbox event(s) published.
- physical-stock race (same position revision, onHand 90 / 80): exactly one 200 winner (80), one 409 `inventory.position.revision_conflict`; winner persisted; two durable inventory facts total, no loser fact; inventory outbox event(s) published.
- price / physical / allocation revisions are independent; no negative availability.
- Evidence: `finalacceptance-evidence/OWN-08.json` (SHA-256 `459fa6d7…`).

### D2-ORDER, D2-HOLD, D2-PARTIAL, D2-RECOVERY — NOT_RUN (blocked)
The compound A05 clauses require a real order-creation / captured-checkout workflow. Exact blocker:
- `POST /v1/storefront/checkout` on a real guest cart returned **401 `checkout.authentication_required`**; the Host `CheckoutIdentityGate` enforces the Catalog `AuthenticatedOnly` policy and does not honor `X-Tooba-Dev-Actor-User-Id` for checkout.
- The existing full Order relational suite (`StorefrontFullOwnerVerticalTests` etc.) requires a separate task-owned PostgreSQL on **port 15433 / database `order_tb231`**, which `OrderEvidenceRelationalTestTarget` explicitly rejects as not `tooba_alpha`.
- Both a new/alternate database and new customer-auth/fixture scope are forbidden by this verification-only task. Prior real owner-contract/provider observations are retained (see the prior evidence paths in the ledger) but are **not** accepted as full PASS.
- Architect decision required: authorize an in-scope authenticated checkout path on `tooba_alpha`, or explicitly accept the retained partial owner-contract evidence.

## G3 — Remaining R10-13-P regression clauses — PASS (supplement, prior clauses reused)
Newly executed only the previously unproven original-spec clauses:
- campaigns: real `/v1/admin/merchandising-campaigns/` returned 11 complete rows (identity, title, lifecycle status, member count).
- Stage2 cart: real guest cart create + add line + independent read-back honored offer identity, quantity 1, and tax-exclusive quote; exact SQL cleanup.
- grid sort: clicking the status header drove the real AG Grid sort model (`aria-sort` none→ascending→descending).
- export: CSV export downloaded 11 real rows with the expected localized headers.
- saved views: a named view persisted and rendered; the original `grid.admin.offers` preference was restored byte-exactly.
- Prior performance/orders/products/paging/filter are **reused** unchanged from the earlier R10-13-P evidence.
- Evidence: `finalacceptance-evidence/R10-13-P.json` (SHA-256 `5a60b06f…`).

## G4 — Architecture acceptance reconciliation (eight AQ gates)
| Gate | Result | Basis |
|---|---|---|
| AQ-01 Localization registry | PASS (Offer scope) | R10-09-P/N/B + OWN-10 fa/rtl, en/ltr, ru/ltr, in-app switch, draft preserved; invalid/disabled canonicalization reused. Legacy shared-shell FA/EN resource fallbacks remain (documented, unchanged). |
| AQ-02 Domain ownership / independence | PASS, with D2 caveat | Offer.Application references foreign Contracts only; price via Pricing gateway; allocation/stock via Inventory owner contracts; OWN-09 real EF commit + owner-history fault passed. D2 full order workflow remains NOT_RUN (see G2). |
| AQ-03 Shared admin/seller workspace | PASS | Shared `OfferWorkspace` used by both admin and seller; OWN-10 exercised the seller surface on the same component. |
| AQ-04 CQRS | PASS | Price/offer/allocation commands via MediatR; R10-08-N/B typed 400/policy exact; Offer backend architecture guards 51/51. |
| AQ-05 Shared design system | PASS | Shared AppDataGrid/MessageBox/Toast reused; G3 exercised export/saved-view/sort on the shared grid. |
| AQ-06 Module & dependency boundaries | PASS (baseline debt excluded) | Frontend architecture: 27 pre-existing source-size violations, **0 Offer-owned/new**; FE-FOLDER failures are pre-existing baseline. |
| AQ-07 Source size | PASS (in-scope) | `OfferWorkspace` below 800; the 27 oversized files are outside Offer and excluded from remediation (not marked resolved). |
| AQ-08 Cross-domain Contracts-only | PASS | Offer.Application → foreign Contracts only; adjacent Pricing/Inventory prior builds/tests green; Host DLL identity unchanged. |

## G5 — Final acceptance gate
- Immutable ID set: exactly **60** unique IDs (order from `FINAL-LEDGER.json`); no missing/duplicate.
- Ledger: **PASS 56 / FAIL 0 / NOT_RUN 4**. NOT_RUN: `D2-ORDER`, `D2-HOLD`, `D2-PARTIAL`, `D2-RECOVERY`.
- Gate self-tests: `acceptance-gate.cjs --self-test` PASS (positive + 11 negative controls, exit 0); `architect-review-gate.cjs --self-test` PASS (positive + 14 negative controls, exit 0).
- Full 60-ID acceptance-gate run: **NOT_RUN** — a PLAN/RUN cannot honestly be bound while 4 IDs are NOT_RUN, and the gate rejects non-executed/NOT_RUN tests. No expected/observed facts were fabricated.
- Architect review gate: **NOT_RUN** — requires an architect-authored `ARCHITECT-REVIEW.json` bound to base/candidate/evidence; a worker must not author it.
- Strict OpenSpec (`offer-production-readiness-296-r10 --strict`): exit **0** (only long-requirement warnings).
- New evidence hashes and outgoing Git blobs are recorded; no credentials, dumps or oversized logs are staged.

## Definition of Done assessment
| Requirement | Result |
|---|---|
| 60 PASS | NOT MET (56) |
| 0 FAIL | MET |
| 0 NOT_RUN | NOT MET (4) |
| 0 unresolved acceptance assertions | NOT MET (D2 full workflow) |
| 0 new architecture violations | MET (0 Offer-owned/new) |
| 0 product code changes | MET |
| Actual successful final acceptance receipt | NOT MET (not issued; architect-owned) |

## Conclusion
G1 is now fully closed (R10-01-B and OWN-10 PASS on fresh executed evidence), G3 is closed, OWN-08 is strengthened, and architecture is reconciled with zero new/Offer-owned violations. Final readiness is **not** achieved: four immutable D2 IDs remain NOT_RUN for their full order-workflow assertions, and a genuine architect acceptance receipt does not exist. Do not start Phase 2. Do not self-authorize acceptance.

## Evidence index (SHA-256)
See `FINAL-ACCEPTANCE-SHA256SUMS.txt`. Per-ID ledger with evidence paths and hashes: `FINAL-ACCEPTANCE-LEDGER.json`. Gate executions and blocker detail: `FINAL-ACCEPTANCE-RECEIPT.json`. Identity/restoration: `finalacceptance-evidence/environment-identity.json`.
