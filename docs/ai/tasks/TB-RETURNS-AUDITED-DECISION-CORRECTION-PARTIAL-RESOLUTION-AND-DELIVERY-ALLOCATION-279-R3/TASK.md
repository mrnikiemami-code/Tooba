PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3
Parent-Task: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: FINAL_NARROW_B1_B2_CLOSURE_AND_OWNING_EFFECT_PROOF
Track: RETURNS_EXISTING_SCOPE_CLOSURE
Expected-Base-SHA: dd5c1d31fcd65bce7164c115599aacb5a4e33083
Reviewed-Code-SHA: f867dcb7163608915112c7450196c49bc8a3ba37
automaticNextImplementationTask: NONE

## 0. Fixed scope and pace

The user explicitly says: "در یک تسک تمومش کن کلی توسعه داریم".
Complete ONLY the two still-open source-confirmed owner defects B1/B2, the missing controlled money/Inventory proof, and the one already-recorded safe source-membership cleanup. ONE worker/task/result. No new roadmap, module rewrite, fresh UI redesign, speculative audit, per-helper tasks, delegation, screenshots or polling loop.
Keep the repaired R2 operator contracts, Dialog/template toast, condition portions/unit gates, correct 2.5 occupancy, legacy slice ambiguity, customer unmount/v1/exact-origin retention, shared payment reservation and atomic decision transition.
Do not suppress a real failure to claim final completion: fix directly related failures exposed by the named scenarios in this SAME scope. Do not reopen unrelated completed features.

Mandatory targeted reads:
- AGENTS.md CURRENT and applicable repository governance;
- docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2/ARCHITECT-REVIEW.md, especially B1/B2 and proof boundary;
- docs/planning/returns/RETURNS-279-CONSOLIDATED-CLOSURE-CHECKLIST.md;
- repository backend feature SKILL.md before backend changes; frontend feature SKILL.md ONLY if a directly affected test/transport requires a frontend change.
Read the relevant owners, not the entire historical Returns task archive. No architecture-findings-register audit.

The exact full envelope is already persisted in docs/ai/tasks/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3/TASK.md and docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3/TASK.md, with issuance-receipt.json byte/SHA256 pins. Use these identical repo bytes. Empty/markdown-normalized Bridge content never authorizes reconstructing or overwriting the canonical envelope/receipt. Keep a transport variant privately and report it separately.

Fresh fetch and safe tree; record ACTUAL claim HEAD. Expected base exists/is ancestor; the following docs-only issuance commit is permitted only after verifying zero incoming source/config/schema delta.
Normal explicit-path commits and push ONLY. NO rebase/amend/force/force-with-lease/reset/stash/history purge or discarded work. On divergence/rejected push STOP for safe reconciliation. No git add . / -A; inspect staged paths for secrets/scratch before every commit.

## Architecture decision

REQUIRE_A_REVIEWED_DECISION_VERSION_FOR_EVERY_NEW_DECISION_AFTER_EXACT_REPLAY_AND_PRESERVE_UNALLOCATED_OR_INCOHERENT_HELD_GOODS_AS_OCCUPIED_OR_TYPED_BLOCKED_WITH_DIRECT_DIRECTORY_MONEY_AND_STOCK_ARGUMENT_PROOF

Returns owns the facts and writes; foreign module access is through owning Contracts ports, no foreign SQL/FKs. One pure receiving/resolution/custody authority, no parallel money/stock logic. Approval stays decision-only; new first refund, same Pending resume and reviewed Failed child retry stay distinct. Exact replay/identity and no external effect under locks remain unchanged.

## A. B1 — refuse a missing decision version for EVERY NEW approve/reject

Source: ReturnDirectory.Corrections.cs ResolveExistingDecision currently checks only:
if (expectedVersion is { } expected && expected != state.EffectiveVersion).
NULL skips the check and permits a NEW decision. The resolution-version gate does not fix this; it returns early for a complete-receipt implicit baseline.

