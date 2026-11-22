# TOOBA — OpenCode Architect Delivery Method

Protocol: **TOOBA-OPENCODE-DELIVERY-V1**
Version: **1.1.0**
Adopted: **2026-11-22**
Owner: **Tooba architect**
Status: **ACTIVE for new OpenCode tasks only**
Supersedes: **1.0.0** ([immutable archive](versions/ARCHITECT-DELIVERY-METHOD-v1.0.0.md))
Revision authority: **owner explicit approval on 2026-11-22**
Pilot evidence: 283 → connected 284 → TB-OCC-284 independent acceptance. The owner explicitly authorized recording this method **only on success**, and subsequently confirmed adoption.
Reviewed code: `611f97a85ec7c2e464e4f649820992202ca3754c`.
Connected evidence: `fc40d412aa6d085d3db3f1deac461eb93a10f48d`.
OpenCode verifier evidence: `78f654f5a96d0bf9c0660b0d39e93e5e80660ca3`.
Pilot's dedicated verification: `docs/opencode/tasks/TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE/`.

## 0. Hard boundary and precedence

This is an **architect-facing, deterministic issuance and review procedure**. It is **not** a product feature, a replacement for the domain architecture, or a new roadmap. It applies only when the recipient worker is OpenCode.

**Never edit to satisfy OpenCode:** `docs/ai/delivery/**`, `docs/ai/tasks/**`, `docs/architecture/TOOBA-DELIVERY-ASSURANCE.md`, historical Cursor gate outcomes, or existing Bridge protocol. Cursor contracts are separate and immutable for OpenCode work. Do not change `AGENTS.md` or the roadmap as a side effect of introducing this OpenCode method. OpenCode-only artifacts live under `docs/opencode/`.

Priority: owner's explicit decision → existing authorized architecture and fixed roadmap → architect-issued task with exact requirements → this execution/verification protocol → worker plan. No worker can silently change a higher-priority contract.

Interpretation rule: **MUST** means mandatory whenever the stated applicability condition holds; **MUST NOT** means forbidden. A test that did not run is NOT_RUN, never PASS. Missing proof does not mean a product bug, but prevents acceptance until addressed. Worker PASS ≠ architect ACCEPT ≠ owner acceptance.

## 1. Architect PRE-ISSUE — mandatory before dispatch

Architect reads the current real source and records all of these fields in `docs/opencode/tasks/<Task-ID>/TASK.md`:

- `Task-ID` unique, `Parent-Task` when relevant, `Repository`, `Branch`, `Expected-Base-SHA`, `Channel`, `Worker-Id`, `Agent-Type: opencode`, `Model`, `Protocol: TOOBA-OPENCODE-DELIVERY-V1`, `STOP_FOR_ARCHITECT`, `automaticNextImplementationTask: NONE`.
- Exactly one approved roadmap stage and an observable operator/customer delivery; exact in-scope and out-of-scope features.
- Real UI entry, owning endpoint, authorization, domain/data owner, request/response, persistence, independent read; enumerate cross-module boundaries. Never treat Catalog SKU as identical to seller Offer SKU without an explicit domain decision.
- Stable unique REQ IDs; each with GIVEN/WHEN/THEN, expected response and durable effect, forbidden effects, relevant actor, expected proof grade, pass/fail assertions. Every REQ exists in both task text **and** a task-local OpenCode machine-readable requirement list; the sets MUST be equal. For tasks governed by v1.1.0+, all normative invariants and corresponding positive/negative/boundary test IDs MUST ALSO appear in the architect-issued `ACCEPTANCE-MATRIX.json` as specified in §1A.
- Test fixture/identity plan, isolation and cleanup; allowed write paths; forbidden migrations/seeds/live business writes; relevant locale/RTL and design-system rules.
- Exit states `READY_FOR_ARCHITECT_REVIEW`, `BLOCKED_PREFLIGHT`, `BLOCKED_IMPLEMENTATION`, `BLOCKED_EVIDENCE`; precise definition of PASS and one post-matrix repair round.

**PRE-ISSUE HARD GATE:** architect validates task file, manifest and schema **before** commit/dispatch: all required fields present; REQ lists identical and unique; base SHA and paths exist; environment/proof chain feasible; no self-contradictory stop or repair instructions; no reference to Cursor structural checker; task bytes SHA256 recorded; and §1A's invariant-to-test coverage audit and matrix schema pass. If validation fails, do not issue; correct artifact first. Pilot 284 failed because an incompatible REQ table was omitted. The architect must prevent this, not delegate discovering malformed task format to the worker.

