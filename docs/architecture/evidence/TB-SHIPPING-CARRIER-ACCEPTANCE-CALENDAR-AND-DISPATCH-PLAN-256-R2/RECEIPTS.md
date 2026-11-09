# RECEIPTS — TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2

All receipts come from this task's own session on the canonical machine (2026-11-09, UTC). Nothing below
created an extra database, runtime or port; every database statement was a read through the owning
authoring API, and the only writes were the authorized editor-document saves on the task-owned draft.

## R1. Bridge dispatch (BRIDGE-WAKE-V1)

```text
GET  http://127.0.0.1:17321/api/tasks                          200
     -> only non-completed row:
        id=0a5b085b-0526-41b0-b596-378be25a4493
        taskId=TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2  status=Pending
GET  http://127.0.0.1:17321/api/tasks/next?channelId=tooba-main  200 (7301-byte TASK payload persisted here)
POST http://127.0.0.1:17321/api/workers/heartbeat                200
     {"workerId":"tooba-worker-01","channelId":"tooba-main","agentType":"cursor","status":"Working"}
```

## R2. Base and identity

```text
git rev-parse HEAD               9543b149e1d27210f823fafb94f80cd707b818be   (== issued Expected-Base-SHA)
git status --short               clean
```

Canonical runtime before the repair (unchanged for the whole task except the authorized FE rebuild):

```text
Host  http://127.0.0.1:5088/health   200 {"status":"ok"}      PID 21536 (not rebuilt, not restarted)
FE    http://127.0.0.1:3000/fa       200                      PID 38148 (old build) → 20404 (this build)
GET   http://127.0.0.1:5088/v1/admin/dev-context
      {"actorUserId":"01a036c2-970e-7000-8eb7-94bf5cc2d8db","tenantId":"store-alpha"}
```

Task-owned draft identity, verified BEFORE any write:

```text
GET /v1/admin/shipping-services/authoring-drafts/01a1beed-d7df-7000-bfc8-0185ea61afe3/editor-document
    owner.ownerKind=Store  serviceCode=tipax  optionCode=standard  revision=3
    weeklySchedule: Asia/Tehran, 60-minute slots, weekdays 1–5 09:00–17:00, 6–7 closed
    carrierSchedule: 7 configured days (weekday 3 = 10:00–13:00, weekday 5 closed)
```

User's own draft, read only (never written):

```text
GET /v1/admin/shipping-services/authoring-drafts/01a17d0d-e033-7000-af43-49a1daa28822/editor-document
    revision=2  serviceCode=post  optionCode=standard
    carrierSchedule = null       (unconfigured → the operator's real next step is configure/copy)
    regionGroups = 1 (one Resolved Country destination ایران, one rate row)
    updatedAt = 2026-10-27T11:10:28.561142+00:00
```

## R3. Reproduction of the user's defect (pre-fix, real browser)

Task-owned headless Chrome, fresh temporary profile, no user tab/profile, no screenshot.

```text
TB256R2_PHASE=before node scripts/tb256r2-authoring-save-dom.mjs
  ready=true
  before:            carrier open 10:00 / close 13:00, 7 carrier rows
  edit:              carrier weekday 3 close → 14:00
  edited:            applyDisabled=false, unsavedHint=true, carrierIssues=[]
  click Save:        disabledAfterClick=false, label unchanged
  after click:       0 status notice, 0 alert, unsavedHint=true, mode banner absent
  PUT count:         0     ← THE DEFECT: no request was ever sent
  reload:            carrier weekday 3 close = 13:00 (edit lost)
```

Persisted as `dom/save-before-fix.json`.

## R4. Focused tests

```text
cd src/frontend
npx tsx --test features/admin-shipping-authoring/authoring-save-carrier-only-256-r2.test.ts
  ✔ TB-256-R2: a carrier-only edit is a real unsaved patch and yields ONE ready Apply request
  ✔ TB-256-R2: the confirmed save clears the carrier patch and adopts the new revision
  ✔ TB-256-R2: an unconfigured-to-configured carrier calendar (copy) is also submittable
  ✔ TB-256-R2: a rejected carrier-only save keeps the operator's edits for correction
  ℹ tests 4  pass 4  fail 0

npx tsx --test features/admin-shipping-authoring/session/editor-session.test.ts \
                features/admin-shipping-authoring/session/editor-session-200-r2.test.ts \
                features/admin-shipping-authoring/authoring-carrier-schedule-256.test.ts \
                features/admin-shipping-authoring/authoring-timing-tabs-256-r1.test.ts
  ℹ tests 23  pass 23  fail 0

npx tsx --test features/admin-shipping-authoring/authoring-carrier-calendar.mount.test.ts
  ℹ tests 6  pass 6  fail 0
```

## R5. Typecheck and production build

```text
npm run typecheck   → exit 0 (tsc --noEmit, no output)
npm run build       → exit 0
                      .next/BUILD_ID = uXwhPh0ZnBqFWcpxujHPd
```

## R6. Deployment of the canonical frontend