1. Preserve genuine stored decision replay FIRST (same immutable reviewed identity; zero NEW effect).
2. For a NEW decision over undecided/reopened state, missing ExpectedDecisionVersion OR a different current version refuses before any durable decision/status/amount/allocation change. Reuse existing return.correction.version_stale for this unmatched/missing NEW review, avoiding unnecessary new route/catalog work.
3. Compare under the existing owning case lock. Never synthesize the current decision version for an old caller; the modal's frozen revision must travel through its adapter. A caller that does not review a version gets the typed refusal.
4. Keep current reviewed resolution-version check and atomic decision + case/frozen amount/capacity transaction.
5. Do not add this mutable-version refusal to a frontend proven-no-effect clearing set: a later refusal cannot erase a possibly-sent earlier command.

Required owner facts:
A1. Approved -> audited reopen -> complete receipt but NO explicit partial plan -> NEW approve WITHOUT decision version refuses; no baseline/revision/status/amount/allocation write.
A2. Same for NEW reject without the decision version.
A3. Wrong reviewed version refuses; exact current version succeeds; a genuine stored replay remains zero-effect.
A4. One reachable seller/Order-adapter shape carries the frozen version or refuses its absence. Existing reviewed-plan mismatch proof stays.
Use the real ReturnDirectory; state assertions, not code-string/source-regex assertions. Update only directly affected fixtures/helpers to supply the snapshot version they actually reviewed; never weaken the invariant or silently choose current/latest inside production adapters.

## B. B2 — preserve rejected legacy/incoherent held goods

Source-confirmed:
- ReturnEligibilityEvaluator legacy fallback reads item quantities only from active-status cases.
- A Rejected/Cancelled legacy case with received goods and NO allocation rows therefore vanishes from both allocations and fallback.
- ReleaseRejectedDeliveriesAsync uses held=0 on incoherent receipt.
- TrimActiveAllocationsToHeldAsync silently returns when legacy attribution cannot be proved.

1. Read outstanding received/custody obligations for rejected/cancelled cases even when no allocation rows exist. Goods not positively handed back must remain occupied or make the relevant line typed BLOCKED until attribution is provable.
2. Use existing stored receiving/resolution/custody authority per own item/condition, not requested quantity inferred as received and not a status-only shortcut. Unknown/incoherent physical evidence is NEVER proof of zero held goods.
3. If exact legacy delivery origin cannot be positively proved, expose the existing typed ambiguity/evidence blocker and refuse a new capacity claim that depends on the missing attribution. Do not guess oldest/newest, manufacture allocations/backfill, or repair historical rows.
4. A release on incoherent/unknown receiving refuses or leaves a conservative explicit blocked claim; it must not mark allocations released as a successful zero-held outcome. Failure to allocate cannot silently disappear.
5. Preserve accepted consumption + TOTAL outstanding rejected-received custody across both conditions. Accepted 2, rejected-received 1, handback 0.5 => occupied 2.5 / free at most 0.5. Handback of the full rejected 1 leaves accepted 2 consumed.
6. Actual evidenced full handback may release ONLY that rejected portion; never accepted/financially consumed units. No cross-item/unit sum or new claimant sharing held goods.

Required owner/evaluator facts:
B1. ONE delivered unit; legacy ordinary Rejected case received one, no custody, NO allocations => another claim cannot take that held unit.
B2. Same with ambiguous expired+live delivery origins => typed blocked, not guessed free.
B3. Requested case with incoherent receiving and an existing allocation: attempted reject/release writes no falsely released row/status/decision and never creates new free capacity. Assert rollback/caller tracking behavior as applicable.
B4. Positive actual handback releases its own rejected quantity; retain existing exact-slice/quantum and 2.5/0.5 mixed cases.
No live case/fixture/backfill is needed or permitted. Controlled in-memory facts may seed task-owned invalid historical shapes to prove fail-closed behavior; never seed the canonical store.

