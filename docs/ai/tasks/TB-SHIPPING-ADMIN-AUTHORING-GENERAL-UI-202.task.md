PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202
Parent-Task: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_EXISTING_DRAFT_GENERAL_UI_AND_ATOMIC_APPLY
Expected-Base-SHA: 439c43017ab38bdd6fb6999e26976796365e8140
Hard-Timebox-Minutes: 45
Stop-New-Implementation-And-Tests-At-Minute: 35
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Deliver actual visible software

Connect the EXISTING admin shipping draft GENERAL form to the real backend through the accepted admin-shipping-authoring feature. This is the FIRST rendered connected slice: names/descriptions, real shipping type, independent freight/calculation settings, ONE atomic Apply. Regions/rates/week are displayed truthfully READ-ONLY and preserved. Do NOT implement region/rate/week editing, whole-method creation, activation or unsupported cutoff/funding/carrier controls in this slice.

Read AGENTS/latest recovery, frontend feature skill, FRONTEND-ARCHITECTURE-STANDARD, Shipping rules and accepted186-R2 preview visual/reference. Backend199/readbridge201 and adapter200 accepted; do not replay their proofs. Preserve Shopeiva/flat-method locks and existing three row actions/legacy behavior.
Fetch/verify expectedbase HEAD==origin/main, safe fast-forward only, preserve unrelated work/stashes. No reset/rebase/stash/force-push or Bridge probe payload.

1. Feature ownership and thin route

Work inside src/frontend/features/admin-shipping-authoring:
cohesive presentational components, small feature hook/controller, local interface-locale messages, existing typed API/model/session. Export rendered capability through index.ts; app pages import PUBLIC boundary only. Network/business mapping stays out of pure fields.
Add ONE thin Next route:
src/frontend/app/admin/shipping-services/authoring/[draftId]/page.tsx
It composes the public screen, validates route identity and UI-only view/edit mode, obtains locale by existing canonical cookie/provider conventions. No business/transport implementation in page, no secrets/session token to HTML. View/edit UI mode is NOT permission/owner authority; every backend request still decides access.

Use approved two-part preview layout as visual reference: General / Regions. Reuse design-system fields/selects/buttons/Dialog/tokens; do not import preview fixtures/models as live data or redesign shared UI. Regions tab here is read-only. Preserve responsive spacing and RTL/LTR. No raw GUID input, manually typed canonical binding or technical-code-as-name workaround.

2. Reachable entry without disrupting legacy editing

Minimal additive entry in existing FlatMethodDetailDialog, ONLY when mode is VIEW, active general tab and binding is server-resolved. A small public feature component may receive that actual serviceCode/optionCode binding and interface locale. Do not deep-import feature internals from the app.
On deliberate entry activation, read existing authorized GET /v1/admin/shipping-services/authoring-drafts and match EXACT binding (serviceCode + nullable optionCode). Validate real response; 0 match = honest localized no-draft state, >1 = ambiguous/untrusted, auth/network/malformed remain errors. Exactly1 -> navigate to the new draft route in view mode; Edit action inside new screen enables local form only. No methodCode splitting, arbitrary seller/tenant selection, automatic POST/create or per-row N+1 preloading. No new grid/row action or replacement of existing legacy edit flow.
This view-only placement prevents silently dropping the legacy form's unsaved edits. All current legacy view/edit/delete/rate/policy controls stay intact.

3. Decode201 destinationFacts and supply real ancestry

Extend current read model/decoder for additive destinationFacts:
distinct(kind,id), Resolved/Unresolved, canonical write area ornull, actual localizedlabel ornull.
Validate entries against selected keys, terminalidentity and canonical shape; duplicate/wrong terminal or contradictory Resolved/null fails closed. No filtering-away corrupt selected facts. Older/missing hydration field must be explicitly unavailable, never invented resolved facts.
Build the synchronous AreaAncestryResolver ONLY from validated responsefacts (kind+id lookup); no all-world Geography scan, guessed parent or global cache. Unresolved/absent fact must block Apply with localized explanation while retaining the original tree/edits. Missing label displays a localized unavailable-name label, never rawGUID as name.

AUTHORIZED compatibility fix: current request.ts writesArea incorrectly requires administrativeAreaId for ALL non-country kinds. Canonical backend201 permits City/ServiceArea with null admin; narrow that check to AdministrativeArea selection itself. City still requires country+city and ServiceArea requires country+city+servicearea, exact terminal match/deeper-field restrictions retained. Add a focused nulladmincity AND nulladminservicearea roundtrip; don't weaken identity validation. No backend rule/DTO changes.

4. General form behavior

Use loaded confirmed baseline + local edits/session reducer accepted200:

Select content languages dynamically from actual active metadata/order/defaultLanguageId, with real direction per input. Interface locale and content language remain independent; switching content language retains edits for other languages. Raw inactive stored text is preserved/readable; never emitted as writable patches.
Name and description controlled inputs patch only intentionally edited active LanguageIds. No default text/auto-translation/dictionary whole replacement. Description clear remains explicit null/empty per canonical contract. Default-name completeness displayed from server fact, not guessed readiness.
Shipping type selector ABOVE method name uses real GET /v1/admin/shipping-services/authoring-types (existing server source). Add typed read helper reusing current feature transport/decode, not a second auth client. Actual translations/ids only, no ordinary/heavy fixtures. Stored Inactive/Missing must be shown truthfully and never auto-cleared. Explicit user clear emits typeAssignment:{shippingTypeId:null}; untouched selection wrapper omitted. Missing type names display unavailable-name state, not invented labels/id fallback.
Independent freight payment mode and calculation mode use exact enum values with localized labels. Unconfigured is a real blank state; don't default Manual/Prepaid. Supplying settings requires BOTH explicit choices. Switching Free does not zero or erase retained manual rows/currency.
Read-only regions show real server titles/default-language labels, exact persisted geography facts and rates/currency/delivery durations without duplicated geography. Read-only week shows real timezone and seven actual day states/remembered hours; no fabricated default schedule. Preserve these sections in the eventual PUT; no regionGroups[] from general edits.
Resolved owner shown read-only as Store/Marketplace; no selector or admin-picks-seller authority. No draft activation toggle or invented operational readiness/status; no cutoff/carrier/funding controls pretending persistence.
No auto-save on input/mode/language/tab change. Apply uses EXACTLY ONE existing editor-document PUT with expectedRevision; source facts/retained ids/decimal token text preserved. Adopt actual validated serverresponse ids/revision (same-revision no-op accepted), not an optimistic fake success or an unnecessary post-save GET.
Saving synchronously gates doubleclick before first await, independently of React's batched render. Prevent edits/close loss while request in flight, use current epoch/requestId for completions. Don't reset accepted guards or duplicate session state into another store.
Loading/denied/notfound/dependencyfailure/untrusted states explicit with meaningful localized retry. Validation root/child codes map to proper fields/summary; rawcodes are not user-facing prose. Keep edits on failure. Server remains permission authority, PUT403 never becomes success.
Conflict preserves edits; explicit discard+freshload requires confirmation, no stale automaticretry. outcomeUnknown stays latched across edits and blocksApply; provide explicit "read server status" recovery using accepted reconcile actions, show operator must review before deliberate nextApply. No automatic replay.
Back/close/discard and same-draft refresh use hasUnsavedWork and existingDialog confirmation; reject/cancel retains all state. Add beforeunload protection for dirty/unknown/inflight work using established local pattern. No global navigation framework/history hacks or localStorage draft persistence. Do not silently drop dirty work on locale refresh/navigation.
5. Focused verification, proportional to visible change

Test actual feature orchestration with controlled fetch/deferred promises and real decoder/session/builder:
load document+201facts -> edit name/description/settings/type -> one PUT -> adopt actual response; old groups/rows/geography/titles/week intact; nulladmincity/servicearea body valid; locale/content selection identity independent; active-language intent only.
DoubleApply during in-flight sends1 request; late A cannot replace currentB;403/409/503/unknown preserve edits, no automaticretry; explicit recovery/discard/cancel works. Test real binding-to-draft entry0/1/ambiguous/errors, no autoPOST. Use existing Node test pattern; no new test framework/package. Existing unrelated source-size offenders remain untouched.
Run new focused tests, current feature regressions once and npm run typecheck with commands<=120s. Relevant touched import/boundary guards only, no wholefrontend discovery/build/criticalstorefront suite (storefront unchanged), no backend/Host/solution tests.

If existing dev app/session is already available, one READ-ONLY visual inspection of the new route at desktop and390x844 with FA/EN interface is permitted, no new server/restart/login probe or merchant write. Otherwise record visualNOTRUN and use available local render/source checks; do NOT claim VisualACCEPT from typecheck. No new browser/server setup to fill the deadline.

Scope / hard bounds

Allowed: existing admin-shipping-authoring feature (api/model/session/hook/components/messages/index/tests); thin authoring/[draftId]/page; minimal VIEW-only entry composition in flat-method-detail-dialog.tsx and directly affected focused test. No preview/legacy business-flow rewrite, sharedCSS/design-system/backend/Host/schema/migration/flag/newproject/package/config/skill/AGENTS/baseline change.
No sharedapp ports3000/3419/5088/DB5432/Redis/RabbitMQ/process restart, containers, migrations, image/package download, cache/bin/obj/.next purge, credentials or live write probe.

45min HARD fromclaim; stop newcoding/tests at35; reserve10 for diff/evidence/scopedcommit/push/fetchparity/result. At most2 bounded harness corrections, stop with exactPARTIAL on wider blocker, no confidence/proof loop.
Docs: received task artifact; own evidence RESULT with reachable route, exact UI/data/state boundary, testcounts/typecheck/visual limits/remaining editing scope; one worker recovery pointer. No certification or whole-delivery completion claim.

Result BEGIN_TOOBA_WORKER_RESULT includes task/parent/mode/base/code/evidenceSHA/parity; real entry/form fields;201hydration/nulladmin compat; onePUT/error/unsaved/doubleclick proof; actualcommands vsNOTRUN visual; exactfiles/time/deviations; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
Only THIS task, no automatic next envelope.