```text
Stop-Process -Id 38148 -Force
Start-Process node node_modules/next/dist/bin/next start -p 3000 -H 127.0.0.1
  (WorkingDirectory D:\Users\User\source\repos\SarvNewVer\src\frontend,
   TOOBA_HOST_ORIGIN=http://127.0.0.1:5088,
   logs D:\temp\tb256r2-fe.out.log / D:\temp\tb256r2-fe.err.log)
FE_START_PID=20404 ; FE_HTTP=200 after 0s ; SERVED_BUILD=uXwhPh0ZnBqFWcpxujHPd
Host 5088 untouched (PID 21536), /health 200.
```

No backend source changed, so no backend publish was performed.

## R7. Real browser saves after the repair

`node scripts/tb256r2-authoring-save-dom.mjs` (carrier-only), then
`node scripts/tb256r2-authoring-save-cases.mjs` with `TB256R2_CASE=customer-only`,
`customer-restore`, `copy-then-edit`, `restore-carrier`.

```text
carrier-only   PUT ×1  200  rev 3→4  body: expectedRevision, regionGroups, methodTranslations, carrierSchedule
                               weeklySchedule in body: false
                          notice «تغییرات ذخیره شد (بازبینی 4).» ; mode→view ; reload close=14:00
customer-only  PUT ×1  200  rev 4→5  body has weeklySchedule, no carrierSchedule
                          reload: customer weekday3 10:00–17:00, carrier weekday3 still 10:00–14:00
customer-rest  PUT ×1  200  rev 5→6  customer weekday3 back to 09:00–17:00, carrier unchanged
copy-then-edit PUT ×1  200  rev 6→7  copy → carrier = customer week; edit carrier weekday3 close→16:00
                          body: only carrierSchedule; reload: carrier 09:00–16:00, customer 09:00–17:00
restore-carrier PUT ×1 200  rev 7→8  carrier weekday3 10:00–13:00, weekday5 closed, 6–7 09:00–17:00
                          customer week unchanged
```

Persisted as `dom/save-after-fix.json`, `dom/save-customer-only.json`,
`dom/save-customer-restore.json`, `dom/save-copy-then-edit.json`, `dom/save-carrier-restore.json`.

Final authoritative read after the last save:

```text
GET 01a1beed-…/editor-document  revision=8  updatedAt=2026-11-09T22:32:00.534329+00:00
   carrier day 1,2,4,6,7 = 09:00–17:00 · day 3 = 10:00–13:00 · day 5 closed
   weekly  Asia/Tehran, 60 minutes, weekdays 1–5 09:00–17:00, 6–7 closed   (unchanged throughout)
GET 01a17d0d-…/editor-document  revision=2  updatedAt=2026-10-27T11:10:28.561142+00:00 (untouched)
```

## R8. Constraints observed

- One database only: `tooba_alpha` on the canonical SingleStore/store-alpha instance. No disposable,
  test, parallel or Docker PostgreSQL, no port 15433, no second runtime or edition change.
- No migration, schema change, manual DDL, business SQL, reset or reseed.
- No checkout, payment, packing, parcel, dispatch, delivery, cancellation or publication mutation.
- No new draft/order/account; the user's draft was never written or published.
- No screenshots; only authorized DOM/HTTP facts.
- DataProtection preserved; CapturedPreview local ON / checked-in OFF; Tipax deferred; 255 parked.
- No force/amend/reset/stash/rebase; commit + push only.

## R9. Unrelated observations (not fixed here)

- Pre-existing site-wide `OPTIONS http://localhost:3000/fa` 400 CORS preflight; plain `GET` is 200.
- Pre-existing `frontend-source-size` 18-finding family; no baseline file edited by this task.
- `slotDurationMinutes` is `null` on the user's own draft (full-window legacy shape) — unrelated to
  this save defect and deliberately not changed.

## R10. Commit, push and final state

```text
Base-SHA (issued)                     9543b149e1d27210f823fafb94f80cd707b818be
Code-SHA (repair + tests + evidence)  fc526c019db73f413e0c41e2d57f958bc114299b   2026-11-10T02:05:44+03:30
Reconciliation merge (docs only)      9219a582ed40d7c9824fec0af48590fe27f9365e   merges origin/main 5 docs commits
git push origin main                  e2cbda84b..9219a582e  main -> main
git status -sb                        ## main...origin/main   (local == origin/main, clean)
```

The push required reconciling five documentation commits that origin/main had advanced while this task ran
(AGENTS.md, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md, both shipping planning records and this task's
own TASK.md). Only documentation banners/ledger text conflicted; no code file, test, script or evidence
JSON was in conflict. The resolution keeps this task's CURRENT banner on top and marks the earlier
issue banners SUPERSEDED, so no unrelated work was overwritten, dropped or force-pushed. Normal
merge+push only — no force/amend/reset/stash/rebase.

Runtime at result time, re-verified after the push:

```text
FE    http://127.0.0.1:3000/fa        200      PID 20404   BUILD_ID uXwhPh0ZnBqFWcpxujHPd
Host  http://127.0.0.1:5088/health    200 {"status":"ok"}   PID 21536 (unchanged, not rebuilt)
```

Elapsed: claimed 2026-11-09T22:18:20Z → result 2026-11-10T02:12Z ≈ 3.9 h wall clock, single worker,
one task at a time.

The evidence envelope commit is the commit that adds this §R10 (its full 40-char SHA is carried by the
Bridge worker result, so no self-referential placeholder is needed here).
