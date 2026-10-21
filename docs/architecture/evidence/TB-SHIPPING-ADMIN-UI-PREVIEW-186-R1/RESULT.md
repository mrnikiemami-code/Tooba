# TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1 — Repair result evidence

Task: `TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1` (Parent: `TB-SHIPPING-ADMIN-UI-PREVIEW-186`)
Program: `TOOBA-SHIPPING-ADMIN-UI-ACCEPTANCE`
Mode: `BOUNDED_VISUAL_PREVIEW_PICKER_DRAFT_VALIDATION_REPAIR`
Date: 2026-10-21 (Asia/Tehran)
Stop-State: `STOP_FOR_ARCHITECT`

## 1. SHAs

| Field | Value |
| --- | --- |
| Expected-Base-SHA | `4b7744eb0f7ba79d93e1ab063802bfa25aa1f14a` |
| Base-SHA (verified before edits) | `4b7744eb0f7ba79d93e1ab063802bfa25aa1f14a` |
| Code-SHA | `b0ebe77985df14e771c869e093f3d203481d01a4` |
| Evidence-SHA | the next scoped commit that adds this file; its exact hash is transmitted in the Bridge result payload (`Evidence-SHA`), because a file cannot contain the hash of its own commit |

`git fetch origin` was run first; local `main` equalled `origin/main` at the expected base
(`HEAD == origin/main == 4b7744eb`) before any edit. No reset/rebase/stash/force-push was used.

## 2. Source repairs (three Architect findings)

### A. Geography — browse is not add (`preview-region-picker.tsx`, `preview-model.ts`)

- `preview-model.ts` gains `previewPlaceCandidateState(selectedIds, candidateId, places)` returning
  `{ placeId, addable, conflict }`: `addable` is true only when the candidate is free, while an
  ancestor/descendant/duplicate of an accepted selection is `addable: false` with a typed conflict.
- The picker no longer sets `disabled` on `<option>` (the exact defect that made Iran unreachable after
  Golpayegan was added). Every level stays navigable; non-addable options are labelled
  `«<نام> (فقط مرور)»` / `"(browse only)"` and carry `data-browsable="true"` + `data-addable`.
- Only the Add button binds to `addable`. The status line shows either the localized addable message
  (`shipping-preview-picker-addable`, emerald) or the localized conflict reason
  (`shipping-preview-picker-conflict`, amber, e.g. «والدِ «گلپایگان» است که قبلاً اضافه شده است.»).
- Ancestry validation was not removed: adding an ancestor/descendant/duplicate is still blocked,
  but hierarchy traversal is not.

### B. Draft safety (`design-preview-screen.tsx`, `preview-regions-tab.tsx`)

- `PreviewRegionsTab` reports its open row editor to the shell through `onPendingInputChange`, so
  pending row input is first-class dirty state next to the method draft (`hasUnappliedWork = dirty || pendingRowInput`).
- Owner change goes through `changeOwner`: with unapplied work it asks for the localized confirmation
  `discardAndChangeOwner`; Cancel re-keys the owner `<select>` (`ownerSelectKey`) so the control snaps
  back to the current owner, keeping the owner, the route/editor and every field value.
- Back-to-list uses the same `hasUnappliedWork` predicate, so method draft and pending row input are
  handled consistently.
- Both tab panels stay mounted (`hidden={tab !== "general"}` / `hidden={tab !== "regions"}`), so an
  open row editor keeps its typed values across tab switches.

### C. First-submit validation (`preview-rate-row-editor.tsx`, `preview-model.ts`)

- Row editing state is the raw string input (`PreviewRowRawInputs`), not a derived numeric row.
- `previewRowInputErrors(raw)` runs on the raw text before any conversion/rounding:
  `notNumber` for non-finite/empty-unparsable, `negative`, `notInteger` for fractional count/day, and
  `inverted` for `max < min`. `previewRowFromRawInputs` converts only after validation
  (kilograms → integer grams at the existing precision; no financial rounding redefinition).
- `submit()` recomputes errors synchronously from the current raw state, so the first click on
  `ثبت ردیف` rejects `min > max` without relying on the previous render.

## 3. Real-browser proof (ordinary controls, DOM assertions)

Browser: Cursor IDE browser (Chromium) against the running Next dev server. Sequences below used real
`click`/`select_option` interactions on the visible controls; no JavaScript mutation of input/select
state and no forced selection was used.

Clean method `ارسال رایگان سوپرمارکتی` (0 groups) → `ویرایش` → tab `مناطق و هزینه‌ها` → `افزودن گروه مناطق`:

| Step | Observed |
| --- | --- |
| Country = ایران → استان = اصفهان → شهر = گلپایگان → `افزودن` | accepted; selected list renders readable path `ایران › اصفهان › گلپایگان`; picker resets |
| Reopen picker: country options | `ایران (فقط مرور)` — navigable, not disabled |
| Iran → استان قم → شهر قم → `افزودن` | accepted; selected list now `ایران › اصفهان › گلپایگان`, `ایران › قم › قم` |
| Candidate = Iran (with Golpayegan accepted) | `افزودن` disabled; status `والدِ «گلپایگان» است که قبلاً اضافه شده است.`; province اصفهان also labelled `(فقط مرور)` |
| Second group (`افزودن گروه مناطق` #۲), country ایران | `افزودن` disabled with the same cross-group reason; قم and اصفهان labelled `(فقط مرور)` |
| Second group, Isfahan → city list | `گلپایگان (فقط مرور)`; `افزودن` disabled → duplicate blocked across groups of the same method |
| Rate-row editor (`افزودن ردیف`) | 0 `<select>` elements inside the editor (no geography in the row) |

Row validation (`ارسال سریع درون‌شهری`, new group, `افزودن ردیف`):

| Step | Observed |
| --- | --- |
| delivery min `5`, max `2`, count-from `1.5`, first click `ثبت ردیف` | editor stays open; both delivery fields show `بیشترین روز تحویل نباید کمتر از کمترین باشد.`; both count fields show `این فیلد باید عدد صحیح باشد.`; row counter unchanged (`۰ ردیف`), no row added |
| Correct to min `2` / max `2` and clear count-from, click `ثبت ردیف` | applied once; editor closed; row counter increased |

Draft safety:

| Step | Observed |
| --- | --- |
| Pending row inputs `1.5` / `5` / `2` with visible errors, switch to `اطلاعات روش` and back to `مناطق و هزینه‌ها` | editor still open with the same three values and the same two error messages (panel kept mounted) |
| Unapplied method edit + open row editor, change owner select | localized discard confirmation appears; Cancel keeps owner `مارکت‌پلیس همین فروشگاه`, the open editor, and all values |

Language / width:

| Check | Observed |
| --- | --- |
| Interface `EN` | preview heading `Shipping — design preview`; preview region `dir="ltr"`, `lang="en"`; back to `فارسی` restores RTL |
| 390 px | `innerWidth` 390, `documentElement.scrollWidth` 375 → no page-wide horizontal overflow |
| 1024 px | `innerWidth` 1024, `documentElement.scrollWidth` 1009 → no page-wide horizontal overflow |

## 4. Commands actually run

```text
git status / git log / git rev-parse HEAD, origin/main                → clean parity at the expected base
node --experimental-strip-types --test app/admin/shipping-services/design-preview/preview-model.test.ts
                                                                      → 15/15 PASS (10 pre-existing + 5 new R1 regressions)
npm run typecheck (tsc --noEmit)                                      → PASS (0 errors)
npm run test:architecture                                             → 6 guard files; only the pre-existing size guard fails (see §5)
git commit / git push origin main                                     → see §1
```

New focused tests added (all PASS):

1. `previewPlaceCandidateState` marks an ancestor as non-addable while `previewPlaceConflict` keeps
   returning a conflict — i.e. browsing stays possible and adding stays blocked.
2. Conflict domain spans groups of the same method, not another owner.
3. First submit rejects `min > max` delivery days computed from raw input (before conversion).
4. Integer/numeric validation happens on raw values before rounding (fractional count, non-numeric text).
5. The picker renders no `disabled` option and both tab panels stay mounted (source-level guard).

Note recorded for the Architect: the ten pure-model tests that passed in 186 did **not** prove dropdown
reachability; they never asserted navigability, which is why the picker defect shipped.

## 5. Pre-existing failure (not caused here, not repaired)

`lib/architecture/frontend-source-size.guard.test.ts` fails on `main` at the expected base with the same
four untouched legacy files listed in 186:

```text
OVERSIZED_GROWTH src/frontend/app/admin/category-admin-screen.tsx: 2230 > baseline 2215
OVERSIZED_GROWTH src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx: 1442 > baseline 1440
OVERSIZED_GROWTH src/frontend/app/storefront/storefront-landing-blocks.tsx: 1033 > baseline 1026
OVERSIZED_GROWTH src/frontend/lib/storefront-composition/shared-composition-renderer.tsx: 918 > baseline 907
```

None of these files is touched by this task; no baseline was refreshed (out of scope).

## 6. Business-network-write check

`performance.getEntriesByType("resource")` filtered to `/v1/` or API paths, after the full browser
interaction session (group creation, 2 geography adds, second group, blocked candidates, row editor,
tab round-trips, language switch, two viewport widths), contains only the pre-existing Admin shell reads:

```text
/v1/admin/dev-context
/v1/admin/access-control/me/capabilities
```

They appear twice because the page was reloaded once for a clean in-memory draft; both are shell reads
performed at load (the Host is not running), unrelated to the preview. No POST/PUT/PATCH/DELETE was
issued by the preview, and the static scan in `preview-model.test.ts` still asserts the absence of
`fetch(`/`XMLHttpRequest`/`localStorage`/`sessionStorage`/`document.cookie`/write methods in preview source.

`Business-Network-Writes: ZERO`

## 7. Process state and preview URL

- URL: `http://127.0.0.1:3419/admin/shipping-services/design-preview`
- Listener: task-owned Next dev server on loopback `:3419`, PID `18428` (`node`), left running for the
  user's visual review; its log stays under the existing git-ignored `.tmp-186-preview/next-3419.log`.
- Shared `:3000`, `:5088`, `:5432`, DB/Redis/RabbitMQ were not used, started or restarted; `.next` was not
  deleted. No package install, browser download or cache purge was performed.

## 8. Changed files

```text
src/frontend/app/admin/shipping-services/design-preview/preview-model.ts
src/frontend/app/admin/shipping-services/design-preview/preview-model.test.ts
src/frontend/app/admin/shipping-services/design-preview/preview-region-picker.tsx
src/frontend/app/admin/shipping-services/design-preview/preview-regions-tab.tsx
src/frontend/app/admin/shipping-services/design-preview/preview-rate-row-editor.tsx
src/frontend/app/admin/shipping-services/design-preview/design-preview-screen.tsx
src/frontend/app/admin/shipping-services/design-preview/preview-i18n.ts
docs/ai/tasks/TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1.task.md            (received artifact, verbatim)
docs/architecture/evidence/TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1/      (receipt, task, RESULT)
```

Only four new dictionary keys were added (`discardAndChangeOwner`, `pickerBrowseOnly`,
`pickerCandidateAddable`, `rowErrorNotNumber`) in both `fa` and `en`; the preview dictionary remains
task-scoped. No production shipping path, DTO/API client, schema, migration, feature flag, global CSS,
shared design-system component, Storefront surface or navigation was touched.

## 9. Elapsed and open gates

- Elapsed: ~18 minutes from claim to evidence commit (Hard-Timebox 45 minutes; not exceeded).
- `USER_VISUAL_ACCEPTANCE = PENDING`
- `PRODUCTION_FUNCTIONAL_IMPLEMENTATION = NOT_STARTED`
- `Automatic-Next-Task = NONE`; state ends at `STOP_FOR_ARCHITECT`.
