# TOOBA — OpenCode Architect Delivery Method

Protocol: **TOOBA-OPENCODE-DELIVERY-V1**
Version: **1.0.0**
Adopted: **2026-11-22**
Owner: **Tooba architect**
Status: **ACTIVE for new OpenCode tasks only**
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
- Stable unique REQ IDs; each with GIVEN/WHEN/THEN, expected response and durable effect, forbidden effects, relevant actor, expected proof grade, pass/fail assertions. Every REQ exists in both task text **and** a task-local OpenCode machine-readable requirement list; the sets MUST be equal.
- Test fixture/identity plan, isolation and cleanup; allowed write paths; forbidden migrations/seeds/live business writes; relevant locale/RTL and design-system rules.
- Exit states `READY_FOR_ARCHITECT_REVIEW`, `BLOCKED_PREFLIGHT`, `BLOCKED_IMPLEMENTATION`, `BLOCKED_EVIDENCE`; precise definition of PASS and one post-matrix repair round.

**PRE-ISSUE HARD GATE:** architect validates task file, manifest and schema **before** commit/dispatch: all required fields present; REQ lists identical and unique; base SHA and paths exist; environment/proof chain feasible; no self-contradictory stop or repair instructions; no reference to Cursor structural checker; task bytes SHA256 recorded. If validation fails, do not issue; correct artifact first. Pilot 284 failed because an incompatible REQ table was omitted. The architect must prevent this, not delegate discovering malformed task format to the worker.

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

The checker MUST independently parse the manifest and evidence file bytes, enforce exact requirement IDs and assertion names, SHA256, code/build identity, required provider grade and cleanup. It must reject malformed status/REQ lists, tampered evidence, missing asserted facts, wrong expected values; negative self-tests must prove rejection. It does not certify the authenticity of worker-authored logs by itself; independent architect inspection remains mandatory.

**Historical truth:** Cursor's failed 284 structure gate remains `KNOWN_FORMAT_INCOMPATIBILITY` and is neither product PASS nor FAIL. Do not fabricate a passing Cursor gate, edit its original task, or hide its failure.

## 6. Outcome, review and promotion

Worker only reports `PASS / READY_FOR_ARCHITECT_REVIEW` after all applicable mandatory REQs PASS and OpenCode verifier + negative self-tests PASS. If not: `BLOCKED / NOT_READY` and exact reason, without automatically issuing another task. Always `STOP_FOR_ARCHITECT` with no future job.

Architect must independently examine: immutable scope, repository diff, underlying source code, evidence and assertion logic (not worker summary alone), provider identity and limits, negative cases, code/build/hash provenance, cleanup, and any contradictions. Architect records `ACCEPT`, `REPAIR` (only within permitted budget), or `BLOCK` with reasons. Distinguish product acceptance from technical architecture acceptance.

**Acceptance of a pilot method is conditional:** candidate governance may only be promoted if functional connected proof, separate OpenCode checker, negative controls, and architect review succeed. For 283/284/TB-OCC-284 these conditions were met as documented; owner explicitly authorized adding the method. This document is that governance-only promotion. It neither changes product code nor marks Cursor's historical gate PASS.

## 7. Decision table

| Condition | Required decision |
|---|---|
| Architect task schema or feasibility incomplete | DO NOT ISSUE; fix before dispatch |
| Unsafe/no connected test chain before coding | BLOCKED_PREFLIGHT; no product edit |
| OpenSpec strict invalid for applicable behavior task | BLOCKED before code |
| UI changed without documented Impeccable critique/post-check | NOT_READY |
| Persistence claimed without owning Host + isolated DB + independent GET/SQL | BLOCKED_EVIDENCE |
| Mandatory REQ FAIL/NOT_RUN | NOT_READY; one bounded repair after first full matrix |
| One repair and final rerun still fail | BLOCKED; no auto task |
| All REQs pass but checker/hashes/negative control fail | BLOCKED_EVIDENCE |
| Worker PASS with full evidence | READY_FOR_ARCHITECT_REVIEW, not ACCEPT |
| Architect evidence audit PASS | Architect ACCEPT, owner acceptance separate |

## 8. Immutable pilot lessons and interpretation

1. **283:** OpenSpec + Impeccable guidance + controlled Chromium + split PostgreSQL tests improved code but did not prove all connected persistence/price-concurrency requirements. Controlled mocks cannot be relabeled connected.
2. **284:** the real connected browser→Next→Host→PostgreSQL harness proved 7 REQs/38 assertions without product code change; Cursor-specific task-table parser rejected the architect-issued OpenCode artifact. That was an **issuance schema mismatch**, not a product defect.
3. **TB-OCC-284:** independent OpenCode checker passed and rejected removed REQ-07, changed REQ-01 price and tampered evidence; records preserved the Cursor gate failure. The evidence-only task did NOT rerun the disposed original database. REQ-05 error branches used controlled response substitution and were correctly labeled. The source JSON's 409 `inventory.position.id_unknown` for foreign position overrides an inconsistent older prose statement of 400.
4. The architect must preflight its own **issuance template** and never confuse a tool's structural success with genuine end-to-end business completion.
5. The method does **not** guarantee all tasks succeed; it ensures safe preflight, no invented success, bounded repair, immutable scope, and explicit accountable stopping.

## 9. Stable recovery rule for future architect sessions

On every future Tooba OpenCode task, the architect MUST first read **this exact file**, then current roadmap/source, then the issued task, its REQ manifest, latest verified code/evidence and architect review. Never reconstruct protocol from a chat paraphrase or another tool's template. Cite `TOOBA-OPENCODE-DELIVERY-V1@1.0.0` in new OpenCode TASK.md. Any change to this protocol requires an explicit version bump, rationale, and architect/owner authorization; no silent reinterpretation.

**This file is the canonical OpenCode architect procedure. The Cursor procedure remains separate.**
