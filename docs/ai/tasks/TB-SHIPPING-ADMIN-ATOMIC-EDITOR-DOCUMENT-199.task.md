PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199
Parent-Task: TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_EXISTING_DRAFT_ATOMIC_EDITOR_DOCUMENT_IMPLEMENTATION
Expected-Base-SHA: 8211c5e43eb9cf5a8a265c99f78c5c68f6758af9
Hard-Timebox-Minutes: 45
Stop-New-Implementation-And-Tests-At-Minute: 35
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Mission / authority

Deliver real GET/PUT /v1/{admin|seller}/shipping-services/authoring-drafts/{draftId:guid}/editor-document for an EXISTING authorized draft. One editor Apply composes regions, nested rates/titles, selected-language method presentation, optional week/settings/type into ONE own atomic commit and ONE root revision advance. No frontend work and no whole-method creation claim.

Read AGENTS, canonical recovery, backend feature skill, Shipping rules and TB198 ARCHITECT-REVIEW.md first. Use its binding corrections, NOT proposal examples as DTO/validation truth. Apply the skill/structure rules on this capability only; no AMSC recertification or general audit.

Verify fetch HEAD==origin/main expected base; fast-forward only. If source changed unexpectedly stop with precise divergence. Preserve unrelated work, stashes and build output. One task only; no next-task enqueue/probe payload.

Fixed scope and semantics
Four additive routes, ONE query + ONE command/handler shared by audiences. Real owner gate before ISender (and before write body parsing): fulfillment.view/read and fulfillment.manage/write independently. Full own owner+draft filter; unknown/foreign same safe404, SingleStore seller canonical404 before seller resolution. No transport owner/tenant/seller/edition/currency authority.
GET loads the complete own authored graph as one consistent root snapshot (groups/areas/rates/currency/translations/titles/week/settings/type id/revision/timestamps). Reuse typed projections and Contracts facts, not six independent endpoint calls. Include authoritative language metadata/default and truthful type resolution using existing composition. Geography ids remain typed; no fake labels, catalog copies or guessed completeness/activation. External facts are current observations, not a distributed snapshot.
PUT has ONE required positive expectedRevision. Root binding/id/owner immutable. Regions are a required non-null whole ordered collection ([] valid incomplete); each group has optional retained GroupId (null/absent=new server id), required Areas and required Rows ([] valid), and required selected-language Titles patch list ([] no change). Nest group-title edits under their group so new groups need no client fake GUID.
MethodTranslations is a required selected-language patch collection ([] no change); omitted languages, including inactive stored text, remain untouched. Existing active-language validation and exact existing presentation bounds apply (name200/description2000/title200, verify actual canonical code). Existing description/title explicit null/empty clearing retained. Do not copy type-catalog128/512 limits or echo inactive rows into a write.
Optional WeeklySchedule and MethodSettings: absent/null means PRESERVE (document this unambiguously, no new clearing authority). Supplied objects use existing required-field/7ISOdays/timezone/closed-hour semantics. Unconfigured stays unconfigured when omitted; never default Manual/Prepaid or seven fake days.
Optional TypeAssignment wrapper: absent/null wrapper=PRESERVE; supplied wrapper MUST contain shippingTypeId (presence-aware). Explicit shippingTypeId:null=CLEAR with zero catalog lookup if response can use unconfigured fact; Guid.Empty invalid; every supplied non-null id requires one exact ACTIVE fact including identical selection, reusing197-R1. Stale revision checked before optional foreign I/O. GET preserves inactive/missing raw id; inconsistent nonempty facts503, never fabricated Missing. An omitted existing type may be read pre-write for truthful response but is NOT revalidated as a new active selection, so deactivation doesn't prevent unrelated edits.
Existing selected-area write shape/ancestry, rate half-open three-axis overlap, trusted currency binding/change safety, numeric precision/duration limits and six mode combinations remain exactly canonical. No new policy/default. Do not accept group/row IDs from another owner/group or ids from deleted groups. Retained ids stable, omitted existing groups/rows removed by explicit whole collection semantics only. No reattachment of another group's row.
New groups may contain rows and titles in the SAME PUT. Resolve server ids and a prospective detached plan; validate the ENTIRE proposed tree BEFORE changing any tracked aggregate/property/order/revision/timestamp. Current PreflightGroupRateRows/PreflightPresentation look up EXISTING groups: adapt shared pure cores to prospective groups, don't apply groups first then discover a late-invalid child. No second range/geography/presentation rule engine. Do not rehydrate arbitrary transport into tracked EF graphs.
Existing Apply* each bumps Revision. Refactor their shared mutation cores minimally and compose one prepared plan so editor advances root once only when actual stored content changes. Preserve all standalone capability APIs/behavior; no revision reset/decrement, suppress flag publicly exposed to callers, repeated committing handler dispatch or browser saga. Existing currency binding must not clear on [].
Global no-op detection includes unchanged region identity/order/areas/rows/text and omitted sections; compare semantic stored content with correct null distinctions. Identical request at current revision preserves root UpdatedAt and revision; stale identical always409. No-op performs no unnecessary SaveChanges.
Atomic persistence — explicitly authorized narrow reuse

