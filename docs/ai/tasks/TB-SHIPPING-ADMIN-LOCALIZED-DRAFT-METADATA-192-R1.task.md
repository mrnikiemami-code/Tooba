PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1
Parent-Task: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 87ae74549d34e941a9c7d76ff61eb1f28ce51568
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_PRESENTATION_ENDPOINT_AND_DYNAMIC_LANGUAGE_PROOF_COMPLETION
Hard-Timebox: 35 minutes HARD INCLUDING cleanup, commit, push and report; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Complete the already-requested TB-192 proof for real presentation GET/PATCH, dynamic language/default transitions, authorizer/validation behavior and the new translation storage constraints. The feature exists, but the two new facts call the directory directly and do not establish the full claimed PASS.
This is focused TEST/EVIDENCE completion plus one XML correction; do not reimplement the feature or start the next product slice.

MANDATORY FIRST CHECK
Fetch main; inspect branch/HEAD/dirty/untracked/stashes and preserve unrelated work. Require HEAD == origin/main == Expected-Base-SHA before edits. A safe strictly-behind branch may fast-forward only to the exact base. Mismatch/divergence/overlapping dirty work => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push.
Read AGENTS.md, docs/ai/AGENT-SKILLS.md, full .cursor/skills/tooba-backend-feature/SKILL.md, current Shipping recovery/rules, TB-192 receipt/result and docs/architecture/evidence/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192/ARCHITECT-REVIEW.md.
Read actual presentation routes/query/command/validators/workflow, context projection/source, owner authorizer, directory/domain/storage mappings and existing endpoint/relational harness before adding tests. No findings-register or unrelated-module scan, no full AMSC cycle.

PROOF GAP / AUTHORIZED SCOPE
TB-192 added Presentation_patch_touches_only_the_selected_language... and A_same_revision_presentation_patch_racing_a_rate_write..., both calling ShippingAuthoringDraftDirectory with manually supplied active-language sets. Existing one/two/three-plus TB-188 context tests are useful reused proof of the projection but do not execute presentation PATCH/GET. Existing endpoint changes only updated route counts.
Preserve those valid facts and historic 579/579; do not relabel them as the missing API/registry proof.
Allowed executable changes: focused Shipping.Tests only, including small cohesive test helpers needed to reuse existing real composition harness. Do not add more unrelated cases to the already very large relational test file; prefer capability-scoped test files, with minimal test-only helper extraction if needed. No new package/project.
Allowed production edit: COMMENT/XML ONLY in ShippingAuthoringDraft.PreflightPresentation. Its remarks claim unchanged inactive-language entries may be accepted, but executable code correctly rejects every included inactive LanguageId. Explain active-only PATCH plus passive retention; do not introduce an inactive-write exception.
If new tests demonstrate an actual production defect, commit coherent tests/evidence, report PARTIAL with exact failing behavior and minimal proposed fix, then STOP_FOR_ARCHITECT. No executable production repair, migration, new permission/route/DTO, fallback resolver or test bypass is authorized here.

EXECUTED PRESENTATION API / REGISTRY MATRIX

REAL COMPOSITION — NOT A DIRECTORY-ONLY SURROGATE
Exercise actual /v1/admin and /v1/seller/.../authoring-drafts/{draftId}/presentation GET/PATCH delegates with the real ShippingAuthoringOwnerAuthorizer, ISender, ValidationBehavior, presentation handlers/projection, ShippingAuthoringDraftDirectory, ApiResponseFactory and error catalog.
Reuse the established in-process HTTP/delegate harness. Controlled platform/auth/Contracts fixtures are allowed and labelled; do not replace the Shipping authorizer/handler with a successful stub or add production auth/test endpoints.
Instrument authorized dispatch and actual handler execution separately: authorization failure occurs BEFORE ISender; malformed body/shape validation may dispatch into MediatR but must fail BEFORE the handler/foreign reads/writes. Do not claim zero Send calls when only zero handler executions was measured.

ONE / TWO / THREE-PLUS ACTIVE LANGUAGES THROUGH PRESENTATION
Use isolated mutable Contracts/context fixtures with real LanguageIds and truthful metadata. Cases:

one active NON-fa language, e.g. a non-fa default;
two active languages with mixed RTL/LTR;
three-plus active languages, default NOT first, plus stored text for an inactive language.
In each case execute presentation PATCH then GET; assert exact returned raw translations, active registry identity/order/default and HasDefaultLanguageName, not just that the generic context query succeeds.
At least one representative case must compose the actual ShippingAuthoringContextSource with a controlled ILanguageLookup / trusted StoreContext fixture to demonstrate the lawful adapter path. Do not invent DB rows or claim fixtures are a live merchant Localization database.
Prove editing one language leaves every other language's stored bytes, group/rate/currency snapshot unchanged.
REAL CONFIGURATION TRANSITIONS, CLEAR AND NO-OP
Against the SAME stored draft:
deactivate a previously written language; GET retains its raw text but excludes it from active registry; PATCH that inactive id rejects and changes nothing;
reactivate it; GET exposes the EXACT previous text, with no auto-translation;
change the actual default (including one with no stored name) and order; HasDefaultLanguageName follows actual stored/default identity, never fallback position;
explicitly clear ONLY the selected-language description and group title; other entries/groups remain byte-identical;
[]/[] and an identical active entry are true no-ops: root revision/UpdatedAt unchanged; stale expectedRevision still gets canonical conflict.
Run GET/PATCH with different interface cultures (fa/en) under a safely restored/isolated test culture scope; assert raw content and configured identities/rules remain invariant. Interface locale must never supply content language.
AUTHORIZATION / MALFORMED INPUT / PREFLIGHT
Real routes for both audiences:
admin and Marketplace seller own GET/PATCH succeed;
view-only PATCH rejected before dispatch; another store/seller cannot GET/PATCH foreign draft; foreign group title rejects with no disclosure/mutation;
SingleStore seller GET/PATCH remains canonical unavailable before seller resolution/dispatch.
Through the real body reader and validation pipeline, cover omitted/null methodTranslations or groupTitles, null elements, duplicate language or (group,language), empty/unknown/inactive language ID, blank/over-length name/description/title. Negative cases must assert stable canonical error code/status and unchanged text/rules/revision, not any generic exception.
Use a changed valid FIRST entry followed by bad LAST group/language/text to prove no partial edit. At least one direct same-instance/context assertion verifies all values/revision/timestamp unchanged and a later SaveChanges cannot leak earlier changes; no assertion based solely on a fresh empty fake store.

