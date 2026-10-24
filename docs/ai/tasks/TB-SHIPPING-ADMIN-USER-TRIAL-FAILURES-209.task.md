TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
Worker-Id: tooba-worker-01
Repository: mrnikiemami-code/Tooba
Branch: main
Parent-Task: TB-SHIPPING-ADMIN-LOCAL-STACK-REFRESH-AND-USER-TRIAL-208
Expected-Base-SHA: d0b8a1e84871c456e343bbb8b09ee0b0170f4df8
Mode: REAL_USER_REPORTED_DESTINATION_SAVE_AND_USABILITY_REPAIR
Hard-Timebox: 45 minutes
Stop-New-Implementation: minute35; final10 focused live verification/evidence/commit/push
Automatic-Next-Task: NONE
Stop-State: STOP_FOR_ARCHITECT

Real user rejection / authorization

The user REJECTED the delivered UI and could not complete/save destination + amounts.
This supersedes any implication of UI/user acceptance in208.208 remains accepted ONLY for schema/runtime/read reachability.
Prior user local NONproduction execution authorization persists; do not ask approval again for necessary dev actions.
Goal is ONE usable real authoring flow, not more source-only proof: name -> destination/cost -> valid schedule/settings as touched -> save -> fresh read.

Screenshots observed by Architect:

General Edit: full-width sparse controls; native type select only "نوعی انتخاب نشده است"/"بدون نوع (پاک‌کردن صریح)" and actual empty active catalog; fa name "پیشتاز"; independent settings selected; timezone input contains the human text "تهران"; weekday interval shown.
Regions Edit after attempted Apply: generic "سرور برخی فیلدها را نپذیرفت، تغییرات شما حفظ شد" with NO actionable field reason.
New card "گروه تازه", title "تست", Country chip "نام موجود نیست", all four destination selects show em dash;
weight1..2, freight120000, packaging5000, delivery1..3 were visibly typed.
These are observations, NOT established root causes. Do not claim amounts cannot be typed merely from the report.
The screenshot's timezone "تهران" may be invalid server input; prove actual rejection code before blaming it.
No screenshots are required to be fetched by worker; exact observations above suffice.
Recover / architecture / visual target

Read AGENTS + TOP canonical recovery + current task, then:
.cursor/skills/tooba-frontend-feature/SKILL.md
docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md
docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md (approved two-part186-R2 reference, one non-overlapping region collection with nested price rows, independent settings).
If a backend repair is genuinely necessary, read backend feature skill first; stay in owning module.
Keep existing feature owner/public boundary, established design-system/forms/tokens, locale/RTL/LTR.
The user explicitly authorizes correcting THIS rejected UI/workflow; preserve approved two-tab General / destinations-and-costs concept, not arbitrary earlier component spacing/labels.
No new architecture/state framework, parallel rule engine, broad redesign or unrelated storefront/legacy grid change.
Persist task verbatim; verify safe main/origin. Preserve unrelated files and already-applied migrations.

Existing live environment

FE3000 unchanged; Host5088 current retained207 binary (historical PID10800, reverify); launcher D:\temp\tb208-host\run-host.cmd.
DB local127.0.0.1:5432/tooba_alpha, tenant-alpha/store-alpha/SingleStore.9migrations already applied; NO reapply/reset as first action.
User trial draft 01a16eeb-70b3-7000-87c2-564c82835496, binding post/null. Its CURRENT revision/content may have changed since208.
URL http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Do not overwrite user's stored content or assume revision1. Preserve actual existing authored content.
Do not reload/erase the user's active browser edits. Use an isolated browser profile for your own reproduction.
Use real authorized APIs; no Fetch fulfilment/response mocks.

One cohesive repair, prioritized

A. REPRODUCE actual failure first (bounded, no diagnosis-only stop)

Read current draft + actual configured language/type/geography responses using normal dev actor/auth.
Trace real selected country/area/city option payloads, labels, parent proof, active state and selected chips.
Capture exact rejected PUT property paths/error codes/status without secrets. Retain raw intent on failure.
Reproduce screenshot-shaped invalid timezone in a controlled draft if needed, without overwriting the user's draft.
One additional LOCAL reproduction draft via existing authorized POST/binding is allowed only if necessary and not conflicting;
otherwise use the user-trial draft only for a clearly valid minimal edit preserving all existing stored sections.
Establish whether country options are empty, unauthorized, rejected by decoder, parent-invalid or missing translations.
Do not turn an error into successful[] or fake labels/parents. Fix actual source/contract mismatch.
In the CURRENT source, new chips render index.labelOf(area) which only indexes baseline facts, while new picks are in pickedFacts.
Investigate this as a source hypothesis for "نام موجود نیست"; wire labels from verified current picks if confirmed,
preserving canonical ancestry and no-GUID/no-code-as-label rules. Do not add another geography store.
Generic save error is definitely inadequate: current fieldLabel defaults unrecognized paths to name and hides error codes.
Map actual owned validation errors to correct local field/section, localized human reason, accessible inline error
and focus/scroll to first invalid input. Keep technical property/code available only in diagnostics, not main copy.
Preserve conflict/unknown latch, dirty input, onePUT, no automatic retry.