One COMMIT is the invariant, NOT one SaveChanges. Existing immediate order/node uniqueness requires staged replacement and same-row node moves, already proven190-R2. Reuse/refactor its module-owned boundary to support editor and existing region writer, preserving staged->final order in one own transaction, rollback/cancellation and actual loser tracker cleanup.

Authorization: extract the existing reviewed staged-save boundary into ONE narrowly named typed helper in ShippingAuthoringDraftDirectory, e.g. SaveAuthoringReplacementAsync, used only by ReplaceRegionGroupsAsync and ReplaceEditorDocumentAsync. Exactly ONE transaction begin site remains; no second begin or arbitrary callback/transaction service. Staged saves may both run but root revision advances once. Avoid nested transactions or executing existing committed methods.

Update ShippingTransactionBoundary and its compatibility tests ONLY for that exact reviewed member/caller universe/required begin-save-commit-rollback shape; retain unexpected file/member/second-begin, startup schema and operational money/capacity negatives, including same-file injected offender. No filename exemption, general allowlist, removed negative, broader schema/money authority, deferred-constraint migration or baseline refresh. If safe sharing cannot preserve this bounded invariant, stop with the exact alternative rather than silently broaden.

All readonly required Contracts facts/ancestry/languages/currency/type resolved before first tracked mutation/write. No new foreign Domain/Application/Infrastructure reference, DbContext, SQL join, cross-module transaction/write. No foreign I/O/cancellation probe/fallible re-query AFTER commit: compose response from saved actual root plus already acquired facts. Dependency failure before write leaves tracked and persisted state unchanged.

Canonical Result/typed code/resource/body reader/FluentValidation/ApiResponseFactory, scoped own DI, existing tracing/correlation. Reuse existing error codes when accurate; minimal new validation/business descriptors only if necessary with canonical resources. New Contracts contain transport facts only; Domain no HTTP/EF or foreign module types.

Proof (focused, meaningful; do not replay prior proofs)

Use real endpoint delegates/real owner authorizer/body reader/MediatR validation/handlers/directory; controlled platform/Contracts facts labelled fixtures. Execute admin+seller success, view-only deny before dispatch/body, foreign owner safe404 and SingleStore seller early404, malformed/presence/null element/duplicate ids with exact validation codes, not status alone.

On isolated real PostgreSQL17 target through existing ShippingRelationalTestTarget:

Start from a complete seeded draft. ONE PUT adds a new group with rates+titles, reorders retained groups and changes multiple sections; NEW context verifies exact whole winning tree, stable retained ids and revision exactly old+1.
Bad LAST child after valid earlier edits: SAME aggregate/tracker/whole persisted tree unchanged, later SaveChanges=0, valid retry succeeds. Include rejected foreign/unknown group/row ids.
Identical composite document with advanced clock preserves revision/UpdatedAt, including nonempty multilingual patches, rates and reordered groups. Omitted inactive language raw text preserved; inactive explicit edit rejected. Focused1/2/3+ language facts reuse real adapter, not hardcoded fa/en.
Editor vs existing rate/region capability on SAME root revision: bounded first-SavingChanges two-arrival barrier AFTER both read and BEFORE writes; capture actual tracked OriginalValues[Revision] BOTH equal1, exactly one success and one canonical409, new revision2 and complete winner-specific tree/no hybrid, ACTUAL loser clean and later SaveChanges=0. One such cross-capability race suffices, don't multiply by every pair.
Fail SECOND staged SaveChanges after first write, and caller cancellation at that same boundary with actual caller CTS: fresh-context full tree unchanged, no staged residue, actual writing tracker clean, retry at original revision succeeds. Use established scoped test interceptor; no production trigger/failure mechanism.
Catalog fixture throws on any post-write lookup: PUT success from pre-read facts; clear/early-denial lookup counts appropriate. Assert source/parent tracing using reused proof unless instrumentation changed.
Exact route/request/handler/validator set equality for four additive routes and module structure/error/transaction guards retained.

Build only Shipping.Tests and affected references, commands <=120s, setup <=8min. Run the smallest new endpoint/domain/storage/guard filters; after they pass run whole Shipping suite ONCE with isolated DB live only if within deadline. No Host/Fulfillment/solution/broad architecture sweep or confidence-repeat loop. Report exactly executed counts; unexecuted not PASS.

Disposable DB/container only existing local postgres:17-alpine on 127.0.0.1:55432, db shipping_tb190r1, role tb190r1; parsed fail-closed target and live current_database/current_user before DDL. No image/package download/shared5432/tooba_alpha/store-alpha/sharedapps3000/3419/5088/Redis/RabbitMQ. Task container removed and port free verified in cleanup even on failure. Never log credentials.

Allowed change universe

Shipping existing Authoring capability Domain/Application/Contracts/Infrastructure/Endpoints and module-owned DI if necessary; cohesive new Authoring files, own tests; request-classification manifest and affected exact structure/error/route guards. No new project/package, foreign module/Host/frontend production file, schema/DbContext mapping/migration/designer/snapshot, shared library, feature flag, skill or AGENTS change. No runtime quote/financial/carrier/funding/item-basis/grouping/activation. Cutoff/status/new clearing remain excluded, no fabricated product decisions.

Docs: received task verbatim in docs/ai/tasks; evidence RESULT.md under docs/architecture/evidence/TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199; append actual implementation facts to Shipping rules (not preview policy), classification exact update and one recovery worker pointer. Do not promote certification or mark Architect ACCEPT.

Deadline and handoff

45min HARD from claim; no new implementation/test starts after35; reserve10 for cleanup/diff/evidence/scoped commit/push/fetch parity/result. Allow at most TWO bounded harness corrections; stop if exhausted or production issue needs wider scope. Do not fix unrelated failures. An unfinished feature must be PARTIAL with exact missing proof, not force green by guard weakening. Do not omit cleanup to meet deadline.

Result BEGIN_TOOBA_WORKER_RESULT includes task/parent/mode/base/code/evidence SHAs/parity; actual wire presence semantics; prospective preflight/new-group proof; single revision/noop; exact one begin-site and commit strategy; actual whole-tree rollback/race/winner/loser proof; executed tests vs reused proof; changed files; any deviations and remaining gaps; task container cleanup; UTC/timebox; Shared-Database-Data-Flag-Changes:NONE; Production-Functional-Acceptance:PENDING; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.

Do not issue another task. Completion is existing-draft atomic editor backend only; UI, full create workflow, live merchant registry acceptance and runtime activation remain separate.