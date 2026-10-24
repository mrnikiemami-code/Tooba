PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_REAL_ROUTE_MOUNTED_BROWSER_AND_VISUAL_REVIEW
Expected-Base-SHA: b7f7ead6df5129a9b0a62ee201592c4a2888f1ae
Hard-Timebox-Minutes: 30
Stop-New-Interaction-And-Tests-At-Minute: 22
Maximum-Runtime-Setup-Minutes: 5
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Outcome
Exercise the ACTUAL rendered existing-draft editor in a real browser. Stop pure-function proof replay. This is an evidence/runtime check task, NOT feature implementation and NOT release authorization.
Read AGENTS/latest recovery,204-R2 ARCHITECT-REVIEW, frontend skill/standard and accepted186-R2 visual reference/Shipping rules. Verify expectedbase HEAD==origin/main safe tree; preserve unrelated work.199/200/201/202/203/204 accepted for bounded code, not visual/live.
Route: /admin/shipping-services/authoring/<synthetic-guid>?mode=view|edit.
Worker PASS != Architect ACCEPT.

Bounded runtime readiness (max5min, no loop)
Read-only inventory of existing frontend runtime/browser tooling and installed dependencies. Prefer an existing usable dev runtime; no stop/restart/output/cache change. Browser task profile/automation may use already-installed Chromium/Chrome or browser tooling (including CDP); no package/browser/image download or install. No requirement to add a framework.
If none runs, you are AUTHORIZED to start ONE task-owned FRONTEND-ONLY instance on127.0.0.1:3517 after proving port free, using installed Next/dependencies and a task-owned temporary source checkout/copy + separate output/profile. A detached task-owned worktree or tracked-source copy with a local node_modules symlink/junction is allowed; no shared .next/tsconfig/next-env/output mutation. Do not copy credentials/env secrets.
Use TOOBA_HOST_ORIGIN=http://127.0.0.1:1 on this isolated instance (fail-closed fallback), never a shared database or backend runner. This authorization does NOT include starting Host, migrating, seeding or changing production config.
A task-owned fresh browser profile/debugging endpoint is allowed using existing installed binary; do not modify the user's browser profile/session. Stop/remove only EXACT task-owned processes/temp outputs on exit.
If mounted/browser setup cannot complete inside5min, STOP BLOCKED with concrete missing runtime/tool/auth wall and exact safe next requirement. Do not spend the rest on Node tests, installs or investigating environment. No guessed completion claim.

Synthetic API interception, no merchant write
Before opening the page, intercept ALL browser /v1 requests for this run. Fulfill ONLY a scoped whitelist using synthetic fixtures matching the ACTUAL current wire envelopes/string enums/decimal number tokens; unhandled /v1 calls fail locally, never pass through. Confirm from network log that no real mutation reached backend.
Read current editor API/decoder/server JSON contract to build lawful fixtures; don't serialize EditorDocument money strings as invalid wire strings. Include:

actual mixed RTL/LTR configured language ids/default, active/inactive stored text;
two groups, retained ids/currency, multiple condition axes, exact money0.50/1850000.00, destinationFacts and root week with fractional peer time;
real-shaped type choices and Geography branch options/parentId/isActive, both non-null-parent and null-admin-city paths;
scripted editor PUT responses with actual assigned synthetic server group/row ids +revision; capture exact request text and request count.
Use API interception strictly in test/browser, never a production endpoint/flag/backdoor or page override. No fake permission/client owner authority. No login probe/credential entry/auth bypass; an observed auth wall is a readiness blocker.
These are MOUNTED UI + intercepted-contract checks, not real-database/live business acceptance. If browsing an existing signed-in app read-only, don't expose account data or send real PUTs; prefer the synthetic run.

