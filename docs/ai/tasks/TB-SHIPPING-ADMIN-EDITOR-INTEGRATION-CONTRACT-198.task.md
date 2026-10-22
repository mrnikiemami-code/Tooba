PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198
Parent-Task: TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_ATOMIC_EDITOR_INTEGRATION_CONTRACT_SOURCE_ANALYSIS_ONLY
Expected-Base-SHA: e33ef067b2705b98a82ee977f591eba8c860eded
Hard-Timebox-Minutes: 20
Stop-New-Analysis-At-Minute: 15
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Objective and current checkpoint

197 is architect-accepted for its bounded owner-local type-assignment repair. Read its ARCHITECT-REVIEW.md and the CURRENT checkpoint in AGENTS.md/recovery; historical worker PARTIAL process deviations remain recorded. Do not replay 196/197 proof or seek a whole-module certification.

Produce ONE concrete, source-backed integration proposal for connecting the approved Shipping editor to real backend facts and saving the entire edited method atomically. This is source analysis and documentation ONLY. It authorizes no implementation, runtime contract change, approved-decision amendment, deployment or activation.

The reason for this slice is specific: SHIPPING-ADMIN-RULES-DECISION section 9 requires an atomic save. Today's region/rate/presentation/week/settings/type capabilities have separate commits and each advances the same root Revision. A browser sequence or Promise.all of those independent PUTs cannot represent one Apply action with all-or-nothing success.

Start, scope and discipline

Read applicable AGENTS.md and relevant backend/frontend skills, current rules/flat-method contract and recovery. Validate HEAD/origin and expected base. Fast-forward only; if only architect documentation advanced, document exact diff and provenance. Stop on unexplained source divergence. Preserve unrelated work/stashes; no reset/rebase/stash/force push.

Bound inspection to current Shipping Authoring Domain/Application/Infrastructure/Endpoints/Contracts, the already-existing Fulfillment Contracts read interfaces and the frontend shipping feature/approved design preview plus necessary canonical local patterns. No general repository audit or findings backlog.