REAL POSTGRES STORAGE / RETENTION
Reuse existing parsed exact target and provider harness on a fresh task-owned PostgreSQL 17 container. Existing TB-192 roundtrip/race facts may be reused unchanged with explicit source identity; no exhaustive repetition of unrelated proof.
Execute a small independent raw/EF database matrix on the two NEW translation tables:

duplicate (draft,language) and (group,language) rejected by their real unique/PK constraint (23505 + owned constraint identity);
invalid own parent FK rejected (23503); do not create a foreign Localization FK;
NULL, ordinary blank-space-only required text and over-length values rejected by the appropriate owned not-null/CHECK constraint (23502/23514, correct constraint/column identity);
valid Unicode method text and group title persist on NEW-context readback.
Use parameterized test-only SQL and rollback each failing attempt so one aborted statement cannot poison another. Do not mistake migration/script presence for executed negative proof.
Execute retained-group REORDER and a rate edit AFTER titles/method text were saved; verify exact preserved method/group texts. Existing group-removal cascade fact may be reused by exact source identity; do not claim it also tested reorder if it did not.
No schema/Domain change to force these proofs. If a raw case exposes an actual invariant mismatch, report the exact case as PARTIAL instead of changing published migrations or widening the task.

ISOLATION / TIME / COMMAND LIMITS
Record UTC claim/deadline plus monotonic elapsed time; reserve final 5 minutes. Start no new implementation/test after minute 30. Every command <=180 seconds under real timeout; no blocking wait >60 seconds, detached test or test/repair loop.
Setup <=6 minutes using already available image/tools only. Verify free loopback port, exact new task-owned container, parsed host/port/database/user BEFORE connection and live database/user before writes. Expected existing fixture target is 127.0.0.1:55432 / shipping_tb190r1 / tb190r1; never shared :5432/tooba_alpha/store-alpha. No credential logs, package/image/browser download/install, shared server restart or Host startup migrator.
Build only affected Shipping.Tests dependencies with isolated task output; run new targeted tests, then at most ONE final whole Shipping suite if targeted tests pass and time remains, with required DB still live. NO Tooba.Host.Tests, solution build/test, whole Order/other-module suite, exclusions/skips hiding required cases or baseline refresh.
Cleanup exact task container/database/listener/output before reporting; shared :3000/:3419/:5088/:5432, bin/obj/.next/cache, Redis/RabbitMQ and unrelated dirty/untracked/stashes stay untouched. No reset/rebase/stash/force-push.

EVIDENCE / ACCEPTANCE / STOP
Map each claimed case to an ACTUALLY EXECUTED test and outcome. Distinguish new proof, unchanged reused proof and source-only/live-merchant limitations. Append explicit correction to TB-192 RESULT.md about the original two facts' scope, preserving historic 579/579. Do not retroactively claim unexecuted tests passed.
Persist exact received task in docs/ai/tasks/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1.task.md after claim. Evidence under docs/architecture/evidence/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1/: RESULT.md, endpoint/language/auth/validation case matrix, raw storage negatives, retained-text comparisons, commands/counts/deadline/cleanup. Update recovery to actual worker outcome, no Architect ACCEPT.
PASS only for executed real presentation API/registry-transition/auth/validation proof, PostgreSQL storage/retention cases, precise unchanged production scope and cleanup inside the deadline. PARTIAL for missing required proof or demonstrated production flaw; BLOCKED for architecture/isolation conflict.
No frontend, new product feature, schedule/taxonomy/mode, runtime evaluator/quote/activation/financial/deployment or certification work. Runtime merchandise basis/measurement and live merchant registry/browser gates stay open. TB-185 paused. Do not issue TB-193 or any automatic next task. No dummy/probe POST to Bridge /api/results.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1
Parent-Task: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_PRESENTATION_ENDPOINT_AND_DYNAMIC_LANGUAGE_PROOF_COMPLETION
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 87ae74549d34e941a9c7d76ff61eb1f28ce51568
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Real-Presentation-Endpoint-Pipeline-And-Authorizer: ...
OneTwoThreePlus-Presentation-Matrix-And-Adapter-Composition: ...
Deactivate-Reactivate-Default-Change-And-Raw-Text-Retention: ...
Selected-Clear-NoOp-And-Interface-Culture-Invariance: ...
Owner-Permission-SingleStore-And-PreHandler-Denials: ...
Malformed-Input-And-Same-Context-Preflight: ...
Translation-Storage-Unique-FK-NotNull-Check-Negatives: ...
Retained-Group-Reorder-And-RateEdit-Text-Preservation: ...
Reused-Versus-New-Executed-Proof: ...
Production-Executable-Schema-Contract-Changes: NONE
Configured-Registry-Source-Versus-Executed-Proof: ...
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: NONE
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: ...
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK