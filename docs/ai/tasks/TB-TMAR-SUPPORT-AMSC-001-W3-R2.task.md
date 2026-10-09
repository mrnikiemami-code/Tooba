PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
TASK-ID: TB-TMAR-SUPPORT-AMSC-001-W3-R2
PARENT-TASK: TB-TMAR-SUPPORT-AMSC-001-W3-R1
CHANNEL: tooba-main
WORKER-ID: tooba-worker-01
AGENT-TYPE: cursor
SKILL: tooba-architecture-certify
MODE: BOUNDED_CANONICAL_201_CREATED_REPAIR
REPOSITORY: mrnikiemami-code/Tooba
BRANCH: main
REQUIRED-STARTING-HEAD: e097a5d5a6c497b9590362a6bc52f3357cc9c08f

ARCHITECT DECISION (EXPLICIT AUTHORITY): OPTION A APPROVED.
Replace the TWO Support ticket-creation success mappings using Results.Json(result.Value, statusCode: StatusCodes.Status201Created) with the existing canonical ApiResponseFactory.Created<T>(location, result). The only permitted wire-visible change is an additive Location header referencing the actual returned TicketId. Preserve status 201, raw TicketSnapshotDto response JSON, Content-Type, and all failure behavior. DO NOT adopt Option B or create a raw-201 exception.

PRECHECK — FAIL CLOSED:

Read AGENTS.md, the certify skill, W3-R1 review.md and proposed-repair.md, existing W3 guards, both endpoints and ApiResponseFactory implementation.
Fetch origin, confirm branch main and HEAD == origin/main == REQUIRED-STARTING-HEAD. Confirm clean tracked working tree; preserve any unrelated untracked material. If HEAD differs or task conflicts with later changes, STOP and report; never reset, stash, rebase, force-push or silently adapt the baseline.
Verify W0 567ac400, W1 5e8c86ef, W2 aaa15b03, W3 c632da65 and W3-R1 e097a5d5 are ancestors. Confirm two and only two raw-201 create mappings and verify actual GET-by-id route templates and TicketSnapshotDto.TicketId type before constructing Location.

ALLOWED PRODUCTION CHANGES — EXACT TWO ENDPOINT FILES ONLY:

src/backend/Modules/Support/Tooba.Support.Endpoints/Customer/SupportCustomerEndpoints.cs
src/backend/Modules/Support/Tooba.Support.Endpoints/Seller/SupportSellerEndpoints.cs
Change success return only, to api.Created with canonical fully-qualified resource paths rooted in the actual matching customer/seller GET ticket routes and result.Value.TicketId. Do not add a fake URI, alter route mappings, alter DTOs, modify error handling or change HTTP 201 to 200/204. Keep the existing Result<T> result intact for factory mapping. Do not touch any other production file or shared ApiResponseFactory.

ALLOWED NON-PRODUCTION:

Add focused Support endpoint response-contract tests (prefer executable behavioral tests) for BOTH creation paths: 201, exact raw TicketSnapshotDto body shape, application/json, correct customer/seller Location with returned TicketId, no Location on failures, and unchanged canonical ProblemDetails/error code behavior. Reuse existing test harness where practical; do not introduce live database dependencies merely for this repair.
Tighten SupportModuleAmsc001W3CertGuardTests to assert exactly two canonical api.Created call sites, the correct per-audience route URI construction with TicketId, and ZERO raw Results.Json or Status201Created in Support endpoint production. Keep every unrelated W3 assertion, route/request matrix and validator checks. Do not weaken guards, expand baselines or use a count-only substitute for path correctness.
Add focused W3-R2 evidence, additive module-local Master Recovery checkpoint and additive SoT W3-R2 repair record without rewriting historical W3/W3-R1 evidence or global Host pointers. Avoid self-referential pending commit placeholders. Include task.md and receipt if required by repo protocol.

REQUIRED VERIFICATION:

Build relevant projects and run the focused Support architecture/test suites, including W1/W2/W3, route-to-request set equality, validator DI and ErrorCatalog guard.
Verify observable response contract at the result level, not only static source-string assertions. If feasible, use the in-repo HTTP harness to prove response status/body/headers for both audiences and failures. If behavioral integration test cannot be constructed within this bounded scope, STOP as VERIFICATION_BLOCKED rather than claim exact wire equivalence.
Compare global failing test names against the W3-R1 baseline; no newly failing fact is acceptable. Existing unrelated baseline failures are not permission to widen baselines.
git diff --check; verify only exact two allowed production endpoint files changed, no schema/migration, no shared factory, no frontend, no global Host checkpoint change. Ensure no test/evidence temporary mutations are committed.
Commit and push once after all gates PASS. Confirm HEAD == origin/main and clean tracked tree.

STOP / REFUSAL CONDITIONS:

Inability to derive truthful per-audience Location from the real GET route and returned TicketId.
Any change beyond additive Location in success response, failure response, or underlying data contract.
Unproven 201/body/content-type preservation, missing executable tests, new failures, unexpected provenance or any need to alter shared framework / other modules.
HEAD mismatch, untrusted/unresolved working tree, test weakening, baseline expansion or proposed use of a raw-201 exception.
On STOP do not apply an unauthorized workaround: return evidence and request Architect decision.

FINAL WORKER REPORT MUST INCLUDE:
Task-ID, Parent-Task, Status PASS or BLOCKED, Starting-HEAD, final commit SHA, changed-file inventory, exact customer/seller Location patterns, explicit status/body/content-type/failure parity and additive-Location proof, build/test totals, pre-existing failure comparison, zero-out-of-scope-change proof, preserved Host/recovery checkpoint, evidence paths, HEAD == origin/main, workflowStop USER_REVIEW_SUPPORT_AMSC_001_W3_R2, automaticNextImplementationTask NONE, STOP.

NO NEXT IMPLEMENTATION TASK WITHOUT ARCHITECT AUTHORIZATION.
