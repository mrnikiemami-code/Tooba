TB-TMAR-STORY-AMSC-001-W3-R2 — Bounded Route-to-Dispatch Certification Guard Repair
Plain text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-STORY-AMSC-001-W3-R2
Parent-Task: TB-TMAR-STORY-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Skill: tooba-architecture-certify
Mode: BOUNDED_DURABLE_ROUTE_REQUEST_SET_EQUALITY_PROOF
Target: src/backend/Modules/Story
Starting-HEAD: 47284caad070b28ae79e803946dd19aed4b1678c
Expected-Branch: main
Automatic-Next-Implementation-Task: NONE
Objective

Repair only the durable certification-proof gap in the current Story AMSC chain. The existing W3 guard counts 25 routes, 25 sender.Send calls and 16 validators; the older StoryModuleAmcW5ValidatorGuardTests compares the set of newly constructed requests against a 25-row manifest. Neither demonstrates a durable exact route + verb + mapped handler → actual dispatched request relation. A within-set dispatch replacement could evade those checks. Do not assume a production defect exists. The authoritative current architecture remains W3, with W3-R1 Recovery already reconciled.

Preflight / strict STOP conditions
git fetch origin; verify main is clean, HEAD == origin/main == 47284caad070b28ae79e803946dd19aed4b1678c, and W0 0c73390a3211e0ee9057e9234d62d3e4f14b5e4e → W1 2a09e7bb7f1ab687435007951160b4bcfefb4c18 → W2 4cd9a6cc543ccd307d775dfe703459de7b12c95d → W3 39ab324e9c57e342516202bdd8df95953dda6439 → W3-R1 47284caa... ancestry holds. If remote moved or worktree has unrelated edits, STOP and report; do not reset, stash, rebase, force push or silently rebase the task.
Read AGENTS.md, .cursor/skills/tooba-architecture-certify/SKILL.md (especially §6a), current Story AMSC W0–W3/W3-R1 evidence, StoryModuleAmsc001W3CertGuardTests, StoryModuleAmsc001W3R1RecoveryGuardTests, StoryModuleAmcW5ValidatorGuardTests, Story endpoint mappings and validators. Preserve the known 25/16/9 classification unless disk evidence contradicts it.
If the audit reveals an actual incorrect production dispatch, unclassified client input, missing validator or forbidden boundary, do not repair production under this task: return CERTIFICATION_REVIEW_BLOCKED, enumerate exact paths/lines and proposed bounded follow-up, and STOP.
Allowed work — test/evidence only
Add a narrowly scoped, independently named durable Host test guard, e.g. StoryModuleAmsc001W3R2SetEqualityGuardTests.cs. Derive the real active audience prefixes, every shipped MapGet/MapPost/MapPut/MapPatch/MapDelete verb+path and mapped handler from Story endpoint source. Trace each handler to its actual ISender.Send argument, including Send(new T(...)) and variable constructs such as var command = new T(...); Send(command, ...). If a request cannot be unambiguously traced, fail closed, not assumed compliant. Reject unmapped, duplicate or multi-dispatch routes; do not silently ignore unfamiliar mapping/dispatch syntax.
Build a verified 25-row expected map of audience + verb + full route + handler + dispatched IRequest type from disk. Prove exact equality of this map against independently parsed shipped code; assert 25 routes, 25 sends, 25 distinct actual request types (only if disk verifies distinctness), one matching real handler per type, no orphans/duplicates and absence of Host-owned Story routes. Do not rely solely on set equality or frozen count.
Cross-check the resulting actual dispatched-request set against the 16 VALIDATOR_REQUIRED + 9 NO_VALIDATOR_REQUIRED matrix; derive concrete AbstractValidator<T> targets, verify actual registration via AddToobaCqrsFoundation / DI / ValidationBehavior (not just string checks), and verify non-circular input-provenance reasons for all nine exemptions. Check nullable/optional locale/market and similar caller-controlled inputs are not incorrectly exempted; no new code or descriptor ownership.
Add a repeatable negative mutation test in memory that replaces one route's dispatched request with another already present in the 25-type set, preserving route count, send count and type-set membership. Demonstrate legacy guards' blind spot and the new exact route-map gate failure. An optional on-disk mutation proof must restore bytes in finally, verify SHA-256, and leave no uncommitted production modification. Mutation must never be committed.
Keep existing W1/W2/W3/W3-R1 guards and their assertions intact. No weakening or baseline widening. Record precise provenance, the derived 25-row map, mutation proof, test command/output, lineage and verdict in docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3-R2/. Add a narrowly scoped storyAmsc001W3R2 SoT entry and module-local Master Recovery checkpoint only if required to record truth, preserving all historical Story/Host entries and global pointers; no self-referential commit placeholders. Do not alter the manifest unless correcting a separately proven false claim, in which case STOP for Architect review rather than exceeding scope.
Forbidden
ZERO changes to production under src/backend/Modules/Story/** or Host/Tooba.Host/**; ZERO schema, migrations, runtime DI, routes, DTOs, behavior, localization, contracts, project graph, solution, frontend, other modules or architecture skills.
No new validator unless a future separately approved production-repair task explicitly permits it.
No rewriting W0–W3 or W3-R1 evidence; no change to HOST_ROOT_FINAL_CERTIFIED, root lastAcceptedTask/Commit, root workflowStop, or automaticNextImplementationTask = NONE.
Do not turn existing unrelated HostGrid/TmarDurable/Catalog namespace/.tmp-baseline failures green by relaxing guards or extending baselines.
Verification and acceptance gates
New durable guard passes its own tests; existing Story AMSC/AMC/recovery guards and focused Story/ErrorCatalog suites have no new failures; exact pre-existing failure-name set compared to clean starting HEAD for broader tests, not just totals.
Negative mutation must fail the new guard while preserving counts/type-set; unmodified source must pass. Confirm post-mutation source hash identical to baseline.
git diff confirms zero Production changes; guard-only plus task/evidence/optional module-local SoT or Recovery metadata. JSON parses; module lock remains COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 unless a real defect is discovered.
Commit and push one bounded, independently reviewable change to main only after gates pass; confirm HEAD == origin/main, provide full 40-char SHA and changed-file list.
Worker result contract
Plain text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-STORY-AMSC-001-W3-R2
Parent-Task: TB-TMAR-STORY-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | CERTIFICATION_REVIEW_BLOCKED
Route-Request-Exact-Map-State: ...
Request-Validator-Matrix-State: ...
Exemption-Provenance-State: ...
Mutation-Negative-Guard-State: ...
Production-Code-Changed-State: ZERO
Guards-Weakened-State: NONE
Baselines-Widened-State: NONE
Focused-Validation-State: ...
Unrelated-Baseline-Failure-Names: ...
Evidence-Path: docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3-R2/
Commit-SHA: <full SHA if PASS and pushed>
HEAD-Equals-Origin-Main: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_STORY_AMSC_001_W3_R2
STOP
END_TOOBA_WORKER_RESULT

Worker PASS is not Architect ACCEPT. Do not start another module or follow-up task automatically.