## 1A. Mandatory test-coverage contract — ARCHITECT PRE-ISSUE HARD GATE (v1.1.0)

**Reason for this revision:** Task 285 had 8 REQs and a 23-assertion checker, yet the first complete matrix did not test a replay with the SAME `requestId` and a DIFFERENT commercial intent. The architect independently found that `CreateOfferCommand` returned the prior Offer based only on seller+variant+requestId, without validating changed SKU/other request fields. This is an **architect test-specification omission** as well as a product behavior gap, not a reason to call the worker's actually executed tests dishonest.

**For every future OpenCode task issued with v1.1.0 or later, the architect MUST:**

1. Derive a finite list of **atomic behavioral invariants** from every REQ's GIVEN/WHEN/THEN. Split compound statements such as "same request reuses id, different intent fails closed" into **separately named** assertions. Each MUST have input, actor/scope, response/observable UI, expected persistent change or forbidden side effect, and an independent oracle (GET/SQL/provider state if persistence is claimed).
2. Create task-local, **architect-issued** `ACCEPTANCE-MATRIX.json` before dispatch. Each entry MUST include `reqId`, `invariantId`, `testId`, `caseType` (`positive`, `negative`, or `boundary`), `given`, `action`, `expected`, `forbiddenEffects`, `oracle`, `proofGrade`, and `reasonIfNotApplicable`. Names and IDs must be unique and stable. In the issued TASK, name the exact mandatory tests and refer to this matrix by path.
3. Cover **positive, negative and boundary** behavior for each REQ when meaningful. A type may be `NOT_APPLICABLE` **only** with an explicit *architect-authored* justification in the issued matrix and its equivalent evidence-grade boundary; the worker cannot unilaterally drop a case. Security, permissions, duplicate prevention, concurrent edits, network uncertainty and inventory/payout invariants always require negative and boundary consideration.
4. Specifically for **idempotency/retry** contracts: test `same key + identical payload`, `same key + changed meaningful payload`, `different key + conflicting business identity`, and concurrent identical requests when relevant; verify typed responses, one durable object, no unwanted writes, and independent read. A "duplicate did not occur" happy path is **not** proof of intent-conflict handling.
5. For every multi-domain workflow, add adversarial **partial commit, stale reads, lost/malformed responses, permissions and cross-tenant/owner** cases where applicable. A nominal successful request must not stand in for these.
6. **PRE-ISSUE COVERAGE GATE:** before publishing TASK, the architect MUST compare every normative statement in TASK/OpenSpec against matrix `invariantId` and `testId`, ensuring no orphan requirement, orphan test, missing case type, ambiguous expected effect or missing independent oracle. Reject the task locally if coverage is incomplete. **Counting REQs or assertions alone is not sufficient.**
7. The OpenCode worker MUST implement the issued matrix *exactly*, record PASS/FAIL/NOT_RUN per `testId`, with concrete observed values and evidence hashes, and the task-local checker MUST validate **every issued invariant/test ID** against immutable issued `ACCEPTANCE-MATRIX.json` (including negative and boundary cases), not merely 8/8 REQs. A test with no executed proof is NOT_RUN. Checker negative self-tests MUST show rejection of deleting a required test, flipping expected fact, and evidence tampering.
8. **ARCHITECT REVIEW GATE:** source-review the actual conditional branches and compare against the issued matrix. If a material source branch/behavior is not covered, do **not** issue `ARCHITECT_ACCEPT` even when all issued tests pass; record `REPAIR_REQUIRED` within the original task's permitted repair budget or `BLOCKED` otherwise. Record omissions as an **architect issuance deficiency** and feed them into the next versioned governance revision, never silently reinterpret issued test scope.

**Required example for the 285 lesson:**
- `REQ-03 / retry-identical`: same requestId, seller, variant, SKU and other creation terms → same OfferId, one row, zero duplicate downstream writes.
- `REQ-03 / retry-changed-intent`: same requestId/seller/variant, SKU-A then SKU-B → typed conflict, no success, one unchanged Offer, zero unintended Pricing/Inventory writes.
- `REQ-03 / retry-concurrent`: concurrent identical requests → one OfferId, no uncontrolled 500, independent GET/SQL shows one row; claims require actual concurrency proof.

