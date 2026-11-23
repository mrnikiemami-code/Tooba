# TASK — TB-DEV-SEED-STALE-CATALOG-VARIANT-288

Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.3.0
Mode: ROOT-CAUSE → BOUNDED FIX → CONNECTED VERIFY
Repository: mrnikiemami-code/Tooba
Context: Task 287 BLOCKED due to Development bootstrap fixture crash; NOT a new Offer implementation.
Base: fetch latest origin/main and independently check worker branch `origin/opencode/iso-agents-002` at `ae7d7dbba265a8b58dd32c169f7af0f42f15ef4c`; preserve all prior work.
Worker: OpenCode / tooba-worker-01
Final: STOP_FOR_ARCHITECT; no auto-next task and no Stage 3.

## One goal
Determine why, after a Development Catalog demo reset, Promotion's `MerchandisingCampaignDevelopmentSeed` reuses an Offer referring to a missing Catalog Variant and crashes the Host via `InventoryDirectory.OpenPositionAsync` with `inventory.catalog_variant.missing`. Correct ONLY the proven development-fixture ownership defect, and demonstrate a stable, repeatable Development bootstrap. Do not modify Offer 287 fingerprint semantics, Pricing/Inventory domain policies, public APIs, Host module ownership/architecture, or production data. Task 287 stays BLOCKED until separate architect closure.

## Source and proof required
Read relevant current source and commits, specifically:
- `src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Development/MerchandisingCampaignDevelopmentSeed.cs` (`EnsureOosOfferAsync` and `EnsureZeroStockAsync`)
- `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Adapters/OfferDevelopmentSeedGateway.cs` (`EnsureActiveCloneFromAnyActiveAsync`, currently returns existing by SellerSku without variant check)
- Catalog demo reset-and-seed endpoint/implementation and any variant-ID regeneration/deletion
- `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.cs` (guard `CatalogVariantMissing`)
- 287 `RESULT.md` and `evidence/fixture-crash-normal-boot*.err.log` on worker branch
- `docs/opencode/DEV-HOST-TEST-POLICY.md`, `docs/opencode/ARCHITECT-DELIVERY-METHOD.md`, current `.opencode/iso-rules` 01–06 and `.cursor/skills/tooba-backend-feature/SKILL.md`.

**Required root-cause decision before edit:** Trace actual Offer → CatalogVariantId and Catalog reset effects. Demonstrate whether the existing OOS Offer points to a now-deleted/invalid variant (and why it is reused), or identify an alternative evidenced cause. A stack trace by itself is insufficient to claim which Catalog operation invalidated the row. Record reproducible before/reset/after SQL/contract observations and owning source conditional branches in `ROOT-CAUSE.md`. If unable to prove cause safely, BLOCKED, no speculative changes.

## Bounded implementation
After root cause is proved, implement the smallest module-owned, Development-only repair preserving data ownership and invariants. For example, a stale OOS Offer must not silently be considered valid merely because its SellerSku matches. Decide safe remediation (recover/rebuild only marked dev seed data or use a stable published variant) against actual foreign keys and Offer/Promotion contracts. Do not bypass Inventory's missing-variant guard, disable Development seeding wholesale, fake success, mass reset shared data, or use cross-domain direct table writes in product code. No schema/public-contract/AMSC changes. If necessary fix crosses ownership/architecture boundaries, stop for architect decision.

Use strict OpenSpec for behavior change with REQ traceability BEFORE code. Impeccable: `NOT_APPLICABLE: NO_UI_CHANGE`. AMSC is NOT required or allowed for routine seed repair.

## Test design: finite, nonlooping
Use the architect-issued immutable `ACCEPTANCE-MATRIX.json` before changes and test it exactly with concrete real-provider oracles:
- S01 negative control: reproduce invalid Offer–Variant relation from the real sequence; trace exact root.
- S02 recovery: repeat normal Development bootstrap after the reset safely, no crash, relevant Offer/Variant linkage valid.
- S03 idempotence: second bootstrap/seed run does not create duplicate seller/Offer/variant/Promotion membership and has stable correct links.
- S04 Inventory fail-closed: production Inventory guard remains effective on invalid variants; no unauthorized Inventory/Pricing writes.
- S05 roles: existing dev seller/admin principals survive or are recovered consistently without privilege bypass or user-role mutations beyond task-owned fixture.
- S06 cleanup: precise task-owned rows only; no shared dev data touched, no Host 5088 or frontend 3000 interruption, no raw Catalog mass-reset, no new per-test Host/DB loop.
- S07 scope and evidence: source→observed JSON provenance, honest NOT_RUN for unavailable proof, OpenSpec strict proof, backend build and focused test results.

**Test environment rule:** prefer existing DEV Host and DB ONLY if its running code matches candidate and test is non-destructive. The reset-and-seed reproduction is destructive by nature; never run it on shared dev DB. Use a SINGLE existing explicitly authorized isolated fixture (or reuse a safe previously provisioned one) for this destructive negative control, at most one setup, one bounded repair, one final full run, one teardown. If no safe fixture exists, STOP `BLOCKED_PREFLIGHT` before writes. Do not create DB or restart Host repeatedly. Avoid forced stopping by process-name; touch only task-owned process and data.

## Exit/deliverables
Store `ROOT-CAUSE.md`, `ACCEPTANCE-MATRIX.json`, `ARCHITECTURE-PLAN.md`, `RESULT.md`, `RECEIPTS.md`, exact script/log/SQL/proof hashes under this task directory. Actual elapsed time and token/cost only if measured, else NOT_AVAILABLE. Final worker message ≤8 lines and `STOP_FOR_ARCHITECT`.
PASS/READY_FOR_ARCHITECT_REVIEW only with all S01–S07 independently proven; otherwise BLOCKED with one exact cause. NO retry loops, NO new tasks, NO Stage 3, NO rewrite or rerun of 287 within this task.
