# OpenCode-only implementation task — TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.0.0
Task-ID: TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285
Parent-Task: TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 26f9a7a4d24d009f25b66b4ea589df884c2a1aa1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: opencode
Model: metisdeepseek/deepseek-v4-flash
Mode: IMPLEMENT_AND_CONNECTED_VERIFY
Roadmap-Stage: 2 of 5 — shared admin/seller offer creation and resumability
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## Architect-issued authority and no-diversion scope

Read `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` first. This is **Stage 2 ONLY** from `docs/planning/offer/TOOBA-SELLER-OFFER-ARCHITECTURE-AUDIT-AND-ROADMAP-2026-11-22.md`, following architect-accepted Stage 1 (`docs/opencode/ARCHITECT-ACCEPTANCE-283-284.md`). One task with internal waves, NOT an open-ended discovery request. Build actual admin-managed and seller-managed Offer creation and continuation, server-side scalable picker/list and correct permissions.

Do not change any Cursor artifacts (`docs/ai/delivery/**`, `docs/ai/tasks/**`, `docs/architecture/TOOBA-DELIVERY-ASSURANCE.md`), `AGENTS.md`, the roadmap, Bridge protocol or the canonical OpenCode architect method. Do not edit Stage-1 historical evidence. No Stage 3 shipping-service work, promotion discounts, order/cart/payment/settlement flows, global UI redesign or new unrelated frameworks. No live commercial data writes, resets or unapproved migrations.

## Source-grounded architectural facts (reconfirm against exact origin/main)

- Seller UI create: `src/frontend/app/vendor-panel/products/new/page.tsx` currently creates Offer, writes Pricing then Inventory independently, with re-create risk after partial failure; do not collapse their domain ownership or change Catalog variant/SKU to Seller Offer SKU.
- Seller list: `src/frontend/app/vendor-panel/products/page.tsx` presently receives an entire seller list and uses `executeGridQuery` in-browser; `ListSellerOffersQuery` and `OfferStore.ListBySellerAsync` currently materialize unbounded offers.
- Catalog picker: `SellerCatalogVariantDirectory.ListPublishedVariantsAsync` takes newest 100 published PRODUCTS before returning variants, excluding older published products.
- Seller endpoints: `OfferSellerEndpoints.cs` GET/POST/PATCH and independent Pricing/Inventory routes; current seller authorizer uses seller-scope view relationship and must NOT be assumed sufficient for all write permissions.
- Admin: `src/frontend/app/admin/admin-shell.tsx` has products and sellers navigation but no explicit shared admin Offer create flow; existing admin seller directory `src/frontend/features/admin-sellers/api/sellers-api.ts` offers `/v1/admin/sellers/query`. Reuse admin shell and proper server-side admin actor permissions. Do NOT fake an admin by injecting seller headers.
- Offer domain identity: `SellerPartyId + CatalogVariantId + Channel` has active/nonarchived uniqueness. `CreateOfferCommand` creates one Offer and enriches detail from other owners. A durable retry must keep one OfferId despite partial Pricing/Inventory failure and network uncertainty.
- Languages: obtain enabled locales from existing admin language registry (`GET /v1/admin/languages` when applicable); keep fa/en, RTL/LTR and design-system patterns. Keep Stage-1 StockItemId safety.

## Mandatory architect requirements — exactly the same set as REQUIREMENTS.json

### REQ-01 — Admin-created offer is visible to chosen seller

- **GIVEN:** Authorized admin selects seller A and a published Catalog variant from the existing admin navigation.
- **WHEN:** Admin creates one draft Offer and independently reopens it.
- **THEN:** Offer.SellerPartyId=A, single stable OfferId, seller A list/detail shows the same OfferId; seller B does not; no Catalog mutation.
- **PROOF:** Connected Chromium Admin→Host→PostgreSQL→independent seller Chromium/GET+SQL.

### REQ-02 — Role and owner authorization on every command

- **GIVEN:** Admin edit rights, admin view-only rights, sellers A and B have distinct authorized identities.
- **WHEN:** Each reads/creates/edits offer and directly invokes owning APIs including price/inventory.
- **THEN:** Only authorized roles and owner scope succeed; B cannot read/change A; view-only cannot write price/inventory/create; denied requests make zero persisted changes.
- **PROOF:** Real Host authenticated/authorized negative API + SQL and browser.

### REQ-03 — Create can resume without duplicate Offer

