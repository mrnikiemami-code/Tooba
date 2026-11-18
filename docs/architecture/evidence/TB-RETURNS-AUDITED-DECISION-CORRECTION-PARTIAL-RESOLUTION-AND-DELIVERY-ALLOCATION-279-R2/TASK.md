PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2
Parent-Task: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: CONNECTED_CONTRACT_CAPACITY_AND_RETAINED_INTENT_CLOSURE_WITH_SAFE_REPOSITORY_CONTAINMENT
Track: RETURNS_CONSOLIDATED_REPAIR
Expected-Base-SHA: 6e3d1873f7f9926c2906ae4e26547a6c9ef97eeb
Reviewed-Code-SHA: 3f8b670cd4a16db9ef8e1c4766704b53a075d364
automaticNextImplementationTask: NONE

## 0. Purpose, exact authority and stop rules

ONE task closes the concrete remaining 279-R1 source-review findings, not a new Returns redesign or per-helper task chain. The user asks "خوب چیکار کنیم؟" after the reviewed defects; this envelope makes the next repair concrete.
Keep the useful shared first/retry reservation, outside-lock payment read, atomic decision scope, accepted-usable-only actual Inventory argument, durable portion fields, shared Dialog and template react-toastify already delivered. Correct their connected gaps below, prove the exact counterexamples and stop.

Read:
- root AGENTS.md CURRENT and applicable governance;
- docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1/ARCHITECT-REVIEW.md (R1-A–F plus repository incident);
- docs/planning/returns/RETURNS-279-CONSOLIDATED-CLOSURE-CHECKLIST.md;
- repository backend/frontend feature SKILL.md before changing the corresponding code;
- the exact 279-R1 envelope only for inherited domain/protected-state details, without re-running its historical task.

Full canonical envelope is committed before execution at docs/ai/tasks/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2/TASK.md and docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2/TASK.md; issuance-receipt.json carries byte length and SHA256. These identical repo bytes are authoritative. If Bridge normalizes markdown, preserve its transport copy privately and report the variant; do not overwrite the canonical task/receipt or retroactively change their hash. Empty Bridge Content means read the existing full canonical repo task, never reconstruct requirements.

Expected base is the latest Architect docs review. A following docs-only issuance commit is allowed: fresh fetch, verify expected base exists/is ancestor, record ACTUAL claim HEAD and show no incoming source/config/schema delta. One worker/task/result, no delegation, no screenshots, no idle/polling loop.
NO rebase, amend, force/force-with-lease, reset, stash, history purge or discarded user work. A rejected normal push or divergence is a STOP for safe reconciliation, never permission to rewrite main.
Stage explicit reviewed paths ONLY. No git add ., git add -A or blanket script/App_Data staging. Normal separate containment/code/evidence commits are allowed; do not squash/amend them for a one-commit cosmetic result.

## Architecture decision

ONE_CONNECTED_REVIEWED_WIRE_CONTRACT_ONE_PORTION_CAPACITY_AUTHORITY_AND_ONE_EXACT_ORIGINATING_CUSTOMER_INTENT_WITH_TARGETED_NEGATIVE_CONTROLS_AND_PRIVATE_LOCAL_KEY_PRESERVATION

All new permission comes from stored owning facts/Contracts-only ports. Keep approval decision-only, first-start vs Pending resume vs reviewed Failed retry separate, stable attempt/Inventory identities, exact six-decimal quantity arithmetic and accepted unit quantum. No frontend-only gate, second money/stock authority, inferred default condition, rounded/clamped quantity, giant rewrite, magic timeout or tracker-wide clear.

## A. Contain accidental tracked runtime material FIRST

Source-confirmed diff from 279-R1 adds plaintext Data Protection master-key XML:
src/backend/Host/Tooba.Host/App_Data/dataprotection-keys/key-6db9ecd9-d991-4408-8189-8a8d839da3d5.xml
and historical scratch scripts previously explicitly untracked (tb266r1/tb270r2/tb276/tb277r1/tb278/tb279), plus an unrelated old RESULT.bridge.txt.
Do not print/copy any key value, connection secret, actor token or private payload into logs/evidence/Bridge/commits.

