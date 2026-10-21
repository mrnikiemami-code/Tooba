# TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2 — Per-weekday schedule repair evidence

Task: `TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2` (Parent: `TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1`)
Program: `TOOBA-SHIPPING-ADMIN-UI-ACCEPTANCE`
Mode: `BOUNDED_MULTILINGUAL_PER_DAY_SCHEDULE_VISUAL_REPAIR`
Date: 2026-10-22 (Asia/Tehran)
Stop-State: `STOP_FOR_ARCHITECT`

## 1. SHAs

| Field | Value |
| --- | --- |
| Expected-Base-SHA | `4a37697eef577963c08519616f44ff035ceb4cb1` |
| Base-SHA (verified before edits) | `4a37697eef577963c08519616f44ff035ceb4cb1` |
| Code-SHA | `65b2cfbd4e8e9e1ff64e4d319f2af2954977a2db` |
| Evidence-SHA | the next scoped commit that adds this file; its exact hash is transmitted in the Bridge result payload (`Evidence-SHA`), because a file cannot contain the hash of its own commit |

`git fetch origin` was run first; local `main` equalled `origin/main` at
`4a37697eef577963c08519616f44ff035ceb4cb1` before any edit. No reset, rebase, stash or
force-push was used, and unrelated dirty files were preserved.

## 2. Root cause

R1 still modelled the weekly schedule as `weekdays: PreviewWeekdayId[]` plus a single
`fromTime`/`toTime` pair. Selecting a day only added it to one shared interval, so every
selected day necessarily shared identical hours and the owner could not give Saturday
`09:00–13:00` and Sunday `15:00–18:00` in one method. The collapsed summary repeated that
shared interval, which made the defect look like intended behaviour.

## 3. Final weekday structure (preview-only)

`preview-model.ts`:

```ts
export const PREVIEW_WEEKDAY_IDS = ["sat", "sun", "mon", "tue", "wed", "thu", "fri"] as const;
export type PreviewWeekdayId = (typeof PREVIEW_WEEKDAY_IDS)[number];

export type PreviewDayHours = { working: boolean; fromTime: string; toTime: string };

export type PreviewSchedule = {
  hours: Readonly<Record<PreviewWeekdayId, PreviewDayHours>>;
  cutoffHours: number; // unchanged method-level meaning
};
```

- Weekday identity is a stable code (`sat`…`fri`); translated labels (`شنبه` / `Saturday`)
  exist only in `preview-i18n.ts`, so interface locale never participates in data identity.
- `previewSampleSchedule(cutoffHours)` builds a fresh record per method; sample defaults are
  explicit (`09:00–18:00`, Saturday–Wednesday working) instead of inferred business hours.
- `clonePreviewSchedule` / `clonePreviewMethod` deep-copy every day object, so drafts never
  alias fixtures, other methods or the other owner.
- Helpers added: `previewDayHoursFieldErrors`, `previewDayHoursIssue`, `previewScheduleIssues`,
  `previewScheduleIsValid`, `previewScheduleActiveDays`, `previewScheduleSegments`,
  `previewScheduleWithDay`.
- `previewDayHoursIssue` returns `notBefore` when `fromTime >= toTime` on an active day and
  `missing`/`notTime` for empty or malformed input. Nothing is rounded, reversed or extended
  overnight, and no "at least one active day" rule was invented.

`preview-schedule-editor.tsx` (new) renders one compact weekly table: seven labelled rows,
each with an accessible working/closed checkbox and its own `from`/`to` time inputs. Closed
days render the explicit closed state with inactive time controls and keep their typed draft
hours, so reopening restores them. View mode disables every control.

`preview-general-tab.tsx` binds `<PreviewScheduleEditor />`, renders the truthful collapsed
summary from `describePreviewScheduleSummary` and shows an all-closed notice when no day is
active. `design-preview-screen.tsx` blocks the first Apply synchronously with
`previewScheduleIsValid(draft.schedule)` plus a localized banner, leaving typed values editable.

## 4. Fixtures and summaries

- `preview-fixtures.ts` builds sample methods from `sampleSchedule(...)`; `method-express`
  demonstrates the owner example: Saturday `09:00–13:00`, Sunday `15:00–18:00`,
  Monday `09:00–18:00`, Tuesday `10:00–14:00`, Wednesday `09:00–12:30`,
  Thursday `16:00–19:00`, Friday closed, cutoff 4 h.
- `previewScheduleSegments` merges contiguous days that genuinely share identical hours and
  keeps distinct hours separate, so the collapsed line reads
  `Saturday 09:00–13:00, Sunday 15:00–18:00, …, Friday closed · order cutoff 4 h` instead of
  a false shared interval; all-closed renders a truthful closed message rather than an empty
  or invented window.

## 5. Real-browser proof

Browser: Cursor IDE browser (Chromium) against the task-owned Next dev server on
`:3419` (PID 18428, already listening before this task; no second server was started).

Ordinary control interactions were used for the behavioural sequence; DOM reads below are
observations of real rendered values, not forced state.

