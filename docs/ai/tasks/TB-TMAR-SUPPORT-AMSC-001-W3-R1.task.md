PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-SUPPORT-AMSC-001-W3-R1
Parent-Task: TB-TMAR-SUPPORT-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Skill: tooba-architecture-certify
Mode: BOUNDED_SUPPORT_201_RESPONSE_CONTRACT_REVIEW
Repo: mrnikiemami-code/Tooba
Branch: main
Starting-HEAD: c632da653d4a34cc6172313a9ef3ef61346e99c0
Automatic-Next-Implementation-Task: NONE

OBJECTIVE
Resolve the certification blocker involving two raw 201 responses in Support's customer/seller ticket-creation endpoints. Determine whether these responses legitimately require a documented compatibility exception, or can be migrated to the canonical ApiResponseFactory without altering the HTTP contract. Do not assume that an Identity precedent is automatically an architectural exemption.

PREFLIGHT — STRICT STOP

Read AGENTS.md, current tooba-architecture-certify SKILL.md (especially canonical API results), Support W0-W3 evidence, SoT, manifest and recovery checkpoint.
git fetch origin; require branch main, clean working tree, HEAD == origin/main == Starting-HEAD. Verify W0 567ac400, W1 5e8c86ef, W2 aaa15b03 and W3 c632da65 ancestry. If any precondition fails, STOP and report; do not reset, rebase, stash or force-push.
One task only. Do not advance to another module or AMSC wave.

MANDATORY INVESTIGATION
A. Inspect the exact customer/seller Create-ticket endpoint implementations and their Result<T> return types. Enumerate status, response body JSON shape, Content-Type, Location header, error behavior and consumers/tests that depend on the current 201 contract. Distinguish verified facts from inferred client expectations.
B. Inspect actual ApiResponseFactory.From<T> and Created<T> implementation; compare behavior of Results.Json(result.Value, statusCode: 201), api.From(result), and api.Created(location, result). Do not invent a Location URI or silently alter 201 to 200/204, body envelope, route, error shape or headers.
C. Examine relevant in-repo existing canonical 201 behavior (including Identity if cited), current skill rule and explicit exception policy, if any. Precedent alone does not override the certify rule.
D. Determine whether a safe canonical 201 mapping exists already, with identical observable contract, or whether a new factory capability / approved architecture exception would be required. Do not implement new framework capability in this task.
E. Check that the existing 17-route / 9-validator + 8-exemption proof remains intact; this task is not permission to reopen unrelated architecture gates.

DECISION / STOP GATE

If an existing canonical factory method demonstrably preserves ALL observed HTTP behavior and no new production mechanism is needed: document an exact minimal proposed repair and its test plan, but DO NOT change production in this task; return READY_FOR_BOUNDED_IMPLEMENTATION and STOP for Architect approval.
If the raw 201 is a necessary compatibility exception: document the actual in-repo authority that permits it, precise scope (only two successful create responses), negative constraints and durable guard evidence. If no explicit authority exists, return ARCHITECT_DECISION_REQUIRED; do not self-authorize an exception.
If evidence is insufficient, behavioral equivalence is uncertain, or any contract/regression risk is found: return CERTIFICATION_REVIEW_BLOCKED with the exact facts needed. No assumptions or opportunistic repair.

ALLOWED CHANGES

Add only task receipt, scoped analysis/evidence documents, and an additive module-local recovery note if necessary. Do not mark final certification ACCEPTED, loosen the existing guard, or alter existing history/SoT truth just to make a check green.
NO production code edits; NO shared ApiResponseFactory edits; NO schema/migration/endpoint/contract changes; NO tests weakened, baselines widened or global pin changes. Tests may be added only if entirely test-only and necessary to establish existing behavior without altering production; prefer existing tests/evidence.
Do not change frontend, unrelated modules, Host production, root checkpoints or automatic next task.

REQUIRED EVIDENCE
Write docs/architecture/evidence/TB-TMAR-SUPPORT-AMSC-001-W3-R1/ with a precise 201-response contract comparison, source locations, consumer/test evidence, risk table, canonical-mechanism verdict and explicitly bounded next decision. State what was inspected, what was tested and what remains unknown. Run focused read-only tests if feasible; report command and exact pass/fail/skip counts. Separate pre-existing failures from regressions with a grounded baseline; never invent test results.

COMMIT / PUSH
If and only if the bounded evidence is complete and preflight still holds, commit scoped evidence as one coherent commit, push normally to origin/main after checking origin has not advanced, and report full SHA plus HEAD==origin/main. If push cannot be made safely, STOP and report without force.

BRIDGE RESULT
Return PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 / BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT with Task-ID, Parent-Task, status (READY_FOR_BOUNDED_IMPLEMENTATION / ARCHITECT_DECISION_REQUIRED / CERTIFICATION_REVIEW_BLOCKED), verified 201 contract, decision rationale, evidence path, changed files, production change ZERO, guard/baseline preservation, commit SHA (if any), global Host checkpoint preserved, workflow stop USER_REVIEW_SUPPORT_AMSC_001_W3_R1, automaticNextImplementationTask NONE, and STOP.
END_TOOBA_TASK