1. Inventory ONLY incident paths against the 279-R1 diff. Preserve each local user scratch/runtime file privately OUTSIDE tracked source before untracking. Verify preservation by private file/hash comparison without showing contents. A copy is not permission to delete the active runtime file.
2. Remove these accidentally added scratch/key paths from Git tracking using narrowly named index operations, retain the user's local files and add exact scoped ignore rules. Do not delete historical user scripts, execute any seed/probe, or blanket-ignore legitimate production scripts. Restore source membership of the unrelated historical report without rewriting its original parent evidence.
3. Inspect actual Data Protection registration, active publish content root/key directory and use of the EXPOSED key by key id, not by displaying key material. Determine whether this key is currently loaded/accepted and which persisted/session purposes depend on it. Git removal alone does not erase historical exposure.
4. Produce key-containment.md with: privately preserved/untracked/ignored proof, active-key assessment, concrete safe retirement/replacement procedure and impact. Do NOT revoke/delete a runtime key, invalidate sessions, change encryption settings, purge history or risk undecryptable stored data under this task. If those are necessary, record Security-Exposure-State=CONTAINED_PENDING_REVIEWED_RETIREMENT and provide the exact minimal next action for Architect/user approval, while completing B–G independently. Do not claim the exposure is fully remediated. If positively proved inactive/unaccepted, record the proof and residual historical exposure; do not invent an emergency rotation.
5. Verify final staged/tracked files exclude the XML, key directories, task scratch copies, incident scripts and connection/secret outputs. A later normal commit removes source tracking; force-pushing secrecy away is forbidden.

## B. Connect every REAL operator modal to its owning contract (R1-A)

Current failures are reproducible from source:
- start_refund freezes only {kind:"start_refund"}, while endpoint expects expectedDecisionVersion in QUERY;
- retry sends only expectedFailedAttemptId, omitting required version;
- resolution modal sends acceptedQuantity/rejectedQuantity, while writer REQUIRES five explicit condition portions;
- custody modal omits required proofKind and uses total rejected (including never-received) as its ceiling;
- frontend snapshot mapper ignores the new portion members.

1. Choose ONE explicit typed wire shape per action and carry it unmodified through real modal -> retained envelope -> API -> route/model binding -> CQRS -> Directory. Keep existing route family and API conventions; additive version/body members are allowed. Do not fix by removing required server gates, defaulting missing versions/portions/proof, or selecting current/latest values at dispatch.
2. Freeze ExpectedDecisionVersion AND the applicable ExpectedResolutionVersion from the snapshot ACTUALLY reviewed when confirming a NEW approve/first-start/retry; include them in the exact retained intent and pass through Order operations adapter too. Do not reconstruct from a fresh GET. Exact stored replay precedes mutable new-effect gates.
3. Expose five explicit resolution quantities per own item:
AcceptedUsable, AcceptedDamaged, RejectedReceivedUsable, RejectedReceivedDamaged, RejectedUnreceived.
Show own product/unit, actual per-condition received quantities and remaining ceiling. Values must account for the whole original request; rejected-unreceived equals the actually unreceived remainder. Refuse locally and at owner; don't auto-accept a condition or turn missing into zero. Preserve requested money and display reviewed approved money separately.
4. Update frontend types/mappers/API helpers to actual additive snapshots, including per-portion completion and condition-specific custody. All zero/unknown/legacy fields remain honest.
5. Custody UI deliberately attests ACTUAL in-person handback, includes proofKind=operator_attestation plus required proof/reference and server-derived recipient meaning, and freezes the attestation. The chosen condition's remaining rejected-RECEIVED amount minus that condition's handbacks is the ONLY ceiling. Never include never-received quantity. No condition auto-default to usable.
6. Shared DS Dialog per reviewed action; safe cancel/Escape/focus/return; >=44px new controls; actual existing single react-toastify provider, one toast per command result and no toast on GET/render. No native prompt/private overlay/inline accordion.

## C. Repair portion occupancy including rejected-only sellers (R1-B)

