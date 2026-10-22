PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200
Parent-Task: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_FRONTEND_TYPED_EDITOR_ADAPTER_AND_SESSION_STATE
Expected-Base-SHA: ccca74258fb70c3eb64be5c743b9ee2d93235e5a
Hard-Timebox-Minutes: 30
Stop-New-Implementation-And-Tests-At-Minute: 23
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Deliverable

Implement the reusable FRONTEND foundation for connecting the approved shipping editor to the REAL existing-admin-draft API. Typed API boundary + local edit/session state + focused tests, no rendered page/UI change in this slice. This is implementation, not another analysis document.

TB199 atomic existing-draft backend is Architect-accepted through199-R1. Reuse it; do not replay backend proofs. Read latest recovery,199-R1 review, AGENTS, .cursor/skills/tooba-frontend-feature/SKILL.md and FRONTEND-ARCHITECTURE-STANDARD.md before coding. Follow relevant frozen visual/flat-method/rules constraints. This task authorizes only the scoped frontend foundation, not an exception for changing live screens.

One owner: src/frontend/features/admin-shipping-authoring/ with a public index.ts, cohesive api/model/session modules and focused Node tests, following established features/admin-sellers or admin-languages placement conventions. Prefer existing shared lib/admin adminRequestHeaders/result/error utilities over importing a page-owned app/admin client into a feature. Do not invent a second auth client, generic transport/store/form framework or duplicate legacy rate-policy APIs.

A. Exact typed API boundary

Trace actual backend DTOs and mapped JSON (Contracts/Authoring/ShippingAuthoringEditorDocumentContracts.cs; Application/Authoring/Models/ShippingAuthoringEditorDocumentModels.cs; editor query/composition, type/context/presentation projections). They are authority;198 proposal/preview sample types are not.

Implement fetch/decode and one atomic PUT for existing authorized ADMIN drafts:
GET/PUT /v1/admin/shipping-services/authoring-drafts/{draftId:guid}/editor-document through the actual existing frontend upstream/proxy convention. Trace existing route proxy/headers; do not assume a new endpoint prefix or add a parallel proxy. Shared auth transport only, no browser-supplied owner/tenant/seller/edition/currency authority. No seller UI/credential mechanism invented; API/model design must not imply admin may pick seller.

Supply helpers needed for subsequent real type selector/context reads using existing authoring-types/context routes only if necessary; no catalog write, draft create, activation or per-capability writes. Each Apply sends exactly ONE editor-document PUT with ONE expectedRevision; no sequence/Promise.all/saga of six PUTs.

Decode unknown response/envelope before exposure: required scalar/object/array identities and known enum variants; no any, blanket type assertion, filter-away-invalid child or fallback empty-success/default. Missing required collections, malformed decimals/times/enums/ids/default-language metadata => untrusted failure. Preserve stored raw translations including inactive language ids; raw text must not be coerced/trimmed by a read mapper.
Match actual enum wire representations, owner response visibility and nested presentation/type shape; reject unknown variants rather than fabricate Manual/Prepaid/Active. Do not hardcode fa/en, IRR, first language as default or convert locale to content-language/currency. Language direction and order come from actual metadata. No translation auto-fill. Currency from GET/context is observational, never inserted as transport authority.
Preserve decimal numeric contract without silent toFixed/parseInt conversion, half-open bounds, nullable max/unconfigured sections, literal null vs absence. Avoid expanding pricing/business validation into client policy; server canonical rules remain authority.
Failures explicit: auth401/403,404, canonical409 conflict,422/400 validation including property-child errors,503/unavailable, malformed2xx/untrusted, network/abort. Keep HTTP status and canonical root/child codes for the later localized presenter; don't hardcode user-facing prose in adapter or discard errors into [].
GET network/cancel is not not-found. PUT network/abort or unreadable success cannot establish no commit: expose outcomeUnknown and NEVER automatically retry/replay with a new revision. Definitive canonical409 is a conflict; don't turn it into unknown success.
No success notification derived merely from request dispatch; accept confirmed validated server response. Saved revision may be unchanged for true no-op. Confirm draft id matches requested; never silently apply another draft's payload.
B. Safe editor session state (small, local feature model)

Provide a cohesive reducer/controller (or existing equivalent pattern) used later by UI. No global store/framework or duplicated sources of truth. API functions stay separate from state and rendering.

State supports load/loaded-empty-values/denied/notfound/error, original server baseline vs local draft/dirty intent, editing/saving, validation failure, conflict and outcomeUnknown. A successful save adopts actual validated response incl server-generated group/row ids/revision; failure preserves edits. Don't auto-reload over unsaved work, automatically resubmit conflicts or create a fake successful baseline.

