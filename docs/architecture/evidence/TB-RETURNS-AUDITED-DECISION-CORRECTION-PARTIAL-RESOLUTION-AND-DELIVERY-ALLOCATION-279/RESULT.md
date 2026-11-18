# TB-279 — Result

Task: `TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: cursor
Bridge row: `60f75a8f-52c9-4aa0-bc87-86c6dbaeaced`
Expected-Base-SHA: `57cccbbe7cffea23c30ee0f474291cb3dda7bc58` (verified code base)
Actual claim HEAD: `6463fdac85a5db66c6d454d4eac5b5e5f89db21b` (the docs-only issuance commit; its parent is the
expected code base, so the incoming delta was exactly the authorized issuance docs)
Mode: `FINAL_CONSOLIDATED_RETURNS_DECISION_PHYSICAL_FINANCIAL_AND_CUSTOMER_COMPLETION`

## Status

**PASS within the issued consolidated scope**, decision:

```
ONE_APPEND_ONLY_EFFECTIVE_DECISION_AUTHORITY_WITH_VERSION_FENCED_REOPEN_A_REVIEWED_PER_ITEM_RESOLUTION_THAT_FREEZES_ONLY_THE_ACCEPTED_MONEY_DURABLE_PER_PORTION_CUSTODY_DELIVERY_SLICE_ALLOCATION_AND_A_RETURNS_MANAGED_SHARED_PAYMENT_BUDGET_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ONE_ADDITIVE_MIGRATION_APPLIED_AFTER_A_VERIFIED_FRESH_BACKUP_AND_ZERO_LIVE_BUSINESS_WRITE
```

NOT Architect ACCEPT, NOT user functional/visual acceptance, NOT a Marketplace/provider/financial readiness claim.
277 / 277-R1 / 277-R2 and 278 work is retained; 278's durability gaps are closed here (no 278-R1 was created).
Single worker, one task, no delegation, no historical subagent resumed, no screenshots, no automatic next task.

| Area | State | What this means |
| --- | --- | --- |
| A decision authority | one append-only authority | `returns.return_request_decision_revisions` appends every later decision and every correction (version, decision, reviewed reason, actor, timestamp, occurrence key); the legacy `return_request_decisions` row stays the immutable version-0 baseline. `ReadDecisionStateAsync` is the single read used by `MapSnapshotAsync`, the work/capability projection and `ApproveAsync`/`RejectAsync`/`StartFirstRefundAsync`. `ExpectedDecisionVersion` fences a stale pre-correction review (409 `return.correction.version_stale`); an exact occurrence replays its recorded revision; a changed reason is `return.correction.occurrence_conflict` |
| A reopen | explicit, blocked by real effects | `POST /v1/admin/returns/{id}/reopen` refuses courier origin (`correction.source_unsupported`), no effective decision (`correction.not_applicable`), a non-decided status (`correction.state_invalid`), ANY refund attempt — pending/failed/unknown/succeeded (`correction.financial_effect_recorded`) — any custody fact (`correction.custody_recorded`), any item disposition (`correction.physical_effect_recorded`) and any settlement/closure fact (`correction.source_effect_recorded`). A rejection re-acquires its own released capacity under the seller-order serialization, and a sibling claim that already took it is `correction.capacity_conflict` with zero rows |
| B partial resolution | accepted quantities only | `POST /v1/admin/returns/{id}/resolution` publishes one reviewed plan version (one row per requested item, accepted+rejected == requested, accepted <= received, reason required). `ReturnResolutionPolicy` derives `approvedAmount` from accepted × the line's STORED unit price; approval freezes it once (`return_requests.approved_amount`) before the first attempt; the original claim (`refund_amount`) stays visible and immutable; all-rejected means zero owed, not "refunded" |
| C custody | durable per-portion physical facts | `POST /v1/admin/returns/{id}/custody` records the ACTUAL handback of rejected RECEIVED goods (item, quantity, condition, server-derived recipient, proof reference, actor, occurrence key); over-handback and a meaningless proof are typed refusals; settlement requires every accepted portion's own physical fact AND every owed handback — the settled read of a legacy case never claims a rejection was "refunded" |
| D allocation | one per-delivery capacity authority | every new claim binds to owning delivered slices (`returns.return_delivery_allocations`); a rejection releases its rows, a reopen re-acquires them; availability is computed once, per slice, from durable allocations + conservatively discounted legacy cases; an expired slice never funds a new claim |
| E budget | Returns-managed shared payment budget | first/retry attempts serialize on a stable per-payment advisory key and count Pending+Succeeded obligations of ALL Returns cases for that payment; the reservation is committed with the attempt BEFORE the external call; a definite failure releases only its own reservation; the same attempt/key answers a duplicate |
| F migration | one additive migration, twice verified | `20261117164135_Tb279AuditedDecisionCorrectionResolutionCustodyAndAllocation` + `20261118004500_NormalizeApprovedAmountColumn`; each applied ONCE by `Tooba.MigrationRunner` after a fresh, verified private `pg_dump` (see `migration-and-backup-proof.md`); post-apply `pendingCount=0` |
| G proof | owning, focused | Returns `172/172` (new directory suite `11/11`, new policy suite `13/13`); AMSC `34/34`; `dotnet build Tooba.slnx -c Release` 0 errors; Host publish 0 errors; FE submission `25/25`, mounted workspace/request suites `17/17`, `tsc` 0, `next build` 0 |
| served | refreshed once | Host `5088` PID `33892`, dll SHA256 `7EE36BFF34B5F87B851C47199D5DA631D16EA710659A49209A21CD0C880766B4`, `/health` 200, `/health/ready` 200 (SingleStore + messaging Healthy, `store-alpha`); Next `3000` PID `5076`, `BUILD_ID 8fLqIGZTLx0CUcUquOuUG`, chunk `7944-0ba65eeffc7022bc.js` (200, 200072 B) serves `reopen_for_review`, `publish_resolution`, `record_custody` and «بازگردانی» |
| data | untouched | zero migration reruns, DDL, seed, backfill, reset, cleanup or restore; zero live business write (no receipt, decision, revision, resolution, custody, allocation, refund, stock, hold, provider, PSP, Wallet, waybill or notification call); all three stored cases remain `Completed` with their original `updated_at`; the four new tables hold 0 rows |

## In-task corrections (found by the new owning proof, fixed, re-verified)

The consolidated task explicitly allowed "fix actual failures within this task". Three real defects were found and
closed, and the facts that caught them remain in the suite:

1. **Reopen could never re-acquire capacity.** With no allocation rows, `EnsureDeliveryAllocationsAsync` re-read the
   eligibility that already counts the case's OWN quantity, so every legacy approval/rejection correction refused with
   `correction.capacity_conflict`. `IReturnEligibilityEvaluator.EvaluateAsync` now takes an optional
   `excludeReturnRequestId`, and the correction paths (re-acquire + rejection re-check) pass the case itself, so only
   OTHER cases compete. Proven by `A_rejected_case_re_acquires_its_own_released_capacity` (1 allocation, 0 provider
   calls) and `A_rejected_capacity_that_a_sibling_case_already_took_is_a_typed_conflict` (typed conflict, 0 rows).
2. **A reopened case was undecidable.** `ReturnWorkPolicy` gated `CanDecide` on `RecordedDecision is null`, but the
   baseline row must survive a correction, so a reopened case published `category=decision` with every action false.
   The gate now reads the EFFECTIVE decision. Proven by `A_reopen_appends_one_reviewed_revision_...`
   (`CanDecide=true`) and `A_post_correction_decision_binds_the_current_revision_and_a_stale_one_cannot`.
3. **`approved_amount` was created PascalCase** by the first migration while EF maps snake_case, so the live detail read
   failed with `42703`. Additive migration `20261118004500_NormalizeApprovedAmountColumn` renames it (with the same
   fresh-backup protocol). Proven live: case detail is `200` again and every new table is empty.

## Known limits (explicit, not papered over)

- Money and stock are proven with controlled ports (scripted gateway/Wallet/Inventory) — no real PSP, Wallet or
  Inventory effect; the only live HTTP receipts are GET/typed-refusal probes.
- No committed two-writer race and no stress harness. Serialization is proven structurally (advisory key stability,
  row-lock ordering, version CAS) plus the directory facts; the reopen-vs-first-start interleaving is not executed with
  two real connections.
- The two authorized user cases (`01a1e6b0…`, `01a1e9c5…`) and the 276 review case remain read-only settled history;
  the settled case's correction is blocked live (`correction.state_invalid`), so no served reopen/resolution/custody
  walkthrough exists. The served proof is the typed refusal, the served chunk and the mounted/DOM behaviour.
- The mounted FE suite is jsdom: behaviour, not browser geometry.
- `Unknown` unit-precision read blocking vs integer-only write remains the disclosed 278 limitation.
- Card refunds outside this observed Returns authority are not represented by the shared budget; no "universal PSP
  refundable balance" claim is made. No shipping/tax/coupon allocation rule was invented.
- The 276-era `admin-return-workspace-276.mount.test.ts` still fails against the current fixture shape (pre-existing
  since 277) and is excluded from this selection; it is not a 279 regression and was not silently rewritten.

## Declared deviations

- `TASK.md` in this directory is the exact issued byte set (18537 bytes, identical to
  `docs/ai/tasks/…-279/TASK.md`); `issuance-receipt.json` is preserved untouched.
- The Host was republished and restarted three times today (including one failed start whose content root was the repo
  root, corrected by launching from `artifacts/publish-primary`); the final served identity is the one recorded above.
- The relational/`SkippableFact` harness was not extended: the new directory facts use in-memory EF like 275/277, so the
  `FOR UPDATE`/advisory-lock branch is exercised only by the running Host and the migration, not by a committed test.
- All probe scripts are untracked scratch files under `D:\temp` (never committed).

## Evidence index

`RESULT.md` (this) · `RECEIPTS.md` · `result-contract.txt` · `decision-and-partial-resolution-proof.md` ·
`delivery-and-budget-proof.md` · `customer-retention-proof.md` · `migration-and-backup-proof.md` · `user-review.md` ·
`TASK.md` (issued bytes) · `issuance-receipt.json` (preserved)
