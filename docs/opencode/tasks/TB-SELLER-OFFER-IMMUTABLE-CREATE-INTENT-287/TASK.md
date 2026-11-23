# TASK — TB-SELLER-OFFER-IMMUTABLE-CREATE-INTENT-287

Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.3.0
Repository: mrnikiemami-code/Tooba
Parent: TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286 (V4 FAIL)
Roadmap: Seller Offer Stage 2 only; Stage 3 FORBIDDEN until architect ACCEPT
Expected base: origin/main at issuance 1085116c34892f692d14b16050632db02df4592f; worker MUST fetch and report actual base/code SHA and verify ancestry; never discard local work
Channel: tooba-main | Worker: tooba-worker-01 | Agent: opencode
Mode: IMPLEMENT + VERIFY + ONE BOUNDED REPAIR; no repeated architect repair tasks
Stop: STOP_FOR_ARCHITECT | automaticNextImplementationTask: NONE

## Source-grounded architect decision
The architect reviewed current `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/CreateOffer/CreateOfferCommand.cs`, `OfferStore.cs`, `SellerOfferConfiguration.cs`, and 286 RESULT/RECEIPTS on main.
`CreateOfferHandler.MatchesCreationIntent` presently compares replay to mutable SellerOffer SKU/status/return-policy/quantity fields; after legitimate edit V4 observed original A→409 and changed B→201. This is an integrity defect.
**Approved contract:** a requestId identifies the **immutable original normalized creation intent** independently from subsequent Offer edits. An identical original replay must resolve the same OfferId without reverting edits; a changed intent using that same identity MUST fail with `offer.create.intent_conflict` (409), even when changed terms match current Offer.
This decision authorizes the *smallest Offer-owned durable mechanism* for NEW idempotent creates, including a narrowly scoped additive Offer persistence field/table and migration source IF current schema genuinely lacks durable storage. No live/canonical migration execution, seed, backfill, permissions change, cross-module schema or public API change is authorized. For historical rows without a recorded fingerprint, NEVER infer original intent from mutable state or silently accept it; fail closed with explicit existing appropriate error and report exact compatibility boundary. If a correct design cannot fit this authority, STOP with a concrete architectural option; do not invent workaround.

## Required workflow, not ceremonial
1. Read `docs/opencode/ARCHITECT-DELIVERY-METHOD.md`, `docs/architecture/TOOBA-DELIVERY-ASSURANCE.md`, Offer architecture locks and current ownership, this immutable TASK and `ACCEPTANCE-MATRIX.json`, 286 RESULT and 285 final architect review. Read short loader and task packs 01–06, 05 and 08 applicability. Do NOT automatically run AMSC: this is bounded Offer behavior/persistence, not Host evacuation or recertification. Use existing backend feature skill. OpenSpec strict proposal/design/spec/tasks and mapping required **before code**; Impeccable = NOT_APPLICABLE because NO UI change (do not fake usage).
2. Preflight current worktrees/branches, actual source SHA, active worker, main remote; isolate authorized scope. Do not interrupt other sessions. Build a minimal plan showing HTTP → auth → MediatR → Offer store → isolated PG → independent reread. Inspect current migrations and retry behavior before selecting mechanism; do not assume one.
3. Reproduce V4 negative control on source-before code using existing 286 focused harness or equivalent minimal adapted harness, preserving raw expected failure. Then implement only Offer-owned durable normalized intent with a stable deterministic canonicalization/version and atomic persistence with Offer creation. Original same-key replay returns the **current** Offer read model (not stale initial data) while checking immutable fingerprint; changed intent returns 409 without writes. Verify concurrency/race branch `PersistNewAsync` as well as pre-existing lookup branch.
4. Include all meaningful creation fields in intent: seller, catalog variant, channel, normalized seller SKU, initial status, return policy/window, min/max quantity and requestId identity as applicable. Preserve equivalent null/default semantics and numerical/string normalization based on actual existing contracts; no accidental hash equality across meaningful changes. Store/retrieve durable fingerprint safely; do not leak it to public API or logs. Preserve existing deterministic ID semantics and owner/tenant boundaries.
5. Execute the architect-issued complete atomic `ACCEPTANCE-MATRIX.json` against real owning Host and disposable PostgreSQL; independent SQL/GET checks and exact Pricing/Inventory before/after write measurements. Must prove V4 after a legitimate SKU edit AND a separate mutable field edit, original-vs-changed replay, concurrency, seller/admin and no unauthorized effects. Negative authorization/cross-seller and legacy no-fingerprint policy must be checked. Use focused tests during development. One complete matrix, at most ONE consolidated fix for failures, one final full rerun. No unlimited loops, no new tasks.
6. Source→evidence provenance: committed script generates observed JSON, verifiable actual values, checker rejects missing test / flipped observed result / tampered evidence. Preserve 285/286 raw artifacts unchanged; do not manually embellish JSON. One clean shutdown/DB teardown. Verify canonical 5088/3000 unaffected. Record actual wall clock and measured tokens/cost if available, otherwise NOT_AVAILABLE.
7. Keep all application changes inside Offer owning module + directly necessary Offer-owned migration/test files; no FE, Host, Cursor, root AGENTS, roadmap, Pricing/Inventory product code, user data, unrelated DB migration. No live migrations applied. Any unforeseen external schema/public-contract/AMSC decision => BLOCKED_ARCHITECT_DECISION, no self-issued follow-up.

## Mandatory REQs and exit
REQ-01 immutable fingerprint and correct replay after mutable Offer edit: T01–T05.
REQ-02 race/ownership/side-effect safety: T06–T10.
REQ-03 legacy/null-fingerprint and canonicalization policy: T11–T13.
REQ-04 evidence, negative checker and scope/cleanup: T14–T16.
All test IDs MUST correspond exactly to `ACCEPTANCE-MATRIX.json`; no omitted normative clause. Not-run is NOT_RUN, not PASS. Do not waive failed proofs.
Deliver under this task directory: `ARCHITECTURE-PLAN.md`, OpenSpec references, `REQUIREMENTS.json`, `VERIFY.mjs`, focused harness/evidence/log/hashes, `RESULT.md`, `RECEIPTS.md`.
Worker outcome only READY_FOR_ARCHITECT_REVIEW or BLOCKED, never ARCHITECT_ACCEPT. 8-line max final chat; complete evidence in files. Stage 2 remains BLOCKED until independent architect source+evidence review, then Stage 3 may be issued separately.
STOP_FOR_ARCHITECT