**Existing task contract protection:** this rule is **prospective**. It does not rewrite 283/284/285's immutable TASK.md or REQUIREMENTS.json or retroactively declare their historic 23 passing tests failures. Task 285 remains `REPAIR_REQUIRED` under its already recorded architect review; it can gain additional evidence in its one bounded final repair without rewriting issued scope.

## 2. Worker PRE-CODE — prove the required chain

After `git fetch origin`, read task and this protocol from `origin/main`; verify repository, HEAD/base ancestry, existing work and remote parity without overwriting user work.

For a persistent UI operation, demonstrate **before product-code edits** a safe end-to-end test path:
real Chromium via Playwright → served Next build → real owning Host endpoint → isolated PostgreSQL → independent GET and SQL → disposable teardown. Show test actor and seller scope, ports, Host DLL/build identity, database identity, fixture values, write boundary and ability to clean up. A health check or mocked request alone is not enough. For non-persistent/non-UI tasks define the equivalent owning-provider evidence chain explicitly.

If unsafe or infeasible, stop **before product edits** as `BLOCKED_PREFLIGHT` with one exact cause and smallest unblock. Do not substitute fake data for required real-provider proof. No live user/business writes, migrations, resets, grants or provider effects without explicit authorization.

## 3. Tool use — required only when applicable, with evidence

**OpenSpec:** for feature/behavior changes, create proposal/design/spec/tasks and fixed REQ mapping **before production code**, run strict validation, record command/exit/log. For verification-only tasks, validate the relevant existing change; do not invent a new spec.

**Impeccable:** for any actual UI design, form, accessibility or interaction change, document `critique → explicit design choices → implementation → post-change evaluation` and concrete evidence. Merely installing or reading it is not usage. For evidence-only tasks without UI changes, state `NOT_APPLICABLE: NO_UI_CHANGE`; do not falsely claim execution.

**Playwright:** real installed Chromium against served app; use owning Host + isolated persistence for claims about Save/reload. Controlled HTTP is permitted for deterministic malformed/lost/wrong-response **error branches only**, labeled explicitly; it cannot replace connected success proof. Record exact request methods/bodies/counts and browser outcomes. Use test tools proportionately; no ritual use of unrelated tools.

## 4. Controlled implementation, bounded repairs, fixed roadmap

Implement ordered small waves **inside the one authorized task**, each `reproduce → implement → focused check → connected check → diff review`. Do not launch self-created follow-up tasks, broaden the scope, or silently redefine REQs. New unrelated findings are backlog, not automatic blockers; a true P0 safety/integrity risk is a blocker.

Debug and fixture corrections while reaching the **first complete acceptance matrix** are development work, not the one architect repair round; record them. After the first complete full matrix, permit **at most one consolidated correction pass** targeted at the failed original REQs, then **one final full rerun**. If not all mandatory criteria pass, stop with specific BLOCKED state and evidence. This rule prevents both premature PARTIAL delivery and infinite repairs.

No stage-to-stage automatic progression. Stage 2 cannot begin until architect review and an explicit next issued task.

## 5. Evidence contract and OpenCode-specific checker

Each task produces under `docs/opencode/tasks/<Task-ID>/`: immutable `TASK.md` and its SHA256; architecture plan before product changes if applicable; a machine-readable evidence manifest; reproducible `VERIFY-OPEN-CODE.mjs` or explicitly versioned equivalent; `RESULT.md`; `RECEIPTS.md`; actual evidence files/logs; plus a review note. These are **OpenCode-specific** assets and must not call or modify Cursor's `check-readiness.mjs` as their acceptance gate.

The manifest declares `protocol`, task and parent identity, fixed REQ IDs, each test result (PASS/FAIL/NOT_RUN), named assertions with observed facts, evidence grade/provider, repo-relative path and SHA256, source SHA, served Frontend/Host identity, and cleanup. For data writes include actor/tenant/seller/offer/position, request methods/paths/bodies/counts, before-state, independently obtained GET and SQL after-state, unintended effects, and no-duplicate-write proof.

The checker MUST independently parse the manifest, immutable issued acceptance matrix and evidence file bytes, enforce exact requirement IDs, atomic invariant IDs, test IDs and case types, SHA256, code/build identity, required provider grade and cleanup. It must reject malformed status/REQ lists, tampered evidence, missing asserted facts, wrong expected values; negative self-tests must prove rejection. It does not certify the authenticity of worker-authored logs by itself; independent architect inspection remains mandatory.

