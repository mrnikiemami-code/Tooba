# TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R3 — Result

Mode: BOUNDED_MOUNTED_DISCARD_CONFLICT_AND_UNKNOWN_WORK_PROTECTION
Expected-Base-SHA: `61043bb5d699a55145ec18fb8d03ce3bd82e2f28` (verified `HEAD == origin/main` before work)
Worker: `tooba-worker-01` / `cursor` / channel `tooba-main`
Task row: `9f91a3b1-cbc0-4d59-a29d-47cfacdff594`
Claimed: 2026-10-24T16:19:54Z. Last interaction: 2026-10-24T16:30:30Z.

## Runtime (task-owned, isolated)

- Frontend `127.0.0.1:3517` (`next dev`, `TOOBA_HOST_ORIGIN=http://127.0.0.1:1`, fail-closed), installed dependencies/junction, isolated output log.
- Chrome `--headless=new` fresh task profile, CDP `:9555`; `Fetch.enable` installed BEFORE navigation to the actual route `/admin/shipping-services/authoring/<IDS.draft>?mode=edit`.
- Harness reused from 205-R2 (`scripts/cdp-harness.mjs` copied into 205-R3 with only the permitted error-plan ops: canonical-409 `reject`, `failput` transport failure, and a deliberate fresh-trial reset). Prepared save scenario / success fixture unchanged; no fixture redesign.
- All `/v1/*` synthetic and fail-closed; no real merchant traffic, no shared writes. Ports `3517`/`9555` proven free before start; shared `:3000` untouched (still listening at end).

## Control 1 — invalid + reject/confirm Cancel Edit — PASS

Fresh initial revision 7. Local work via real DOM typing/clicks: FA General description `توضیح موقت R3`, Monday start `09:00 → 08:45`, existing rate freight `0.50 → 0.75`; then the stored FA name blanked (`ارسال استاندارد → ""`).

Observed DOM state while invalid:

```
{"name":"","desc":"توضیح موقت R3","nameIssue":true,"dirty":true,"applyDis":true,"times":["08:45","17:30"]}
```

- Visible name-required issue (`نام این زبان نمی‌تواند خالی بماند.`), `اعمال` disabled, dirty indication present.
- `انصراف از ویرایش` opened the real discard dialog (`تغییرات اعمال‌نشده`, actions `بازگشت به ویرایش` / `خروج بدون ذخیره`).
- Reject (`بازگشت به ویرایش`) retained everything:

```
{"name":"","desc":"توضیح موقت R3","times":["08:45","17:30"],"nameIssue":true,"dirty":true,"editable":true}
{"freight":"0.75"}   // Regions tab still shows the edited rate
```

  `putCount` delta = 0 for the whole reject path.
- Confirm (`خروج بدون ذخیره`) returned to view mode and restored stored values:

```
{"mode":"view","name":"ارسال استاندارد","desc":"ارسال عادی"}
view text: گروه ۱ … استان: تهران … گروه ۲ … شهر: قم   (2 groups, stored geography)
```

- Re-entering Edit showed no discarded residue:

```
{"name":"ارسال استاندارد","desc":"ارسال عادی","times":["09:00","17:30"],"nameIssue":false,"dirty":false,"applyDis":true}
{"groups":["گروه تهران","گروه دوم"],"row1freight":"0.50"}
```

  Clean state (Apply disabled because nothing is dirty), same group count/titles/ids, original rate and week restored. `putCount` delta 0, `getCount` unchanged.

## Control 2 — canonical 409 preserves work, no retry — PASS

Fresh trial: initial document reloaded (GETs 2 → 4, StrictMode double-read recorded separately), edit `نام تعارض R3`, `اعمال` → held PUT (`putCount` 0 → 1). Fulfilled the held request with HTTP 409 and the canonical envelope grounded in `ShippingErrorCodes.AuthoringDraftRevisionConflict` + the adapter's 409 mapping:

```
{"errorCode":"shipping.authoring.draft_revision_conflict","errors":{"expectedRevision":["stale"]}}
```

Observed:

```
{"conflict":true,"kept":"نام تعارض R3","dirty":true,"mode":"edit","applyDis":false}
message: "این پیش‌نویس هم‌زمان تغییر کرده است؛ تغییرات شما حفظ شد."
put=1 gets=4   (after an extra 3s of observation)
```

- Conflict surface shown, typed name and local work preserved and still dirty, still in Edit.
- Server revision unchanged (only the initial read document was ever served; the harness never promoted a write).
- Request count stayed exactly 1 for the attempt over repeated/delayed observation: no automatic retry and no post-save GET.

## Control 3 — transport unknown → explicit reconcile, zero replay — PASS

Fresh trial: initial document reloaded, edit `نام نامعلوم R3`, `اعمال` → held PUT, then `Fetch.failRequest` (ConnectionAborted) as a real transport failure.

Unknown state:

```
{"kept":"نام نامعلوم R3","dirty":true,"applyDis":true,"reconcile":true}
put=1 gets=2
```

- Explicit "unknown" message + explicit `خواندن وضعیت از سرور` action, edits kept, Apply blocked.
- Additional local edit (`نام نامعلوم R3b`) did not clear the latch and sent nothing:

```
{"keep":"نام نامعلوم R3b","applyDis":true,"reconcile":true}
```

- One click on the explicit reconcile action, served the initial valid read document (this fixture branch models no committed write, not a guessed real outcome):

```
{"name":"نام نامعلوم R3b","reconcile":false,"mode":"edit","dirty":true}
put=1 gets=3
```

  GET delta exactly +1 attributable to that explicit action only; uncertainty cleared, typed work retained, no automatic PUT/replay, no second reconcile. Stopped before any further deliberate save.

## Deviations / limits

- One harness correction used (permitted maximum): the fresh-trial reset left the first-control renderer unresponsive (`Runtime.evaluate`/`captureScreenshot` timeouts, no new document GET on reload). The harness was restarted once and Control 3 was then executed normally; no product defect reproduced, no retry loop.
- Control 3 was executed after the minute-10 new-interaction stop point (last interaction ~minute 10.6); setup stayed inside budget. Timing compliance for this sub-box is therefore not claimed as PASS.
- No screenshot set produced (optional in this task; the observed DOM states are recorded above as text). Visual ACCEPT from 205-R2 evidence is reused; no new Visual ACCEPT claimed.
- No full geography/group move/remove matrix, no happy-path repeat, no new feature, no production file touched.
- A-E breadth remains model-tested, NOT mounted-certified beyond the three controls above.

## Status

- Production-Functional-Acceptance: PENDING
- Shared-Database-Data-Flag-Changes: NONE
- Visual: existing 205-R2 evidence reused; no new Visual ACCEPT
- Stop-State: STOP_FOR_ARCHITECT
- Automatic-Next-Task: NONE