## C. Complete the TWO missing effectful owner proofs

Prior CountingGateway/CountingInventory THROW-if-called facts are valid negative proofs; they cannot prove a successful refund/actual Inventory argument. Keep them and add recorder/scripted ports for these positive scenarios through the ACTUAL Directory:

C1. Actual partial accepted restock:
- requested/received 3 usable, reviewed accepted 2 / rejected received 1, valid owning stock/payment binding, recorded decision-only approval;
- invoke separate FIRST refund using the real shared reservation/outcome/settlement Directory path with a scripted successful gateway;
- assert exactly ONE durable first attempt, expected stored amount/key/reference, actual Inventory port receives quantity EXACTLY 2 (never 3), and only that accepted portion's completion persists;
- rejected received 1 remains owed custody; money success cannot claim all physical work settled;
- replay does not create another attempt, gateway effect or restock; test the real retained identity, not a hand-built policy snapshot.
C2. Cross-case budget on NEW failed-child retry:
- payment total100; A's positively bound failure60; another Returns case B now has Pending/Succeeded obligation60 on that SAME payment;
- invoke real RetryRefundAsync with exact failed id + reviewed decision version; typed budget refusal, NO child/reservation/status change, gateway/Wallet/Inventory zero;
- independent lawful control (B reserved40, A60) creates exactly ONE deterministic child through the same Directory, and duplicate reviewed intent never mints another child;
- missing/foreign/currency payment authority refuses with zero attempt/effect. Reuse valid owning facts and existing scripted ports; do not change production policy to fit a stub.
No real PSP/Wallet/Inventory/money effect. No new committed concurrency harness. The existing real PostgreSQL rollback-only lock/atomic-approval proof retains its limited scope; a sum query is not an executed refund-reservation proof.

## D. One tiny safe cleanup, no key operation

The old file docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/RESULT.bridge.txt is still tracked, contrary to the R2 report.
Privately preserve/verify its local copy, then remove from index ONLY with a normal commit and one exact scoped ignore. Preserve the original historical RESULT.md/evidence. Do not delete the user's working copy or re-stage scratch/key material.

The exposed key is already out of current Git. Keep Security-Exposure-State=CONTAINED_PENDING_REVIEWED_RETIREMENT. No key rotation/revocation/deletion, cookie invalidation, encryption-setting change, source-history purge or secret output is authorized in this task. A different active ring id alone does not prove historical shared-store carrier ciphertext was unaffected. Preserve the existing review/retirement procedure honestly; this security decision does not authorize broad new Returns development or hold independent functional repairs hostage.

## E. Focused verification and exact exit criteria

Acceptance scope is FIXED to A/B/C/D plus their direct regressions and protected state. No exploration of new cosmetic/architecture issues to grow another task chain. Log an unrelated non-blocking observation only in this result; do not start work on it.

Before final validation, make a compact matrix A1–A4/B1–B4/C1–C2/D -> named actual test/source/assertions. Fix any directly exposed real failure within this envelope. Do not return PASS while one of these concrete counterexamples remains.

Run after the final relevant code change:
- changed owner behavior classes and necessary directly affected retained Returns classes ONLY;
- affected real seller/Order adapter/route contract fact for A4, existing focused lock facts only if coordination changed;
- focused existing frontend transport/mount test ONLY if its production/fixture code was changed;
- backend Release solution build and Host publish; frontend tsc/build only if frontend production source changed.
NO whole Returns/Host project run, whole AMSC/vitest sweep, stress test, repeated green-suite loops or blanket "172/177 tests passed" substitution. No test assertion weakening/suppression, global tracker clear or regex-only proof.
No new error code is expected; reuse typed faults. If a small necessary contract/error registration change is truly unavoidable, update only affected exact sets and focused pins truthfully, not the broad architecture filter.
Record every failed final-check attempt with its actual fix; do not hide reruns or fabricate elapsed time.