| # | Interaction (ordinary controls) | Observed |
| --- | --- | --- |
| 1 | List → `مشاهده`/`Edit` on sample `ارسال همان‌روزه شهری` → expand `جزئیات برنامه` | seven day rows with independent `from`/`to` inputs |
| 2 | Set Saturday `09:00–12:00`, Sunday `16:00–20:00` | Sunday edit left Saturday unchanged, and vice versa |
| 3 | Toggle Friday closed → working | only Friday changed; Saturday/Sunday hours untouched |
| 4 | Set Wednesday start `17:00` with end `12:30` → `اعمال در پیش‌نمایش` | Apply rejected synchronously: field error on Wednesday plus error banner; typed `17:00` kept |
| 5 | Correct Wednesday to `09:00` → Apply | preview applied; nothing persisted or sent anywhere |
| 6 | Edit Saturday to `09:00–11:00` → `مناطق و هزینه‌ها` tab → back to `اطلاعات روش` | unapplied draft value retained |
| 7 | Switch interface language to `EN`, then content language to English | all draft values and the closed/open state preserved; labels localized |
| 8 | `بازگردانی نمونه` | sample schedule restored; notice confirms original sample data |
| 9 | View mode (`مشاهده`) with schedule expanded | all seven day toggles and twelve time inputs report `disabled` + `readonly`; no Apply control |

Discard protection (same browser, ordinary control clicks; `window.confirm` temporarily
instrumented to record the prompt and answer Cancel, so the Cancel branch could be observed):

| Step | Observed |
| --- | --- |
| Draft name change → change owner select to `Seller (sample)` | prompt recorded `Changing the owner discards the unapplied draft and any open row input. Continue?`; owner stayed `marketplace`, draft name kept, editor open, unsaved badge visible |
| Same draft → `Back to list` | prompt recorded `Go back without applying your changes?`; stay on the editor with the draft intact |
| `Restore sample` after removing the instrumented confirm | sample data restored, page reloaded to pristine in-memory state |

### Responsive / direction measurements (real layout reads)

`document.documentElement.scrollWidth` vs `clientWidth` and the schedule section's own
`scrollWidth` vs `clientWidth`:

| Width | Page overflow | Section overflow | Time-input width | Max control right edge |
| --- | --- | --- | --- | --- |
| 390 (fa, RTL) | no (`375 == 375`) | no (`285 == 285`) | 130 px | 330 |
| 1024 (fa, RTL) | no (`1009 == 1009`) | no (`607 == 607`) | 231 px | 680 |
| 1440 (fa, RTL) | no (`1425 == 1425`) | no (`1023 == 1023`) | 439 px | 1096 |
| 390 (en, editor subtree `direction: ltr`) | no | no (`285 == 285`) | 130 px | 160 |

Closed Friday renders no time inputs: 12 `input[type=time]` elements standing for six open
days. Under English the schedule section computes `direction: ltr` with `text-align: start`,
and all seven English weekday labels render.

## 6. Commands and results

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test app/admin/shipping-services/design-preview/preview-model.test.ts` | 21/21 pass, 0 fail (includes six R2 regressions: independent weekdays, clone isolation, closed-day semantics, first-Apply rejection, honest collapsed summary, per-day controls + View immutability) |
| `npm run typecheck` | `tsc --noEmit`, 0 errors (~4 s, inside the 180 s bound) |
| `node --experimental-strip-types --test lib/architecture/frontend-import-boundary.guard.test.ts …` (import-boundary, flat-folder, feature-boundary, test-discovery) | 7/7 pass |
| `npm run test:architecture` | known unrelated baseline failure: `frontend-source-size.guard` reports `OVERSIZED_GROWTH` for `category-admin-screen.tsx` (2230 > 2215), `admin-landing-page-composer.tsx` (1442 > 1440), `storefront-landing-blocks.tsx` (1033 > 1026), `shared-composition-renderer.tsx` (918 > 907). None of these files is touched by this task and they are absent from `git status`; not fixed or refreshed |

## 7. Changed files

```text
src/frontend/app/admin/shipping-services/design-preview/design-preview-screen.tsx
src/frontend/app/admin/shipping-services/design-preview/preview-fixtures.ts
src/frontend/app/admin/shipping-services/design-preview/preview-general-tab.tsx
src/frontend/app/admin/shipping-services/design-preview/preview-i18n.ts
src/frontend/app/admin/shipping-services/design-preview/preview-model.test.ts
src/frontend/app/admin/shipping-services/design-preview/preview-model.ts
src/frontend/app/admin/shipping-services/design-preview/preview-schedule-editor.tsx   (new)
docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md                            (section 12 only)
docs/ai/tasks/TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2.task.md                              (receipt)
docs/architecture/evidence/TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2/**                      (this evidence)
```

Diff for the source slice: 6 files changed, 568 insertions(+), 109 deletions(-), plus the new
editor file. No shared component, global CSS, dependency, schema, DTO, client, backend,
Storefront, navigation or feature-flag change; `/admin/shipping-services` live screens are
unchanged.

## 8. Preview runtime

- URL: `http://127.0.0.1:3419/admin/shipping-services/design-preview`
- Start command: `cd src/frontend && npm run dev -- -p 3419`
- Listener: task-owned dev server already healthy on `:3419` (PID 18428); the attempted second
  start failed with `EADDRINUSE` and was not retried, so no other process or shared port was
  disturbed. No Host/DB/Redis/RabbitMQ was started, no carrier call was made and no business
  network write happened (`ZERO`).

## 9. Boundary statement

This is a technically verified bounded visual preview repair. `USER_VISUAL_ACCEPTANCE`
remains PENDING, `PRODUCTION_FUNCTIONAL_IMPLEMENTATION` remains NOT_STARTED, and no server
validation, persistence, seller authorization or real freight calculation exists in this slice.
