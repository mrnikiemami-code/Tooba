# TB-279-R3 — RESULT (worker, PASS within the issued fixed scope)

Date: 2026-11-18 (Asia/Tehran) · Protocol: `BRIDGE-WAKE-V1` · Channel: `tooba-main` · Worker: `tooba-worker-01`

- Expected-Base-SHA `dd5c1d31fcd65bce7164c115599aacb5a4e33083` (verified ancestor).
- Actual claim HEAD `e7db94398e663b604a536b82bd86670bb50f72db` (= `origin/main`, the permitted docs-only issuance
  commit `docs(returns): issue TB-279-R3 fixed-scope final closure`).
- Reviewed implementation `f867dcb7163608915112c7450196c49bc8a3ba37`.
- Decision:
  `REQUIRE_A_REVIEWED_DECISION_VERSION_FOR_EVERY_NEW_DECISION_AFTER_EXACT_REPLAY_AND_PRESERVE_UNALLOCATED_OR_INCOHERENT_HELD_GOODS_AS_OCCUPIED_OR_TYPED_BLOCKED_WITH_DIRECT_DIRECTORY_MONEY_AND_STOCK_ARGUMENT_PROOF`

This is **PASS inside the issued B1/B2/C/D/E scope**. It is NOT Architect ACCEPT, NOT user functional/visual acceptance,
NOT Marketplace/provider/financial readiness, and NOT retirement of the historical key exposure. Parent 279 / 279-R1 /
279-R2 / 278 remain PARTIAL. One worker, one task, no delegation, no screenshots, no loop, no automatic next task.

## A. B1 — a NEW decision always names the revision it reviewed

`ReturnDirectory.Corrections.cs` `ResolveExistingDecision`:

1. an exact recorded replay of the effective decision is still answered FIRST with zero new effect;
2. for a NEW approve/reject over undecided/reopened state a **missing** `ExpectedDecisionVersion` now refuses with the
   existing `return.correction.version_stale` exactly like a stale one — before any decision/revision/status/amount/
   allocation row;
3. the comparison stays inside the existing owning case lock; no version is ever synthesized from a fresh read;
4. the reviewed resolution-version gate and the atomic decision + status + frozen amount + capacity transaction are
   unchanged.

`ReturnAdminOperationsAdapter` (the reachable Order/admin shape) no longer back-fills a missing revision from a fresh
read: the reviewed values travel exactly as supplied, so an unreviewed caller is refused by the owner.

## B. B2 — rejected/cancelled held goods cannot disappear

- New `ReturnHeldGoodsPolicy` (Application, pure) is the ONE answer to "what does this case still physically hold?",
  derived per item/condition from stored receiving + reviewed resolution + custody facts — never from status and never
  from requested quantity read as received. Incoherent receiving is never "nothing held".
- `ReturnEligibilityEvaluator.ReadOccupancyAsync` now reads outstanding held obligations of REJECTED/CANCELLED cases even
  when they own **no** allocation rows, and adds them to the line's unallocated legacy occupancy. `ReturnAllocationPolicy`
  then either subtracts them from a single provable slice or flags every slice `LegacyAmbiguous` and refuses a new claim
  (typed blocked) instead of guessing an origin.
- `ReturnDirectory.Allocations.cs` `DescribeRejectedHeldAsync` computes the held map BEFORE the rejection is recorded and
  refuses an incoherent receipt with `return.recovery.evidence_conflict` (zero rows). `ReleaseRejectedDeliveriesAsync`
  consumes that same map, so a release can never read a different "held" than the decision was reviewed over, and a
  failed allocation cannot silently return.
- Mixed acceptance is preserved: accepted 2 + owed rejected-received 1 − handback 0.5 keeps **2.5** occupied / 0.5 free.

## C. C1/C2 — the two missing effectful owner proofs (controlled ports)

- **C1** `ReturnsFinalClosure279R3Tests.C1_the_real_first_refund_restocks_exactly_the_accepted_portion_and_replays_with_zero_effect`:
  through the ACTUAL `ReturnDirectory` first-refund path with a scripted successful gateway, the ONLY durable attempt
  carries `2 × unit price` and the case-derived identity/key, the recording Inventory port receives **exactly `2`**
  (never `3`) for the owning reservation, `UsableRestockedQuantity = 2`, rejected-received `1` stays owed custody and the
  recovery phase is `PendingDisposition`; replay adds no attempt, no gateway call and no restock.