- **GIVEN:** A multi-step Create has written Offer but Pricing or Inventory failed, or response was lost.
- **WHEN:** Same operator reconnects, retries or resumes the saved intent.
- **THEN:** Reconciliation with authoritative GET/owned listing returns original OfferId, follows remaining incomplete steps; never blindly posts second Create or duplicates price/inventory effects; Draft is not falsely ready.
- **PROOF:** Connected browser→Host→isolated PostgreSQL; injected controlled error branch only with real first write.

### REQ-04 — Published Catalog picker supports server search and paging beyond 100

- **GIVEN:** At least 150 published products plus multiple variants and at least one older item beyond newest 100.
- **WHEN:** Admin or seller searches exact old product/variant and pages through the picker.
- **THEN:** Correct result appears from server, not from loaded first 100; stable pages, total/count/hasMore grounded; selection maintained across paging/search/errors; non-published excluded.
- **PROOF:** Real Catalog query/provider PostgreSQL + Chromium, request paging assertions.

### REQ-05 — Offer list uses real server paging and search for both roles

- **GIVEN:** At least 200 seller offers with distinct search keys and multiple sellers.
- **WHEN:** Seller and authorized admin search, sort and paginate.
- **THEN:** Server applies owner-scoped filtering/query, stable deterministic order and bounded pages; totals accurate, no full-array fetch or client-only filtering; empty/error/loading distinct.
- **PROOF:** Real Host PostgreSQL indexed pagination / request trace + browser.

### REQ-06 — Safe cancel, malformed result, failed continuation and status

- **GIVEN:** Draft create or existing resumable workflow has unsaved edits or partial progress.
- **WHEN:** Cancel, disconnect, malformed/wrong seller response, permission refusal and retry occur.
- **THEN:** Cancel before first write yields zero business writes; no false success; partial progress is explicitly disclosed and recoverable via same OfferId; status stays Draft until appropriate owning prerequisites; no accidental duplication.
- **PROOF:** Connected success and controlled error branches with independent GET+SQL.

### REQ-07 — Usable Persian FA/EN role experiences, real Impeccable critique

- **GIVEN:** Admin and seller forms are loaded at 360x640 and desktop in fa and en.
- **WHEN:** Operator selects seller, searches picker, fills Draft, saves/resumes, navigates to list/detail.
- **THEN:** Existing shells/design system, proper RTL/LTR, registry languages not hard-coded authority, clear words for non-engineers, accessible 44px primary controls, no horizontal overflow, mobile keyboard/reopen and error feedback; documented Impeccable before/after critique.
- **PROOF:** Impeccable critique log + served Chromium geometry/interactions.

### REQ-08 — No domain ownership or roadmap drift and deterministic delivery

- **GIVEN:** Catalog, Offer, Pricing, Inventory, Party and AccessControl remain owning domains.
- **WHEN:** Implementation and acceptance complete.
- **THEN:** No unsafe cross-domain write or schema migration without approved contract; no Stage3 shipping or discounts; strict OpenSpec and task-specific OpenCode verifier with negative self-tests; code/build hashes, cleanup, all REQs independently reviewed.
- **PROOF:** Diff/contract audit, strict spec, reproducible verifier, connected evidence and rollback.

## Fixed acceptance tests (names and invariants)

- `admin_creates_for_A_seller_reads_same_id`
- `B_cannot_read_or_change_A`
- `view_only_cannot_change_price_or_create`
- `lost_create_resumes_once`
- `find_variant_beyond_100`
- `list_search_pagination_is_server_owned`
- `cancel_has_zero_writes_and_partial_is_honest`
- `fa_en_mobile_admin_seller_360x640_accessible`
- `stage1_inventory_and_price_safety_retained`

Each named test MUST assert actual identity, HTTP methods/counts, data before/after, and independent read. Tests that are genuinely not applicable must have a preapproved explanation; no silent N/A on mandatory assertions. Test B's direct API attack in addition to UI hiding. A server paging "API" must actually perform bounded DB query, not load all rows and slice in code; prove search finds items beyond first 100 and list beyond 200.

## Mandatory FIRST ACTION — preflight before product code