No build, test, typecheck, dotnet ef/scaffold, server/Host, browser/HTTP probe, database/container/schema/migration/seed, package/image operation, cache/output cleanup, shared port/process operation. Git read/fetch and scoped documentation commits/push are permitted. No src/**, AGENTS.md, skill, manifest, certification, deployment or approved rules changes.

Required source-backed outputs
1. Editor field / API / ownership matrix

For every visible approved editor field/action, map:

exact frontend source path/symbol and present fixture-vs-real status;
authoritative backend fact, existing route/request/port/domain symbol if present;
wire identity, unit/presence/null/empty meaning and owner of validation;
read/create/save support today, concrete missing fact, and recommended location.

Cover method canonical binding/name, operational shipping type, multilingual method description and group titles, region selection and labels, conditional rate rows and fees/delivery duration, independent freight/calculation modes, all seven weekdays/timezone, cutoff, configured/completeness/status, list/create/edit/discard/Apply. Distinguish configured draft choices from operational readiness.

Source-check actual API clients/routes; do not assume an existing preview payload is production-ready. Don't invent a seller/admin choice seam.

2. One proposed atomic editor document contract

Give concrete proposed GET/create/replace document shapes, with illustrative JSON that is documentation only, and explain whether existing routes suffice for reads. Choose a minimal own Shipping capability using the existing endpoint -> ISender -> Application -> Domain/port -> own Infrastructure pattern. Show exact reuse/refactor points instead of a second rule engine.

Specify:

trusted full owner scope before dispatch; fulfillment.view/manage independently; immutable canonical binding and existing owner+binding uniqueness;
new draft creation versus editing existing draft; foreign catalog creation is outside a Shipping transaction;
one root expectedRevision, stale rejection before optional foreign I/O where possible, exactly one successful revision/timestamp advance and a truthful identical-document no-op;
stable retained group/rate ids; server ids for new nested children without fake GUIDs or duplicate geography; unknown/foreign/duplicate ids rejected before mutation;
distinguish missing, explicit null, [] and unchanged fields per subdocument. Reconcile selected-language PATCH semantics with a proposed full editor save without erasing untouched/inactive stored text;
reuse current half-open ranges, decimal money precision/trusted currency binding, ISO weekdays and same-day hours, type Active/Inactive/Missing resolution, and raw LanguageId identity;
ALL readonly dependency facts resolved/validated before first tracked mutation; proposed whole graph fully preflighted before applying; one own aggregate save/transaction as actually necessary; no remote writes/distributed transaction;
identify existing Apply methods that increment revision independently and explain a minimal composition/refactor to advance it ONCE; don't reset revision afterward, call independent committing handlers in a loop, or use client compensation;
own EF rollback/cancellation/concurrency/unique-conflict handling and clean loser tracker; immutable response composition after commit without another fallible catalog/registry/geography lookup (197-R1 lesson);
preserve current independent capability APIs/legacy quote behavior, migrations and default-OFF gates. No runtime quote activation.

List a compact meaningful future acceptance matrix: late-invalid-child same-object/no-later-save, new-context complete roundtrip, full-document no-op with advanced clock, one-winner shared-root race, actual loser tracker, mid-write failure/caller cancellation rollback/retry, owner/permission predispatch denial, one/two/three-plus registry and inactive preservation. These are FUTURE tests, not executed proof.

3. Explicit gaps and decisions, with no invented business rule

The preview cutoffHours comment and label describe an order cutoff BEFORE THE START of that working day's interval. Confirm from source. Its sample default 4 and UI min/max 0..48 are fixture/UI facts, NOT approved production technical limits or policy. Weekly schedule currently has no cutoff. Record exact unanswered scope, unit/precision/bounds, closed-day and timezone/DST/runtime questions; recommend minimal draft-only representation separately from any execution algorithm. Don't implement or silently approve a cutoff policy.

Likewise don't derive live activation/status from a preview boolean. Don't imply type selection means quote readiness, free funding or carrier integration. Only source-backed supported draft status may be proposed.

Admin resolves Store or Marketplace; Seller resolves its authenticated authorized owner. A preview owner dropdown does not authorize admin selection of arbitrary seller/tenant. Identify the real allowable UI scopes and any unavailable selector source.

Content languages/currency must use TB188 authoritative context and database registry, independently of interface locale. No hardcoded fa/en production list/default, locale->currency inference, copied registry/catalog or geography. No item classification/customer grouping/basket basis design is authorized.

4. Concrete handoff

Recommend exactly ONE first implementation slice with paths/layers, dependencies, bounded timebox and reviewable acceptance conditions. Distinguish necessary product decision from implementation judgment. Order subsequent major work only as dependencies (atomic backend, feature adapter/UI, live configured-registry/owner acceptance); don't emit additional executable task envelopes or claim delivery complete.

Allowed files and delivery

Persist received task verbatim:
docs/ai/tasks/TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198.task.md

Create only:
docs/architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198/EDITOR-FIELD-API-MATRIX.md
docs/architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198/ATOMIC-EDITOR-CONTRACT-PROPOSAL.md
docs/architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198/RESULT.md

Append one precise worker status pointer to:
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md

Preserve approved decisions/history. Use exact source paths/symbols and separate observed fact, proposal, unknown and pending execution. Keep outputs concrete and compact, not a broad redesign.

At minute 15 stop new investigation; reserve last 5 for evidence, scoped commit/push, fetch parity and result. If incomplete report precise PARTIAL/BLOCKED and next missing fact, without extending the box. No test correction budget applies because tests are forbidden. Do not send dummy Bridge results/probes or enqueue a next task.

Return BEGIN_TOOBA_WORKER_RESULT with:
Task-ID/Parent/Mode; Expected-Base/Base; Status; Code-SHA: UNCHANGED;
Evidence-SHA; HEAD-Origin-Parity; atomic-contract choice and reuse points;
field/API gaps; cutoff/status/owner decisions still requiring approval;
recommended first implementation slice; actually-run checks: SOURCE_ANALYSIS_ONLY;
Changed-Files; Schema-Data-Flag-Changes: NONE;
claim/result UTC and elapsed/timebox compliance;
Production-Functional-Acceptance: PENDING;
Stop-State: STOP_FOR_ARCHITECT;
automaticNextImplementationTask: NONE; Automatic-Next-Task: NONE.
Task PASS means completed source analysis only, never approved production API or functional acceptance.