# TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2 — RESULT

Base-SHA: `0c8f2803462442b8eda8aa51823420f196d7519f`
Status: **PASS** for the ONE prepared mounted save/adoption scenario, the pending-duplicate gate and the
four locale/viewport captures. Nothing here is production, database or live-server acceptance, and the
narrowed scenario does **not** complete all of 205's A–E.

## 1. Prepared scenario reused, not redesigned

The Architect's `scripts/save-scenario.mjs` is the only fixture source for this run: `initialResponseText`
(revision 7), `savedResponseText` (revision 8 with the assigned group `…7777-999999999999`, row
`…8888-999999999999` and matching `destinationFacts`), `expectedRequestText`, `assertScenarioRequest` and
`preflight` were imported unchanged. **One** preflight invocation was made and passed:

```text
{"ok":true,"result":{"initialRevision":7,"savedRevision":8,
"newGroupId":"77777777-7777-4777-8777-999999999999",
"newRowId":"88888888-8888-4888-8888-999999999999",
"validReadResponses":2,"invalidWriteAsReadRejected":true}}
```

Harness wiring only: editor GET → `initialResponseText` before the save and `savedResponseText` after it;
the one PUT is captured, `assertScenarioRequest(body)` is called, and the already-prepared response is
returned **only** if the assertion passes (otherwise the harness answers 400 and never fakes success). A
hold/release of that single paused PUT is added so a pending duplicate click can be measured. R1 fixtures
remain the source for countries/types/shell; the only branch correction is country2 (Germany) returning a
successful **empty** `areas`/`cities` pair, since the R1 area rows belong to country1. All other `/v1`
traffic fails locally.

## 2. Runtime

Task-owned frontend-only `next dev` on `127.0.0.1:3517` from `D:\temp\tb205-runtime\frontend`
(node_modules junction, `.env.local` = `TOOBA_HOST_ORIGIN=http://127.0.0.1:1`); ports 3517/9555 proven
free before start; installed Chrome `--headless=new` with a fresh task profile and CDP `:9555`;
interception installed before navigation. Setup ≈ 3 min (ready in 1904 ms).

## 3. The executed scenario

Real clicks/typing on production components at
`/admin/shipping-services/authoring/0a1b2c3d-…?mode=edit`:

- FA method name → `ارسال استاندارد R2`; switched to the EN **content** tab and set `Standard shipping R2`;
  switched back to FA and the FA value was still `ارسال استاندارد R2`.
- Monday (ISO 1) **end only** → `18:15`; start stayed `09:00` (raw `09:00:00.500` in the request) and no
  other day was touched.
- Regions → `افزودن گروه`; new group FA title `گروه آزمایشی`; in that group's real picker selected
  country `آلمان` (`…1111-2222`) and `افزودن این مقصد` (chip `کشور: آلمان`); added one nested row with
  freight `0.50`, packaging `0.00`, delivery `1..2`, all optional conditions blank. Retained group1
  (`استان: تهران`) and group2 (`شهر: قم`) were untouched, no reorder or removal happened.
- Apply once → the single PUT was **held**. `assertScenarioRequest` passed (`putAssert.ok === true`): the
  body is byte-for-byte the prepared expectation — `expectedRevision` 7, two retained groups with their
  read-shape areas and full stored rows, the new `groupId: null` / `rateRowId: null` group with its FA
  title patch, both edited translations, the whole week with Monday end `18:15:00` and start
  `09:00:00.500`, plus exact raw tokens `"freightAmount":0.50`, `"packagingAmount":1850000.00`.
- **Pending duplicate gate:** while held, the save control was already `در حال ذخیره…` and `disabled`; a
  real click on it produced **no** second request. `putCount` remained **1** before and after release.
- Release → `savedResponseText`; the UI left the saving/unknown state and entered **view** mode.
  Adoption observed in the DOM: FA name `ارسال استاندارد R2`, three groups with `گروه ۳` / `کشور: آلمان`,
  and the week `دوشنبه 09:00 تا 18:15` with every other day unchanged and exactly **one** root week.
  Revision 8 and the assigned ids came from the confirmed document, not from the fixture text alone.
- **No post-save GET:** editor GETs before Apply = 2 (StrictMode initial double read); after the save the
  count stayed at 2 — no refetch, no speculative second read.

## 4. Four locale/viewport captures

Interface locale was switched with the canonical `tooba_locale` cookie via CDP `Network.setCookie` and a
full `Page.reload` of the same actual route (not `--lang`, not the content-language tab, not `dir`):
DOM confirmed `lang=en` / `dir=ltr` after the switch versus `lang=fa` / `dir=rtl` before it.

| File | Locale | Viewport | Tab |
| --- | --- | --- | --- |
| `shots/01-fa-1440-regions-edit.png` | FA/RTL | 1440×900 | Regions (before Apply) |
| `shots/02-fa-1440-after-save.png` | FA/RTL | 1440×900 | Regions (adopted, view) |
| `shots/03-fa-1440-general.png` | FA/RTL | 1440×900 | General (adopted, view) |
| `shots/04-fa-390-regions.png` | FA/RTL | 390×844 | Regions |
| `shots/05-en-1440-regions.png` | EN/LTR | 1440×900 | Regions |
| `shots/06-en-390-general.png` | EN/LTR | 390×844 | General |

Observed: the two-part General/Regions structure and the single shared week render in both locales; EN/LTR
flips the whole chrome direction (`Back to shipping methods`, `Method information`, `Regions and costs`,
`Apply`) while stored Persian content stays RTL inside its own fields; the group list renders each group's
geography once, all ten rate axes are labelled per row, and no horizontal overflow appeared at 390×844 in
the captured frames. A reload of the actual route served the adopted revision-8 document (data came from
the prepared read response, so this is fixture-boundary persistence, not a database read).

## 5. Explicit remaining gaps (not claimed)

- Blank-name / incomplete-time / pending-timezone blocking, reject-discard and Cancel-Edit restoration.
- Scripted 409 and PUT transport-failure/`outcomeUnknown` + explicit reconcile (only the pending-duplicate
  gate and the successful path were exercised here).
- The full geography matrix (null-admin city and country-wide non-null parent through the picker, inactive
  option disabled state), group move/remove isolation, chip removal.
- The EN name is not visible in view mode because the default content language is FA (view shows the
  default-language name); its adoption is proven by the asserted request and the saved document, not by a
  separate EN-name view screenshot.
Consequently this run does not certify all of A–E and is not user Visual ACCEPT.

## 6. Limits and cleanup

No production file changed (no page/component/model/api/route/backend/Contracts/Host/schema/flag), no
package/config/baseline/skill/AGENTS change, no build/typecheck/suite sweep, no Bridge probe, no
`post-result.js` from a historical task. New content is task-owned only: this RESULT, the harness
`scripts/cdp-harness.mjs` + one-shot `scripts/preflight.mjs`, the six screenshots, the task receipt and one
recovery pointer. One harness correction was needed (a helper that reads the command result raced with the
writer) and is disclosed; the prepared scenario itself was never modified.

Cleanup: harness `node`, headless Chrome under the task profile and the task-owned `next dev :3517` tree
stopped; ports 3517 and 9555 verified **not listening** afterwards and the pre-existing `:3000` dev server
untouched.

Shared-Database-Data-Flag-Changes: NONE
Production-Functional-Acceptance: PENDING
Visual: REVIEW_EVIDENCE_ONLY
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