1. `git fetch origin`; verify repository, branch, base ancestry, clean/saved user modifications and `origin/main`. Read THIS task and task-local `REQUIREMENTS.json` from `origin/main`, along with canonical OpenCode method, Stage-2 roadmap and affected source.
2. Establish a disposable connected test stack **before** production edits: Playwright installed Chromium → served Next with admin and seller route → real Host with owning Catalog/Offer/Pricing/Inventory/Party/AccessControl → isolated PostgreSQL. Confirm authorized fixture admin, view-only admin, sellers A/B, 150+ published products/variants, 200+ offers, safe rollback/disposal, independent GET/SQL, and no live data. Record ports/build/process/database/fixtures. May build harness and create disposable DB as authorized test infrastructure; no production seed/migration.
3. If no safe complete chain, stop as `BLOCKED_PREFLIGHT` with one precise reason and smallest unblock **before** product changes. Health 200, UI mocked responses or isolated unit tests alone do not suffice.
4. OpenSpec: create a Stage-2 change named `seller-offer-dual-role-creation-285` with proposal/design/spec/tasks, actual REQ-01..08 Given/When/Then; `openspec validate --strict` before code and preserve evidence. The contract must explicitly decide admin authorization, idempotent/resumable Create, server paging/filters and what an incomplete Draft means; no ambiguous architecture left to worker convenience.
5. Impeccable v4.10.0: **use it**, not only read installation. Record critique of BOTH admin and seller flows at desktop/mobile with concrete before screenshots forbidden unless authorized; use DOM/interaction observations, documented design decisions, actual implementation and post-evaluation. Avoid new shell and technical labels. UI is in scope so NOT_APPLICABLE is forbidden.
6. Save `ARCHITECTURE-PLAN.md` with routes/owners/contracts, DB indexes/paging strategy, entry→write→GET map, negative cases and ordered waves; commit plan BEFORE production code.

## Implementation constraints and fixed internal waves

W1: server owner-scoped and role-scoped contracts; admin cannot impersonate seller; authorization decisions checked server-side for create, price and inventory; negative roles and cross-seller refusal.

W2: safe **Draft-first** create or equivalent truthful lifecycle. Once Offer Create succeeds, persist/recover stable OfferId across partial Pricing/Inventory failure and lost response, never blind second Create. Choose and document deterministic idempotency/reconciliation mechanism in Offer-owned boundary; verify replay/concurrent conflict and unique index. Resume remaining steps from authoritative state. No fabricated frontend-only key that disappears on refresh.

W3: server-side Published Catalog variant search & pagination and Offer admin/seller list search & pagination, with bounded DB queries, stable sorting, truthful total/hasMore and explicit owner scopes. Must handle 150+ products / 200+ offers; index only if genuinely needed and authorized isolated schema change, but no live migration. If a migration is unavoidable in production, STOP for architect approval before applying.

W4: reusable interaction patterns within **existing AdminShell/VendorShell**; suitable admin seller selection (reuse real seller query), seller self scope, understandable draft/resume step UX, fa/en and mobile 360×640, loading/error/empty and dirty/cancel handling. Impeccable critique and post-check receipts are mandatory.

W5: complete connected Chromium–Next–Host–PostgreSQL acceptance for ALL 8 REQs, separate admin A and seller A/B actor contexts, no mock success. Controlled responses allowed for error injections only, marked. Independent GET/SQL, negative tests and no unchanged aggregate corruption. Run affected builds, strict OpenSpec, task-local `VERIFY-OPEN-CODE.mjs` with negative self-tests, evidence hashes, cleanup. One consolidated repair after FIRST complete acceptance matrix, one final full rerun; no unlimited iterations. If still FAIL or missing proof, `BLOCKED / NOT_READY`, no automatic next task.

## Machine-readable artifacts and delivery

Within `docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285/`: `TASK.md` immutable after issue, `REQUIREMENTS.json` (architect-owned immutable), `ARCHITECTURE-PLAN.md` (before code), `EVIDENCE-MANIFEST.json`, `VERIFY-OPEN-CODE.mjs` and its tamper/missing-REQ/changed-expected-value negative controls, `RESULT.md`, `RECEIPTS.md`, tool/connected logs, exact hashes. Do not make a checker that passes because it trusts a top-level 'PASS' label.

Bridge response must state each REQ separately, source/plan/evidence commits, Next build and Host hash, Postgres provider, real HTTP and SQL changes, dev repairs, nonblocking backlog, cleaning done, diff boundaries, origin parity and true readiness. Only all 8 PASS plus negative verifier PASS can claim `PASS / READY_FOR_ARCHITECT_REVIEW`. Worker cannot issue Architect ACCEPT or start Stage 3. Always STOP_FOR_ARCHITECT.

END_TOOBA_TASK
