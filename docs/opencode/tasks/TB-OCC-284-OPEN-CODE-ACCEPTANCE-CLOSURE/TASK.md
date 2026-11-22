# OpenCode-only Task — TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE
Parent-Task: TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284
Program: TOOBA
Channel: tooba-main
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: opencode
Model: metisdeepseek/deepseek-v4-flash
Expected-Base-SHA: f006fbdbdbfd4911ea439fe9a3443438af6cf786
Mode: OPEN_CODE_INDEPENDENT_ACCEPTANCE
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK_HEADER

## 0 — Non-negotiable: do not touch Cursor governance
This instruction is an **OpenCode-only** artifact. The following are legacy Cursor-governed structures for this task and MUST remain byte-exact and untouched:
- `docs/ai/delivery/check-readiness.mjs` and everything in `docs/ai/delivery/`
- `docs/architecture/TOOBA-DELIVERY-ASSURANCE.md`
- `docs/ai/tasks/TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283/**`
- `docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/**`
- `AGENTS.md`, `docs/ROADMAP.md`, domain architecture and existing Bridge protocol

Do not reissue or edit 284's frozen TASK.md, replace its SHA, introduce a waiver, make Cursor's gate pass by changing inputs, or reinterpret its FAIL as PASS. Do NOT run Cursor's structural gate as an OpenCode acceptance condition. Its known schema failure remains historical truth; do not remove the evidence. Do not edit feature/source code, launch Stage 2, or broaden requirements.

## 1 — Objective and exact success
Decide whether **Stage 1 seller offer safe edit** has enough authentic evidence for architect review despite the separate, format-specific Cursor gate rejecting OpenCode's task artifact. Reuse 283/284 evidence with explicit original provenance and validation, not unverified worker claims. This work is **evidence / OpenCode acceptance only**.

Output under `docs/opencode/tasks/TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE/`:
- `EVIDENCE-MANIFEST.json`: OpenCode-specific strict schema described below
- `VERIFY-OPEN-CODE.mjs`: small dependency-free reproducible Node checker
- `EVIDENCE-REVIEW.md`: independent review of evidence adequacy/limitations
- `RESULT.md`: decision and instructions to architect
- `PROPOSED-OPEN-CODE-DELIVERY-METHOD.md` **only on successful verification**. This is CANDIDATE, not adopted governance.
- `verification.log`: actual checker stdout, status and SHA references

No Cursor template, Cursor manifest or Cursor readiness checker is used for acceptance.

## 2 — Preflight / source authentication before any write
Run `git fetch origin`; validate origin remote, `main`, existing worktree and branch ancestry; if HEAD has drifted, reconcile without overwriting user work. Read this TASK from `origin/main`; read original 283 TASK, OpenSpec proposal/design/spec, 284 TASK/RESULT/RECEIPTS, architect review, real `evidence/connected-284.{json,cjs,log}`, both gate logs, and actual production code paths. Record HEAD / origin/main, the SHA-256 of every source evidence file, source code SHA `611f97a85ec7c2e464e4f649820992202ca3754c`, historical evidence commit `fc40d412aa6d085d3db3f1deac461eb93a10f48d`. Do NOT trust hashes supplied in a Markdown report without recomputing from the Git object bytes. If source evidence is missing, inconsistent or not safely attributable to code/build, fail closed.

## 3 — OpenCode acceptance contract (NOT a Cursor REQ-table)
Use a machine-readable JSON object with exact keys:
`protocol="TOOBA-OPENCODE-ACCEPTANCE-V1"`;
`parentTaskId`, `sourceCodeSha`, `sourceEvidenceCommit`, `requirements`, `toolEvidence`, `cleanupEvidence`, `result`.
Requirements must contain **exactly** unique IDs REQ-01, REQ-02, REQ-03, REQ-04, REQ-05, REQ-06, REQ-07. Each row must have `id`, `status` PASS/FAIL/NOT_RUN, `evidencePaths` (nonempty, repo-relative), `checks` (exact assertion names and observed values), `evidenceClass` (connected-real or controlled-error), and `limitations`. `result` holds `status` PASS/BLOCKED and `blockingItems` array. `toolEvidence` identifies OpenSpec strict evidence, Chromium/Playwright execution, real Host/Next build identity and PostgreSQL database. `cleanupEvidence` identifies disposal and canonical service verification.