**Historical truth:** Cursor's failed 284 structure gate remains `KNOWN_FORMAT_INCOMPATIBILITY` and is neither product PASS nor FAIL. Do not fabricate a passing Cursor gate, edit its original task, or hide its failure.

## 6. Outcome, review and promotion

Worker only reports `PASS / READY_FOR_ARCHITECT_REVIEW` after all applicable mandatory REQs PASS and OpenCode verifier + negative self-tests PASS. If not: `BLOCKED / NOT_READY` and exact reason, without automatically issuing another task. Always `STOP_FOR_ARCHITECT` with no future job.

Architect must independently examine: immutable scope, repository diff, underlying source code, evidence and assertion logic (not worker summary alone), provider identity and limits, negative cases, code/build/hash provenance, cleanup, and any contradictions. Architect records `ACCEPT`, `REPAIR` (only within permitted budget), or `BLOCK` with reasons. Distinguish product acceptance from technical architecture acceptance.

**Acceptance of a pilot method is conditional:** candidate governance may only be promoted if functional connected proof, separate OpenCode checker, negative controls, and architect review succeed. For 283/284/TB-OCC-284 these conditions were met as documented; owner explicitly authorized adding the method. This document is that governance-only promotion. It neither changes product code nor marks Cursor's historical gate PASS.

## 7. Decision table

| Condition | Required decision |
|---|---|
| Architect task schema, normative behavior-to-test coverage, or feasibility incomplete | DO NOT ISSUE; fix before dispatch |
| Unsafe/no connected test chain before coding | BLOCKED_PREFLIGHT; no product edit |
| OpenSpec strict invalid for applicable behavior task | BLOCKED before code |
| UI changed without documented Impeccable critique/post-check | NOT_READY |
| Persistence claimed without owning Host + isolated DB + independent GET/SQL | BLOCKED_EVIDENCE |
| Any mandatory issued invariant/test FAIL/NOT_RUN (not only REQ count) | NOT_READY; one bounded repair after first full matrix |
| One repair and final rerun still fail | BLOCKED; no auto task |
| All REQs pass but checker/hashes/negative control fail | BLOCKED_EVIDENCE |
| Worker PASS with full evidence | READY_FOR_ARCHITECT_REVIEW, not ACCEPT |
| Architect evidence audit PASS | Architect ACCEPT, owner acceptance separate |

## 8. Immutable pilot lessons and interpretation

1. **283:** OpenSpec + Impeccable guidance + controlled Chromium + split PostgreSQL tests improved code but did not prove all connected persistence/price-concurrency requirements. Controlled mocks cannot be relabeled connected.
2. **284:** the real connected browser→Next→Host→PostgreSQL harness proved 7 REQs/38 assertions without product code change; Cursor-specific task-table parser rejected the architect-issued OpenCode artifact. That was an **issuance schema mismatch**, not a product defect.
3. **TB-OCC-284:** independent OpenCode checker passed and rejected removed REQ-07, changed REQ-01 price and tampered evidence; records preserved the Cursor gate failure. The evidence-only task did NOT rerun the disposed original database. REQ-05 error branches used controlled response substitution and were correctly labeled. The source JSON's 409 `inventory.position.id_unknown` for foreign position overrides an inconsistent older prose statement of 400.
4. The architect must preflight its own **issuance template** and never confuse a tool's structural success with genuine end-to-end business completion.
5. **285 lesson and v1.1.0 owner-approved improvement:** merely matching 8 REQs and executing 23 named assertions failed to expose a different-payload idempotency replay. Every normative behavioral clause now requires an architect-issued atomic test mapping, including meaningful negative/boundary coverage before issuance; architect independently audits source branches after delivery.
6. The method does **not** guarantee all tasks succeed; it ensures safe preflight, no invented success, bounded repair, immutable scope, and explicit accountable stopping.

## 9. Stable recovery rule for future architect sessions

On every future Tooba OpenCode task, the architect MUST first read **this exact file**, then current roadmap/source, then the issued task, its REQ manifest, latest verified code/evidence and architect review. Never reconstruct protocol from a chat paraphrase or another tool's template. Cite `TOOBA-OPENCODE-DELIVERY-V1@1.1.0` in newly issued OpenCode TASK.md; already issued tasks retain their original protocol version and remain immutable. Any change to this protocol requires an explicit version bump, rationale, and architect/owner authorization; no silent reinterpretation.

**This file is the canonical OpenCode architect procedure. The Cursor procedure remains separate.**