1. ReadOccupancyAsync currently returns empty when the active-status request-id set is empty BEFORE reading HELD allocations for rejected cases. Read positive allocations/held goods irrespective of that status set. A rejected-only seller must not gain free return capacity while holding goods.
2. Custody recording currently trims active allocations to OwedCustody alone, discarding ACCEPTED consumption on mixed outcomes. ONE authority must distinguish:
accepted/consumed quantity (not available to another claim), outstanding rejected-received custody, unresolved active claim, and positively released rejected-unreceived/handed-back quantity.
Use this SAME authority for query eligibility, creation/reacquire and release under seller serialization.
3. Concrete required counterexample: delivered/requested/received 3, accepted 2, rejected-received 1; hand back 0.5 of rejected => occupied 2.5, available at most 0.5. It MUST NOT shrink to occupied 0.5. Once the rejected 1 is fully handed back, accepted 2 remains consumed.
4. Whole rejection with received goods keeps the held amount until actual condition-specific handback; unreceived rejected remainder may release safely. A rejected legacy case with no allocation rows cannot silently disappear when allocation cannot be proved. Incoherent receiving evidence cannot become held=0 and release capacity.
5. No mixed-item shortcut: each line and condition keeps its own facts and unit; no conversion to the first item's unit. Preserve old completed user facts, no data patch/backfill.

## D. Enforce reviewed plan/version and owning unit at write boundaries (R1-C)

1. A NEW approve/reject requires the reviewed decision version. NEW approval also binds the reviewed RESOLUTION version, carried from the real modal, not the POST-side fresh snapshot. Reopened/re-published plans cannot be approved by an older review even at the same decision version.
2. Under the same locked decision transition, re-read and compare reviewed versions/approved basis before writing baseline/revision/status/frozen approved amount/capacity. Preserve current atomic scope and no external port inside the transaction.
3. First/new retry verifies its reviewed version/basis; exact bound attempt replay remains lawful with zero new attempt/money. Missing required review is a typed refusal, not a current/latest fallback.
4. PublishResolutionAsync and RecordCustodyAsync currently enforce only six decimals. Enforce EACH NEW portion against the owning accepted Order line unit quantum using the existing owner reader/rule. A whole-piece line cannot accept 0.5 or hand back 0.5; a 3-decimal gram line can accept 0.125, not 0.0001. Missing metadata must follow the established conservative explicit policy, never a guessed six decimals.
5. Exact stored occurrence replay happens BEFORE new quantum/availability gates and compares the whole immutable intent. Preserve rollback/savepoint/caller tracking invariants and exact provider conflict identities; no Clear or swallowing other faults.

## E. Only positively evidenced custody and slice allocation (R1-D)

1. No owning carrier delivered-back reader exists in this increment. Therefore REFUSE carrier_delivered_back with the typed unavailable blocker at parser/owner; accepting that text plus a reference is not verified delivery. Operator attestation remains the explicitly reviewed actual in-person path. Do not build a carrier subsystem.
2. Legacy ambiguity cannot be inferred only from the count of CURRENTLY in-window slices. Expired + live slice can still have multiple possible historical origins. Require positive unique attribution across ALL plausible original delivery slices; otherwise typed legacy ambiguity. Never guess oldest-first for unbound legacy consumption.
3. Keep oldest-eligible-deadline-first for NEW provably allocatable claims. Reopen must re-acquire the exact previously owned slice capacity under seller serialization, not reactivate all rows because aggregate quantity in some other slice is free.
4. Counterexamples: expired slice A + live slice B + unbound legacy case of uncertain origin => no guessed capacity; A fully taken by sibling while B has free quantity => old A-bound reopen refuses. No new date, cross-schema SQL, guessed legacy allocation or backfill.

## F. Close unmount/v1/same-key retained-intent gaps (R1-E)

1. The customer effect has no cleanup, so unmount leaves old epoch valid. Invalidate on unmount, close and scope/viewer change; fence BEFORE every next child send as well as after answers, UI/toast/callback/finally and busy-lock release. A late answer may reconcile ONLY its exact originating stored child, not continue the old act on a new viewer/surface.
2. Recognize existing v1/tb278 retained evidence. Changing prefix/version to v2 must not read it as none and create new random keys over a possibly completed first send. Preserve exact legacy bytes and block new submission for unresolved/unreadable legacy evidence until safely attributable/reconciled; do not adopt another viewer's act, clear it, or pretend an unscoped old act is authenticated. If safe replay cannot be proved, explicit unresolved support/review state, no replacement send.
3. recordChildOutcome must compare the EXPECTED exact originating envelope/child body and scoped identities before updating. submissionKey/childKey alone are insufficient. A changed same-key/body cannot be marked recorded or cleared by the old callback.
4. Maintain read-back-before-send, safe storage property/method acquisition, unknown outcome retention, independently terminal recorded children and exact replay of unresolved bodies only. Validate a success snapshot's owning case/line/seller binding before terminal recording, not any ok-shaped object.
5. Preserve same-line multi-reason quantities under one ceiling and body-dictated-only no-effect rules. No timer/polling/retry daemon.