PASS requires:
- all new unversioned decisions refused after replay, current succeeds, stale refuses with zero writes;
- unallocated/incoherent held goods cannot release/fund a second claim; valid handbacks/mixed acceptance remain correct;
- C1 asserts the ACTUAL Inventory argument and durable outcomes; C2 executes the ACTUAL failed-child reservation Directory branch;
- historical posting safely untracked; key/current user work preserved;
- no unresolved critical regression in directly touched paths.
Provider readiness/user acceptance/security retirement are separate declared limits, not required live effects or a reason to invent another implementation task.

## F. Canonical environment and protected state

ONE canonical tooba_alpha @127.0.0.1:5432 SingleStore/store-alpha, Host5088, Next3000. ZERO migration/DDL/Down/reapply/seed/backfill/reset/cleanup/restore, second database/Docker15432/15433, settings/edition/tenant/port/connection/key/CapturedPreview change.
Existing three Completed user cases and their original updated_at/money/receipts/attempts/history remain read-only. No new demo case/order/hold/receipt, no live POST (even refused probe), no provider/PSP/Wallet/Inventory/waybill/notification operation.
Any relational task-owned proof is rollback-only with canonical-target validation and separate committedRows=0; no committed fixtures/delete cleanup.
Refresh only changed Host/Next gracefully using current config and correct publish content root. Verify loaded DLL hash, BUILD_ID if FE changed, health/ready/platform and GET-only existing history. No screenshot or active live walkthrough claim. Give the user accurate review URLs and state when no active case exists; do not reset/create one to manufacture the walkthrough.

## G. Delivery, bounded completion and recovery

Persist exact TASK.md/issuance-receipt.json unchanged; add concise RESULT.md, RECEIPTS.md, closure-matrix.md, owner-proof.md, user-review.md and result-contract.txt under docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3/.
Do NOT produce multiple large duplicate proof documents or overwrite old task/result/acceptance records.
Update compact AGENTS CURRENT, recovery/START-HERE and Returns checklist with precise state. Close only the issued remaining items when evidenced; preserve accepted R2 improvements and historical partial records.
No automatic new task/roadmap. Architect's next review is limited to these issued acceptance gates, their actual affected-path regressions, truthful evidence and protected state. Non-blocking polish must not delay this closure.

Canonical Result:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3
Parent-Task: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Expected-Base-SHA: dd5c1d31fcd65bce7164c115599aacb5a4e33083
Base-SHA: <ACTUAL claimed HEAD; permitted docs-only delta stated>
Code-SHA: <sha>
Evidence-SHA: <sha>
Final-HEAD: <sha>
HEAD-Equals-Origin-Main: <fresh fetch>
B1-New-Decision-Version: <actual missing/current/stale/replay proof>
B2-Held-Capacity: <legacy/corrupt/handback/mixed proof>
C1-Inventory-Argument: <actual 2 rather than 3, durable effect/replay proof>
C2-Retry-Budget: <real Directory refuses 60+60 over100; lawful control/identity>
Historical-Posting-Cleanup: <private preserve/index-only/ignore>
Security-Exposure-State: CONTAINED_PENDING_REVIEWED_RETIREMENT
Validation-State: <focused actual commands/facts/failures>
Schema-State: ZERO
Live-Business-Write-State: ZERO
Provider-Money-Stock-Effect-State: ZERO
User-Work-Preserved: YES
Git-Rewrite-State: NONE
Architect-Accept-State: NO
User-Acceptance-State: NO
Evidence-Limits: <controlled ports, no committed race/active live walkthrough, security retirement pending>
Recovery-Next-Task: NONE
Next-Recommended-Task: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT
END_TOOBA_WORKER_RESULT

Commit/push normally, fetch and verify parity. Post ONLY complete actual result to Bridge, never a placeholder /api/results probe. Complete the claimed lifecycle, return IDLE and STOP entirely.
STOP_FOR_ARCHITECT
END_TOOBA_TASK