Actual page interactions (bounded)
Drive visible controls rather than evaluating helpers or assigning component state:
A. General loaded from fixture, edit name/description/settings; switch content language/tab and retain edits. Blank stored name: visible issue +Apply blocked +dirty-close warning. Reject discard keeps typed work. Confirm Cancel Edit restores General/groups/rates/week and re-enter edit does not resurrect discarded values.
B. Regions: add group, choose country/area/city/service via actual options, add/remove destination chip, localized title, nested new row with0.50. Existing groups and all retained ids/values persist. Exercise country-wide city parent evidence and null-admin path; inactive options disabled. Moving/removing a group should affect only its own intended content.
C. Root week: edit ONLY start of one stored fractional day; end token remains intact. Raw incomplete time or pending timezone stays dirty/blocked; reset restores baseline. Week exists once, not per group.
D. Compose General +group/rate/week changes then Apply: intercept exactly ONE PUT/current expectedRevision, no post-save GET, full expected retained tree +new null ids, exact money number-token text and unchanged time peer; fulfill lawful response with real synthetic newids/revision and confirm view displays/adopts them. Doubleclick while deferred PUT is pending still one request.
E. One failure/recovery control: scripted409 preserves typed work and does not retry. Separately PUT transport failure/outcomeUnknown blocks re-Apply; explicit reconcile triggers GET and permits review, never auto-replay. Keep run count bounded; no matrix/soak/100-click test loop.
If a real product defect appears, record first decisive reproduction with console/network context and STOP that scenario; do not repair production during this task or absorb another implementation wave. Continue independent screenshots only if still meaningful and time permits.

Visual review, real screenshots
Compare the rendered TWO-PART General/Regions layout against accepted186-R2 reference and existing design-system contract, not copied reference branding.
Inspect FA/RTL and EN/LTR at desktop1440x900 and mobile390x844: spacing/overflow, labels, field direction per CONTENT language, group areas once, all rate axes visible, single root week, dialogs/buttons reachable. No global CSS/design-system changes.
Capture at least4 synthetic-only screenshots spanning both locales/viewports and General/Regions (up to8 if already available). Include viewport/locale/tab in filenames and observations. No personal/account/credential screenshot artifact. A screenshot alone is not user Visual ACCEPT.
Record browser console/hydration errors and first failed requests; distinguish fixture error, runtime blocker and production UI defect. No typecheck-as-visual claim.

Validation/limits/delivery
No production file change. Allowed new task-owned browser script/fixtures and synthetic evidence under this task's docs/evidence directory, task receipt, RESULT, one recovery pointer. Do not commit temporary worktrees/node_modules/profiles/build outputs or browser downloads. No package/config/baseline/skill/AGENTS change, no new runtime project, backend/Host/solution/build/discovery/source-size or whole unit-suite loop.
Hard30min fromclaim; setup<=5; stop newinteractions/tests22; final8 only cleanup/evidence/diff/scopedcommit/push/fetchparity/report. One harness/fixture correction max; report PARTIAL if more or a source defect blocks the run.
No shared ports3000/3419/5088/5432/Redis/RabbitMQ/process restart, DB/schema/data/flag writes, migration/seed/image install, cache/bin/obj/.next purge, forced Git operation or Bridge probe. Any local browser fixture PUT is explicitly permitted; REAL merchant PUT remains forbidden.
Report exact runtime/source SHA, whether actual route was mounted, fixture-vs-live boundary, scenarios ACTUALLY executed with request counts/body observations, screenshots/findings and unexecuted cases. List exact task-owned startup/cleanup identities and prove port3517 free afterwards if used. Never label a blocked/mocked run as production-functional acceptance.
Persist received task verbatim; own RESULT; one recovery pointer. Result includes IDs/base/code/evidence/parity; Production-Functional-Acceptance:PENDING; Shared-Database-Data-Flag-Changes:NONE; Visual:REVIEW_EVIDENCE_ONLY or NOTRUN/BLOCKED as actually observed; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
Only this task; no automatic next task or implementation envelope.