B. USABLE regions/costs presentation (same model, clear language)

Visible "گروه" becomes "محدودهٔ ارسال"; action "افزودن محدودهٔ ارسال"; remove/move/title likewise.
Explain once, briefly: choose destinations sharing these cost rules. Internal group ids/DTO/domain remain unchanged.
One coherent compact destination selector/chips followed by its rates. Do not show four equally active empty selects
before their prerequisites; loading/empty/denied states visible and child levels disabled until lawful branch evidence.
Real country/area/city selection and add/remove must work. Required terminal path active check stays intact.
Rates: keep understandable primary fees/delivery visible; optional condition axes organized/expandable with clear units.
No hide-and-delete conditions, no numeric rounding (raw DecimalText), no guessed currency label/unit.
Display real currency when known; new area's currency comes from authoritative editor context/server, not a browser guess.
Readable type/name/payment/calculation blocks, sensible widths/spacing, reduced repetitive technical banners.
Replace "(پاک‌کردن صریح)" with ordinary user wording while keeping explicit clear semantics.
Follow accepted visual reference/design-system; no custom framework or global CSS patch.

C. TYPE EMPTY must have a usable outcome

Verify real catalog remains empty vs failed load. Use existing Fulfillment admin catalog APIs/permission gates.
For this explicitly development user trial, creating ONE real "عادی" type through existing authorized catalog create/update/
activate flow with the actual defaultLanguageId is authorized if the catalog is genuinely empty.
No raw SQL, invented GUID, fake client-only choice, auto-assignment or duplicate seed. If a suitable active type exists, reuse it.
Record exact code/id/revision/created vs reused. Let the operator explicitly choose it.
Empty-state UX must explain what is missing and expose the EXISTING actual type-management entry when one exists.
Do not link to a guessed route. Do not add a whole catalog CRUD subsystem in this task.
If no management UI exists, record that limit, but the real dev catalog option above must be selectable for trial.

D. TIMEZONE and validation

Do not make operators know a raw IANA/Windows id to avoid an unexplained save400.
Reuse existing timezone selector if available; otherwise small accessible feature-owned select/combobox using supported,
server-lawful ids with human labels. Human label "تهران" must resolve to a genuine selected id such as Asia/Tehran,
never be sent as if it were a valid id. Do not infer timezone from UI language/browser or silently default it.
Unknown custom text shows field-local reason before Apply; backend remains authority.
Preserve per-bound fractional time semantics and root-week ownership. Do not add DST/cutoff/scheduling policy.
Proof / completion gate (REAL save, not page-load-only)

Use actual mounted route with live backend:

name + explicit existing real type + one real destination (lawful label/ancestry) + one fee row120000/5000
with valid conditions1..2grams and delivery1..3 (use actual server currency).
explicit valid timezone selection and one lawful weekday interval if schedule is being configured.
Exactly ONE editor Apply PUT; record canonical validation/success and real returned revision/ids.
Fresh GET AND browser reload show exact saved destination label, type/name, fee tokens/values, schedule and new revision;
report whether the test used the user's draft or a separately identified local reproduction draft.
On deliberately invalid timezone/field, show specific field error and keep values; no fake success.
Take/read FA desktop screenshot(s) of the repaired relevant General/Regions state for comparison; mobile390 check for overflow
on affected form. Evidence-only until user Visual ACCEPT.
Focused tests for actual changed decoder/intent/error mapping + npm run typecheck; no all-feature/backend/solution battery.
If backend changed, one targeted test/build/runtime refresh sufficient. Preserve guards, do not widen baseline.
Max two bounded source/harness correction iterations; no repeated confidence runs. If genuine technical blocker remains,
report exact failing field/API and partial fix promptly, not "ready".
Keep user Host/FE running; cleanup only your temporary browser/harness. No credentials/screenshots with private payload in git.
Result / recovery

docs/architecture/evidence/TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209/RESULT.md:
before/after actual root causes, exact validation codes/properties and UI resolution, catalog/geo state,
real draft id/revision, realPUT+freshGET/reload values, visual artifacts actually inspected,
focused commands/counts, time/deviations, retained runtime/PID/source.
State READY_FOR_USER_RETEST only if real complete save+readback works; otherwise exactBLOCKED.
208 read-only handoff did NOT prove whole workflow; keep that correction in current recovery.
No user Visual ACCEPT/fullprogram/quote/activation claim.
Update recovery TOP and AGENTS current pointer. Explicit user dev authorization remains valid; no repeat approval.
Commit scoped source/docs, push/fetch parity; complete result then STOP_FOR_ARCHITECT. No automatic new task.