## G. Meaningful connected proof, not permissive mocks (R1-F)

Create a short scenario matrix FIRST, implement once, then run only changed focused suites after final edits. Each regression must fail on reviewed 279-R1 code for the stated reason and pass on the fix; a documented temporary negative control is allowed, restored before commit, no baseline rewrite.

Required controlled connected/owning scenarios:
G1. Real workspace start-refund modal freezes versions; actual API route receives the required values and a valid owning controlled case creates exactly one first attempt. Duplicate replay creates nothing; wrong/missing version refuses. Cover query/body binding explicitly, not a mocked route that accepts all bodies.
G2. Same for new reviewed Failed retry, preserving exact failed identity and version in the frozen transport; real Directory A-failed/B-reserved/A-retry exceeds shared payment budget => no child, gateway/Wallet zero; missing/foreign/currency authority refuses. Retain the existing reservation method.
G3. Workspace resolution sends all five actual portion fields through real mapper/helper/endpoint contract to a controlled Directory. Received 3 usable / accepted 2 / rejected 1 => actual Inventory argument 2 only. Received 2 / rejected-unreceived 1 progresses with no invented receipt. Mixed accepted usable/damaged needs both durable confirmations; injected local-save failure does not create false completion or duplicate stock.
G4. Whole rejection then explicit condition-specific handback succeeds with operator attestation; missing proof/wrong condition/over-ceiling refuses. Unsupported carrier token refuses. Cover rejected-only seller occupancy and the 2.5 mixed-occupancy example above through the actual evaluator, not only hand-built policy snapshots.
G5. Re-publish resolution after operator GET but before old approve, missing version after reopen, half-piece portion/custody, expired+live ambiguous legacy slice and exact-slice reacquisition counterexamples refuse with no rows/effects.
G6. Mount real customer modal: unmount/close during first of two children => no second send/stale toast/callback; unresolved v1 evidence => no new act; replace frozen body under same act/child key before response => no old-outcome update/clear. Existing viewer/checkout/storage/read-back/partial-success/exact retry regressions stay.
G7. Retain existing rollback-only real database lock/atomic approval facts; if coordination code changes, extend the SAME bounded harness only for that path. No new committed fixture/race/delete cleanup/second database. Test protocol queries are not proof that the effectful reservation Directory ran; report the exact boundary.
G8. Strict frontend transport contract assertions reject missing actual versions/portion/proof. Real owning Directory facts prove effects using scripted ports; jsdom proves lifecycle, not pixels. Do not let all POST mocks blindly return success.
G9. Read-only 390/1440 DOM/overflow for currently served history; controlled mounted active-dialog behavior/focus/local scroll. No screenshots or manufactured active demo.

Validation selection:
- only directly changed Returns behavior classes plus retained focused classes affected by this repair;
- affected customer/operator mounted, submission and API contract suites, tsc, one production Next build;
- targeted affected Host endpoint/relational facts and exact architecture pins ONLY if route/contract/error additions require changes;
- backend Release solution build and Host publish, record exact command/count/error.
No whole Returns/Host/AMSC broad run, no repeated green-suite loops, no fake test-name citations. Report failed attempts/corrections honestly. Closure matrix maps each R1-A–F/small item to actual source and existing named meaningful fact; do not describe pure tests as Directory/Inventory tests.

## 3. Schema/deploy/data authority

Default ZERO migration: R1 already added the durable portion/proof columns. Repair mapping/validation/logic using them; do not edit applied migrations, Down/reapply/backfill or introduce another migration automatically. If required durable intent/version/quantity facts truly cannot be represented, finish independent work, provide exact minimal proposal and STOP for schema authorization before creating/applying it. Never use an in-memory-only workaround to avoid a necessary durable fact.

