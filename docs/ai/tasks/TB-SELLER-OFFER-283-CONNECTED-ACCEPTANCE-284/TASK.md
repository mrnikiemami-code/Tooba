PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284
Parent-Task: TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: opencode
Model: metisdeepseek/deepseek-v4-flash
Title: Close seller-offer Stage-1 connected acceptance, then conditionally propose delivery governance
Mode: BOUNDED_CONNECTED_ACCEPTANCE
Expected-Base-SHA: f67d6ecd6cb222a2a17d39d6e2d6af391218e4ad
Issued-Date: 2026-11-22 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# 0. Authority — no roadmap diversion
This is one closure task for existing 283, NOT Stage 2. Do not rewrite the five-stage Seller Offer roadmap, change product rules, refactor unrelated code, or loosen acceptance. Original task 283's REQ-01..REQ-07 and its existing OpenSpec remain authoritative. The actual open gates are REQ-01 connected browser→owning Host→PostgreSQL and REQ-03 real concurrent Pricing/PostgreSQL. Preserve provenance of prior 283 tests; controlled HTTP cannot become connected evidence by relabeling. Only architect can ACCEPT. Worker PASS is not architect acceptance.

# 1. First: real git and NO-WRITE infrastructure preflight
From actual Tooba checkout: git fetch origin; verify exact remote, main, local HEAD, origin/main, ancestry, changes and user work before touching files. Read this task from origin/main, the original 283 TASK/PLAN/DELIVERY/RESULT, AGENTS.md, docs/architecture/TOOBA-DELIVERY-ASSURANCE.md, docs/product/OPERATOR-UX-ACCEPTANCE.md, Seller Offer roadmap, OpenSpec 283, owning frontend/backend/Pricing/Offer/Inventory code. Reconcile advanced main; never force push, overwrite user files or weaken guards.

Before production edits, actually demonstrate a safe test chain: installed Playwright Chromium, served Next page, exact Host process/build and endpoint, legitimate test seller authorization, isolated PostgreSQL with Offer, Inventory and Pricing owners, fixture and transactional read/write visibility, independent SQL/GET verification, safe rollback or disposable DB cleanup. Record command/port/provider/fixture source and proof, not intentions. A Host /health=200 alone does not establish connectivity. Prove that the connected browser can write only disposable data and that the database survives unchanged afterward. If this setup is not safely possible, stop BEFORE changing production with BLOCKED_PREFLIGHT, a single concrete cause, and smallest safe unblock. Do not silently fall back to mocked HTTP.

# 2. Plan gate / tools
Before production changes create ARCHITECTURE-PLAN.md + DELIVERY.json in this task folder, mapping fixed REQ-01 and REQ-03 to executable connected assertions, exact provider and teardown. Commit plan separately before code. Run the existing docs/ai/delivery/check-readiness.mjs plan gate with the issued TASK.md byte-exact hash and record its stdout, exit code, and plan SHA. OpenSpec v2.1.1: validate existing 283 change in strict mode; only make a new delta if an actual contract change is required and validate it before code. Impeccable v4.10.0: for an actual UI fix, use a documented critique→design decision→implementation→post-check workflow; merely installing/reading guidance does not count as design usage. Playwright real Chromium is mandatory. No new generic framework if a minimal isolated harness works.

# 3. Required primary acceptance: CONNECTED-01 (original REQ-01)
On REAL served seller edit UI, through REAL owning Host and isolated REAL PostgreSQL, sign in as fixture seller and open one fixture offer with named positions OnHand=3,7; nonzero reservations; known price and SKU. Capture SQL before-state and stock adjustment count. Change only SKU using the actual UI and Save; capture exactly one PATCH to owning Offer endpoint, zero Pricing writes, zero Inventory writes, and exact offer/seller IDs. Independent GET and SQL after-state must show new SKU, unchanged price, positions 3,7, reservations and adjustment count; not optimistic React state or separate unrelated test fixtures. Verify rollback/disposable-database cleanup, no persistent business mutations.

# 4. Required primary acceptance: CONNECTED-03 (original REQ-03)
Two independent authorized browser contexts/sessions open SAME fixture offer through real Host and isolated PostgreSQL, with B loading the OLD SKU/price before A's write. Session A saves new price through real owning Pricing command. Independently GET/SQL verify new price. Session B then changes SKU only on its stale served form. Record B has zero Pricing writes; independently GET/SQL verify both new SKU and new price survive together and stock/reservation remain unchanged. The tests must have real Pricing persistence, not controlled fake responses. Record precise order, fixture IDs and before/after SQL. Verify cleanup.

