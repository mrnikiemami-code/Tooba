PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-RETURNS-AMSC-001-W3-R1
Parent-Task: TB-TMAR-RETURNS-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Skill: tooba-architecture-certify
Mode: BOUNDED_CERT_GUARD_PROOF_REPAIR

STARTING HEAD
fa535eca3ff41f0fcdfb0e8d1ccc6b62a1c0e01e

AUTHORITIES
W0 Analyze: f5c5a6db110f46facde788ce0412f022bff9819a
W1 Migrate: 0a5738645b9b55fd4bd159ef9568acf573588a96
W2 Structure: 6cab1b873d25d34bb941cc8d51777109cca3f2d4
W3 historical certification: fa535eca3ff41f0fcdfb0e8d1ccc6b62a1c0e01e
Skill: .cursor/skills/tooba-architecture-certify/SKILL.md section 6a (current HEAD authority)

PRECHECK

Fetch origin, verify main and known safe tree; HEAD must equal origin/main, or perform ONLY a provably safe fast-forward with unchanged W3 ancestry. Never reset/rebase/stash/force-push.
Read AGENTS.md, Recovery SoT, architecture locks, W3 evidence, four skills and the real route/handler/validator sources.
One task only. Do not start another module.

AUDIT FINDING
W3 ReturnsModuleAmsc001W3CertGuardTests currently counts 11 route mappings and 11 sender.Send call sites, and separately asserts that 11 hard-coded request names have declarations/handlers. It DOES NOT independently derive the actual dispatched request-type set and compare it for exact equality with the classified request set. Thus a changed/replaced dispatch can preserve counts while invalidating the matrix. This violates the NEW Certify section 6a durable set-equality hard gate, even though a manual audit found the current 11 requests consistent. Do not claim a production defect without fresh evidence.

GOAL
Repair ONLY the durable verification gap, then independently verify Returns' real HTTP → request → handler → validator/policy provenance and certification truth. Preserve unchanged behavior, already accepted structural locks, historical W3 evidence and other modules.

REQUIRED

From disk, derive all shipped Returns endpoint verb+path pairs and each actual ISender.Send request type; support both Send(new T(...)) and Send(variable) by tracing local construction. Do not infer from the W3 hard-coded request arrays or static 11 count. Prove one-to-one 11 route mappings → 11 sends → 11 actual distinct requests → 11 matching handlers. Inspect all three audience endpoint files and any registration call sites.
Independently derive validator targets from concrete AbstractValidator<T> definitions, and compare ACTUAL endpoint-request set to the exact partition: 4 VALIDATOR_REQUIRED (CreateReturnCommand, ApproveReturnCommand, RejectReturnCommand, QueryAdminReturnsGridQuery) + 7 NO_VALIDATOR_REQUIRED (ListCustomerReturnsQuery, GetCustomerReturnQuery, ListSellerReturnsQuery, GetSellerReturnQuery, ListAdminReturnsQuery, GetAdminReturnQuery, RetryReturnRefundCommand). Reject any missing/duplicate/orphan/unclassified dispatch.
Recheck real provenance for all seven exemptions, including route :guid binding, server-derived actors, absent body/query input, and actual runtime GridQuery policy/error mapping for QueryAdminReturnsGridQuery. An optional or validated-by-HTTP value is not intrinsically server-derived. If any exemption is unsupported, stop as CERTIFICATION_BLOCKED; do not repair production.
Strengthen the existing Returns W3 certification guard or add a narrowly scoped W3-R1 guard with machine-verifiable set equality derived from the actual endpoint code, not mere equal counts or assertions copied from SoT. For the variable-dispatch case, use a deterministic explicit source-tracing strategy with a fail-closed guard when extraction is ambiguous. Include a focused negative/mutation demonstration that replacing one endpoint-dispatched request with another while preserving the overall 11 Send count fails the guard, with no mutation committed to main.
Verify validators' discoverability in DI/MediatR pipeline, and existing focused behavior tests. Recheck no raw API results, schema/Host/Contracts-only boundary regression, and manifest/SoT claims, using focused evidence only.
Record findings and a short W3-R1 checkpoint/evidence. If all pass, record a new Returns module-local W3-R1 verification block without falsifying or silently rewriting historical W3, and record actual commit only in Worker Result (do not self-reference its SHA). Preserve global Host checkpoint and automaticNextImplementationTask NONE.

IF ANY NEW ACTUAL PRODUCTION DEFECT OR UNSUPPORTED CLASSIFICATION

Report CERTIFICATION_BLOCKED and STOP. Do not modify production or claim ARCHITECT ACCEPT. Describe exact route/request/source, actual failure and bounded recommendation.

ALLOWED

Returns-specific architecture certification guard(s) / focused tests
Returns W3-R1 evidence, corresponding task artifact
Minimal Returns-only SoT/recovery status addendum if genuinely supported
Minimal Returns manifest note only if essential to reconcile certification truth

FORBIDDEN

Production code, other modules, project/solution/folder moves, schema/migrations
Global certified-module list or accepted Host checkpoint mutation
Weakening guards, widening baselines, silencing existing test failures
Claiming deployment/runtime microservice proof
Automatic next task / next module

VALIDATE
Focused Returns W1/W2/W3/W3-R1 guards, Tooba.Returns.Tests and ErrorCatalogUniqueCodeGuardTests. Compare any relevant failing wider guard names to W3 baseline; do not run repeated full suites. Report exact PASS/FAIL/SKIP. Validate both JSON files parse. Inspect final git diff for scope, commit, push, re-fetch and verify HEAD == origin/main and safe working tree.

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-RETURNS-AMSC-001-W3-R1
Parent-Task: TB-TMAR-RETURNS-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | CERTIFICATION_BLOCKED
Summary: <short grounded result>
Route-Request-Set-State: EXACT_11_MATCH | CONFLICT
Handler-Set-State: EXACT_11_MATCH | CONFLICT
Validator-Matrix-State: EXHAUSTIVE_4_REQUIRED_7_NO_VALIDATOR_REQUIRED | CONFLICT
Input-Provenance-State: VERIFIED | CONFLICT
Mutation-Negative-Guard-State: PASS | FAIL
Boundary-State: CONTRACTS_ONLY | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Host-Checkpoint-State: PRESERVED | REGRESSED
Production-Code-Changed-State: ZERO | NONZERO
Guards-Weakened-State: NONE | NONZERO
Baselines-Widened-State: NONE | NONZERO
Focused-Validation-State: <counts>
Commit-SHA: <actual sha>
HEAD-Equals-Origin-Main: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_RETURNS_AMSC_001_W3_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP. No automatic recovery follow-up or next module. Architect reviews the result and reconciles final SHA.
END_TOOBA_TASK