ONE canonical tooba_alpha at127.0.0.1:5432 SingleStore/store-alpha; Host5088, Next3000. No Docker/second database15432/15433, edition/tenant/settings/connection/port/CapturedPreview change.
ZERO live business mutation or provider/PSP/Wallet/Inventory/waybill/notification call. No live POST even a refused probe. GET/read-only DOM/SELECT on the three stored Completed cases only; preserve original updated_at/refund/receipts/attempts/history. No demo case/order/hold/reset/seed/cleanup/restore.
Task-owned relational facts roll back, separately verify committedRows=0; don't execute the historical scratch scripts.
Refresh changed Host/Next gracefully from correct publish working directory and current config. Verify loaded DLL hash, BUILD_ID/manifests/chunks, health/ready/platform and read-only history. Do not equate health/build success with business-path coverage.
No active live case/customer session is authorized here: declare the active user walkthrough unavailable instead of inventing one. User may inspect history; connected behavior proof uses the actual components/Directory with controlled ports. No real financial/provider/full-Marketplace acceptance claim.

## 4. Delivery/recovery/result

Evidence under docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2/:
TASK.md (exact issuance, unchanged), issuance-receipt.json (unchanged), RESULT.md, RECEIPTS.md, closure-matrix.md, connected-wire-proof.md, quantity-capacity-proof.md, retained-intent-proof.md, key-containment.md, user-review.md, result-contract.txt.
Keep evidence concise. Correct overstated old claims by ADDING this review/result, not changing old TASK/RESULT/receipt to match a new story. Original R1 reports and history remain immutable.
Update compact AGENTS CURRENT, recovery, START-HERE and Returns checklist pointers; distinguish functional closure from security retirement and live/provider limits. Last accepted foundations remain unchanged; parent279/R1 and278 remain PARTIAL until Architect review.
Record actual claim HEAD (not merely expected base), code/evidence/final SHA, each normal commit and fresh origin parity. Before every commit inspect staged diff for exactly allowed paths and zero secrets/scratch/App_Data. No force push, amend or rebase under any circumstance.
PASS requires all functional B–F and their meaningful G proofs; unresolved critical functional counterexample => PARTIAL/BLOCKED. Repository containment must be truthful, historical exposure/retirement reported separately. Never label all security remediated when only tracking was removed.
If key retirement needs approval, deliver a concrete reviewed impact/procedure after ALL authorized independent fixes; do not stop at the first key finding. No automatic follow-on task.

Canonical final result:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2
Parent-Task: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Scope: A_G_CONNECTED_279_R1_REPAIR_AND_PRIVATE_REPOSITORY_CONTAINMENT
Expected-Base-SHA: 6e3d1873f7f9926c2906ae4e26547a6c9ef97eeb
Base-SHA: <actual claimed HEAD; permitted docs delta separately>
Code-SHA: <sha>
Evidence-SHA: <sha>
Final-HEAD: <sha>
HEAD-Equals-Origin-Main: <fresh fetch>
Repository-Containment-State: <private preservation/untracked/ignored>
Security-Exposure-State: <inactive proved or contained pending reviewed retirement; no key values>
Wire-Closure-State: <actual modal/transport/endpoint proof>
Capacity-Closure-State: <rejected-only and mixed accepted/held proofs>
Version-Quantum-State: <actual owning refusal/positive facts>
Evidence-Authority-State: <carrier/legacy exact proof>
Customer-Intent-State: <unmount/v1/exact-origin proof>
Validation-State: <actual commands/facts and failed attempts>
Schema-State: ZERO
Live-Business-Write-State: ZERO
Provider-Money-Stock-Effect-State: ZERO
User-Work-Preserved: YES
Git-Rewrite-State: NONE
Architect-Accept-State: NO
User-Acceptance-State: NO
Evidence-Limits: <actual boundaries>
Recovery-Next-Task: NONE
Next-Recommended-Task: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT
END_TOOBA_WORKER_RESULT

Post only the complete true Result to Bridge after validation/normal commits/push/fresh parity. No /api/results placeholder probe. Complete task lifecycle, return IDLE and stop entirely.
STOP_FOR_ARCHITECT
END_TOOBA_TASK