Scope async results by draft/session/request generation: slow load or save for A must not overwrite B or an obsolete session. Prevent concurrent double Apply for the same in-flight request. Discard/reset is explicit; closing/switching with dirty work exposes a confirmation decision to UI, never silently discards. Do not use window.confirm/localStorage in model.
Selected-language method/title edits serialize ONLY intentionally touched active languages; inactive/untouched text stays on read baseline and is NOT echoed as writable patches. [] is no text change. Interface culture changes do not change ids, selections, money, ISO weekdays, time values or the content-language edits.
Preserve explicit optional semantics: absent/null schedule/settings wrapper=PRESERVE, supplied full object=replace; no new clear/reset authority or enum-zero default. Type wrapper omit=PRESERVE, supplied {shippingTypeId:null}=CLEAR, supplied non-null=selection; never omit an explicit clear on serialization. Empty required regions/rows arrays are allowed incomplete drafts and never interpreted as free/unrestricted.
Retained group/row ids remain server ids; new children represented without server GUID until response. A client-local UI key, if needed later, must NOT be serialized as a group/row id.

Important geography boundary:
GET areas are (kind,id) ONLY; write areas require canonical ancestor fields. Do NOT build a PUT by blindly echoing GET or guessing parents from ids, display labels or catalog method codes. Define typed write-selection input that must be supplied from the existing authoritative Geography picker/resolution path in the subsequent UI task. An unresolved read selection must block request construction with an explicit needsResolution result, not silently disappear from a group. Keep that distinction tested. Do not add new Geography backend seams, persistence or scan APIs.

No preview-model/fixtures import as runtime authority and no mapping ordinary/heavy/etc to realUUID. No cutoff/status/completeness/carrier/free-funding policy, owner picker or new whole-method create workflow.

C. Meaningful focused tests and verification

Use the repo's existing node --experimental-strip-types --test pattern, mock fetch with controlled wire fixtures (labelled fixtures, not live registry proof). Tests must assert actual outgoing URL/method/header/body and response mapping, not just constants:

Exact real composite wire -> decode, 1 active NON-fa language, mixed rtl/ltr two languages and3+ with nonfirst default; inactive stored text retained; malformed/unknown child fails closed.
Single PUT expectedRevision/presence/null/[]/stable vs new id semantics; type explicitclear retained; no owner/tenant/currency authority; no per-capability mutation call. Read selection lacking ancestry cannot serialize into a destructive PUT.
Auth/notfound/canonical409 with property errors,503/network/malformed2xx. PUT network/abort becomes outcomeUnknown and no automatic retry; GET abort not fabricated404.
Local edits survive conflict/error; save success adopts real server ids/revision; no-op same revision accepted. Dirty-discard decision and duplicate Apply gated.
Deferred promises: late A load/save cannot overwrite B; intentionally touched active language patch only; interface locale switch doesn't alter raw model/body.
Avoid exhaustive implementation-mirroring tests or broad suite confidence loops.

Run focused feature tests and npm run typecheck ONCE when feasible, bounded commands<=120s. If typecheck has unrelated existing failures, report exact path/diagnostic and do not fix unrelated modules or change config/baselines. Relevant existing feature-boundary/import/test-discovery guards only if affected; no whole frontend test-discovery suite/build/Next server/browser/HTTP probe. No backend build/test/DB/container/migration needed.

Files / constraints

Allowed: only new cohesive features/admin-shipping-authoring feature files/public index/tests; own task/evidence docs and one worker recovery pointer. No src/backend change, no package/project/config/skill/AGENTS/manifest/architecture-baseline edit; no shared lib admin transport mutation (reuse current public export), no app routes/page/live flat screen/preview/design-system/storefront/sharedCSS change. If existing transport cannot be safely reused without broader edit, report exact blocker rather than copy it.

No shared app ports3000/3419/5088/DB5432/Redis/RabbitMQ, server startup/restart, credentials, downloads or dependency install; no .next/bin/obj/cache purge. No live network probing or merchant writes. Preserve unrelated work/stashes; no reset/rebase/stash/force-push. No certification/visual acceptance claim because no visible UI changed.

Handoff / deadline

30min HARD from claim; stop new implementation/test at23; reserve7 for diff/evidence/commit/push/fetch parity/result. At most2 bounded harness corrections; then precise PARTIAL, no retry loop. No new task emitted. UI wiring is the next dependency only AFTER Architect review.

Persist received task verbatim docs/ai/tasks/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200.task.md.
Own evidence docs/architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200/RESULT.md must include feature ownership/public interface, exact endpoints/wire mapping, state diagram/table if useful, geography unresolved boundary, executed tests/typecheck diagnostics vs unrun, files, API/visual impact, claim/resultUTC/elapsed and corrections, HEAD/origin parity, limitations. Append worker recovery pointer only.

Result BEGIN_TOOBA_WORKER_RESULT: Task-ID/Parent/Mode/Base/Code/Evidence SHA/parity; typed adapter/session behavior and tests; permissions/server authority; onePUT/no unsafe retry/late response proof; actual command counts; no visual/live registry claim; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.