- **C2** `C2_the_real_failed_child_retry_refuses_a_cross_case_budget_overrun_and_mints_nothing` (payment 100, A's bound
  failure 60, sibling Pending 60 on the SAME payment ⇒ `return.refund.budget_exceeded`, one attempt, zero gateway/
  Inventory/status change), `C2_an_independent_lawful_control_mints_exactly_one_deterministic_child_and_replays_it`
  (sibling 40 ⇒ exactly one deterministic child, one gateway call of 60, one accepted-usable restock of 1, duplicate
  reviewed intent mints nothing), and `C2_a_missing_payment_authority_refuses_the_retry_with_zero_attempt_and_effect`
  (`refund.payment_missing`).

## D. Historical posting (index only)

`docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/RESULT.bridge.txt`
was removed from the Git index with `git rm --cached` in a normal commit. The local working copy stayed on disk
(`Test-Path` = True), one exact scoped ignore
(`docs/architecture/evidence/**/RESULT.bridge.txt`) now matches it, and the historical `RESULT.md`/evidence of 277-R1 is
untouched. No key material was read, printed, rotated, revoked, deleted or history-rewritten;
`Security-Exposure-State = CONTAINED_PENDING_REVIEWED_RETIREMENT` is unchanged and its ordered retirement procedure stays
an explicitly separate decision.

## E. Focused proof actually run

| Command | Result |
| --- | --- |
| `dotnet test … --filter FullyQualifiedName~ReturnsDecisionCorrection279DirectoryTests` | **21/21** |
| `dotnet test … --filter FullyQualifiedName~ReturnsFinalClosure279R3Tests` | **4/4** |
| `dotnet test … --filter FullyQualifiedName~ReturnsModuleAmsc001` (Host) | **34/34** |
| `dotnet build src/backend/Tooba.slnx -c Release` | **0 errors** (642 pre-existing warnings, NU1900 offline feed) |
| `dotnet publish src/backend/Host/Tooba.Host -c Release -o artifacts/publish-primary` | **0 errors** |

Declared deviation: the whole `Tooba.Returns.Tests` project was also executed **once** (`187/187`, 0 failed) beyond the
issued focused selection, and the server-side refresh required stopping/restarting the Host. No assertion was weakened,
no test was deleted, no global tracker clear was used and no source-regex assertion replaced an owner fact.

## F. Served state (GET-only)

Host `5088` PID `39776` loading `artifacts/publish-primary/Tooba.Host.dll` SHA256
`04CD98A718E4BCB9702AF12F54BA4872E6B11FEB6956604AF9A65E4055ED34C5`:
`/health` 200, `/health/ready` 200 (`SingleStore`, messaging `Healthy`), `/__platform-commerce` → `store-alpha`.
Next `3000` PID `9904` (unchanged — no frontend production change), `/fa/admin/returns` 200. Review URL
`http://127.0.0.1:3000/fa/admin/returns`.

Existing user history read GET-only with the dev actor: `01a1e6b0-f33a-7000-80ee-beffb6344490` and
`01a1e9c5-c729-7000-b614-79daf3d0e812` both `200`, `Completed`, one attempt each, `updated_at` unchanged
(`2026-11-17T00:31:26…Z` / `2026-11-17T14:03:06…Z`), `approvedAmount` absent. There is no active case, so no served
approve/reject/refund walkthrough is claimed and none was manufactured.

## G. Schema / data / effects

ZERO migration/DDL/seed/backfill/reset/cleanup/restore. ZERO live business write and no live POST at all. ZERO real
PSP/Wallet/Inventory/waybill/notification operation. Existing cases, receipts, attempts and history remain read-only.

## Known limits

Money/Inventory facts are controlled-port (scripted success) proofs, not a real PSP/Wallet/Inventory effect; the
cross-case budget refusal runs over in-memory EF with the owning Directory (the existing real-PostgreSQL rollback-only
lock/budget/atomic-approval facts keep their stated narrow scope). No committed two-writer race harness was added.
`IsUnprovable` on the read path is surfaced as the conservative claim + `LegacyAmbiguous`/`evidence_conflict` refusal
rather than a new UI copy string. Key retirement is contained in Git only and remains pending review.

STOP_FOR_ARCHITECT; `automaticNextImplementationTask: NONE`.