The checker must:
1. validate exact protocol/IDs/status/schema; reject duplicate/missing requirements;
2. read referenced files INSIDE repository; recompute SHA-256 and compare to committed artifact hashes;
3. read `connected-284.json`, independently verify explicit assertion outcomes and per-REQ scenario status, **not only top-level 7/7**; require at least 38 concrete passing assertions;
4. require REQ-01 proof of one SKU-only Offer PATCH, zero price/stock writes, independent GET+SQL, unchanged 3/7 positions, reservations, price and outbox;
5. require REQ-03 proof of two distinct sessions, old price observed by B, owning Pricing write by A, SKU-only PATCH by B with zero Pricing writes, independent GET+SQL showing BOTH new SKU and price;
6. require REQ-02/04/05/06/07 proof from exact scenario and supporting data, including real provider for success and controlled substitution only for error branches;
7. require verified disposable database removal and no source-code change vs reviewed SHA; recognize legacy Cursor gate as KNOWN_FORMAT_INCOMPATIBILITY, not product PASS/FAIL;
8. fail if any evidence path/hash mismatches or an assertion is absent. Script MUST have its own small self-test demonstrating that tampering with an evidence file, deleting a REQ, or changing a required expected fact returns nonzero; restore every fixture afterward.
9. emit machine-readable pass/fail with explicit exact reasons, and nonzero exit on failure. Script does not certify truth of externally authored logs by itself; EVIDENCE-REVIEW must discuss this limitation.

Keep verification deterministic, no network/service writes. Do NOT invent results, assertions, ids, URLs or logs.

## 4 — Independent review (not loop)
Read the actual Playwright harness `connected-284.cjs` and evaluate whether the assertions truly execute UI through actual served Next + owning Host/isolated PostgreSQL and whether SQL/GET results are genuinely independent. Inspect negative tests for forgery/control limitations, cleanup, and real-time Price concurrency order. Cross-check actual source code route. If any material doubt remains, report BLOCKED with one precise missing proof. Do not rubber-stamp the worker JSON and do not claim to rerun the expired disposable environment.

No rerun of the 38 checks if existing authentic evidence is sufficient. If evidence is materially insufficient, a single bounded rerun may be proposed but **not performed without a new explicit authorization**, because the original isolated runtime is gone and this task is no-write acceptance-only. There is no product repair round in this task.

## 5 — Proposed OpenCode method only after PASS
If and only if independent review and OpenCode checker pass, author `PROPOSED-OPEN-CODE-DELIVERY-METHOD.md` marked `CANDIDATE — NOT ADOPTED`, explaining:
- architect must preflight source and test-chain feasibility BEFORE issuing OpenCode task;
- fixed scope, observable customer workflow and exact domain ownership; OpenSpec strict before code;
- actual Impeccable critique and after-check for tasks changing UI (not installed/read-only as evidence);
- Playwright real Chromium plus connected owning backend/provider for persistence claims;
- separate development-debug loop from one post-acceptance repair and explicit stop policy;
- independent evidence, exact version hashes, fail-closed result; no circular self-attestation;
- preserve Cursor governance untouched; OpenCode maintains its **own** deterministic contract and checker;
- worker PASS != architect ACCEPT != owner acceptance; bounded STOP_FOR_ARCHITECT;
- future OpenCode task issuance must be schema-checked **before** dispatch, including negative-control tests.

**Do not automatically adopt it** into `AGENTS.md`, `docs/architecture/` or pipeline. Only the architect and user may authorize a later OpenCode-specific standards promotion after independent acceptance. A failed task must not publish an adopted method.

## 6 — Delivery limits / stop
Only docs/tools under this task folder may be created/changed; no source, schema, migration, live data or config changes. No Cursor changes. `git diff --name-only <expected-base>..HEAD` must show only this task folder (aside from pre-existing records which must be reported), and prove the legacy Cursor artifacts retain their original blob SHAs. No force push. Record exact commit/HEAD parity, evidence hash, checker execution output, independent review outcome and next action. Only `PASS / READY_FOR_ARCHITECT_REVIEW` if all checks pass and candidate is clearly noncanonical. Otherwise `BLOCKED / NOT_READY` with no new follow-up task. Always STOP_FOR_ARCHITECT; no automatic next task.

END_TOOBA_TASK