# 5. Preserve existing REQs / classify failures
Recheck directly affected parts of REQ-02/04/05/06/07, including no-change zero writes, explicit limit clear, Draft no-op, target StockItemId and refusal of multi-position ambiguity, response identity/effect, cancellation, mobile 360x640, pending edit preservation. Follow existing 283 spec without inventing new mandatory REQs. A false success for a changed field or loss of an edited unsaved section is an in-scope blocking defect if demonstrated, not a new product feature. Do not widen task to stage-2 create/admin, shipping, discounts, catalog picker, broad UI redesign or architecture rewrite. Other unrelated findings enter BACKLOG and do not block this closure. Do not change canonical ownership of Catalog/Offer/Pricing/Inventory.

# 6. Controlled implementation and stopping
Complete developer debug and targeted retest *inside this task*, rather than shipping predictable PARTIAL. After the FIRST full acceptance matrix, at most ONE consolidated bounded correction pass and ONE final full acceptance rerun. Do not count setup fixes before the first complete acceptance run as the architect repair round, but keep a concise progress log and avoid infinite local attempts. If still unsuccessful, Status BLOCKED/PARTIAL, Readiness NOT_READY, precise failing requirement and evidence, STOP; no auto continuation, no self-authorized new task. Do not relax original criteria, replace real backend with mocks, or mark mandatory proof N/A. No live data writes, seed/reset/grant/migration/checkout/payment/provider. Tests use reversible isolation; disclose any inability to isolate before writing.

# 7. Deliverable and architect decision
Write ARCHITECTURE-PLAN.md, DELIVERY.json, RESULT.md, RECEIPTS.md and supporting real logs under this task; link original 283 evidence without rewriting historical claims. For each scenario record actual command, test name/assertion, source SHA, served Next build and Host identity, database identity, exact HTTP counts, SQL state and rollback, evidence SHA256 and PASS/FAIL/NOT_RUN. Rerun affected tests, TypeScript checks, frontend/backend build, strict OpenSpec validation, review gate (including gate exit code). Push reviewed commits on main only if ancestry verified; no force. Report code/evidence/plan SHAs, git parity, remaining blockers and truthful BRIDGE-WAKE-V1 envelope. READY_FOR_ARCHITECT_REVIEW requires every mandatory criterion PASS and structural review gate PASS. STOP_FOR_ARCHITECT always.

# 8. CRITICAL CONDITIONAL GOVERNANCE — not before success
The new architect delivery method must NOT become permanent policy just because worker claims PASS. In THIS task, after both connected proofs and review gate PASS, prepare a proposed *candidate* file at docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/PROPOSED-ARCHITECT-DELIVERY-METHOD.md. Mark CANDIDATE / AWAITING_ARCHITECT_ACCEPT; do not update canonical governance, AGENTS.md, pipeline or roadmap yet. Candidate must precisely define:
(A) architect-owned input contract: source-grounded scope, fixed REQ, user-visible result, domain ownership, risk and expected base;
(B) mandatory preflight for real test chain BEFORE coding, including safe fixture/rollback and explicit BLOCKED_PREFLIGHT;
(C) locked OpenSpec design and strict validation BEFORE code; actual Impeccable critique workflow when UI changes; actual Playwright + owning backend/provider as appropriate;
(D) internal developer debugging vs at most one consolidated post-acceptance repair; termination as PASS/READY or BLOCKED, never cosmetic PARTIAL;
(E) evidence level, required independent read/SQL checks, falsification / negative controls, precise logs and code/served identity;
(F) worker PASS ≠ architect ACCEPT ≠ owner acceptance; independent architect review and fixed STOP_FOR_ARCHITECT;
(G) never broaden scope or invent follow-up work; backlog non-blocking;
(H) deterministic checklist/decision table and concrete lessons from 283.

Only AFTER the user/architect explicitly confirms the successful result of this task should a separate *governance-only* approval action promote that candidate into docs/architecture/TOOBA-DELIVERY-ASSURANCE.md (additive revision with version/date, no weakening of existing controls), add a pointer in AGENTS.md, and record acceptance/provenance in an ARCHITECT-REVIEW.md. That promotion must not change feature code or roadmap and must not happen automatically. If proofs/gate fail, no candidate is considered adopted; no canonical governance change permitted. This makes the condition unambiguous.

END_TOOBA_TASK
