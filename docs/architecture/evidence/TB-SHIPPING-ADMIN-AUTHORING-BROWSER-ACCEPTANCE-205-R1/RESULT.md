# TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1 — RESULT (PARTIAL)

Base-SHA: `be286fcb788aee83c3fd5733221065bc470d1e15`
Status: **PARTIAL** — harness/fixture defects repaired and a real mounted General+save path was driven;
group/geography creation, week edit, 409/transport recovery and the EN-LTR screenshots were **not**
executed inside the box. Not production, database or visual acceptance.

## 1. Harness defects repaired (from 205 ARCHITECT-REVIEW)

| # | Defect | Repair (verified in the run) |
| --- | --- | --- |
| 1 | null-admin city emitted `parentId: "null"` (string) | `childOption` now emits JSON `null` when the parent is null; GUID parents stay quoted |
| 2 | group1 selected Country+Admin+City+ServiceArea (parent/descendant overlap) | fixtures now seed **disjoint terminals** only: group1 = AdministrativeArea `…1111`, group2 = null-admin City `…4444`. No production overlap validation was touched |
| 3 | country-wide and filtered city lists identical | two separate envelopes: `citiesFiltered` (exactly the requested `administrativeAreaId` parent, no null-parent row) and `citiesCountryWide` (null-admin + in-country non-null parent), selected by the presence of `administrativeAreaId=` in the request |
| 4 | PUT response was the original document with only text-replaced revision | response is now a **prepared full document**: current-document blocks for `weeklySchedule` / `regionGroups` / `methodTranslations` replaced by this request's own text (brace-balanced splice, so real number tokens survive), plus new revision |
| 5 | `authoring-types` treated as shell traffic | now fulfilled with the real `{ types: [...] }` envelope of `decodeAuthoringTypeChoices` |
| 6 | CDP `__error` treated as success | every CDP call returns a timeout/`protocol:` error and the runner stops classifying it as success; BOM strip and `*/v1/*`-only Fetch interception retained |

`dev-context` (`{ actorUserId }` of `prepareAdminDevActor`) and `me/capabilities`
(`{ userId, roleCodes, permissions[] }` of `mapEffective`) are whitelisted with the real reader shapes.
Unhandled `/v1` still fails locally: **0** blocked calls remained in this run because every envelope the
page actually requested is now whitelisted.

## 2. Correction to the 205 finding (append-only)

205 attributed the `بررسی پیش‌نویس ناموفق بود.` banner to blocked shell reads. The Architect's source
inspection is correct and is CONFIRMED by this run: `authoring-types` is a **feature** read consumed by
the type selector, its failure renders `messages.entryFailed`, and the banner is that. Once the real
`types` envelope was served, the banner disappeared (`banner: false` in the run's pre-state) and the type
select rendered the stored type. This is a fixture-boundary effect, not a product defect. The 205
RESULT is left intact and corrected only by this note.

## 3. Scenarios actually executed (real mounted route, real clicks/typing)

Route `/admin/shipping-services/authoring/0a1b2c3d-…?mode=edit`, real `ShippingAuthoringScreen`.

- **A (General, partly):** loaded from fixture with the banner absent. In the فارسی tab the name field
  (content-language `dir="rtl"`) went `ارسال استاندارد` → `ارسال استاندارد R1`; Apply became enabled and
  the dirty indicator «تغییر اعمال‌نشده دارید» appeared. Switched to the انگلیسی tab: the field switched to
  `dir="ltr"` with the EN value `Standard shipping` → set to `Standard shipping R1`. Switched back to
  فارسی: the FA edit was **retained** (`ارسال استاندارد R1`), proving per-language retention, not a shared
  field. Not executed: blank-name issue, discard/reject, Cancel-Edit restoration.
- **D (one combined save, partial):** Apply produced **exactly ONE PUT** and **no post-save GET**.
  Recorded request facts: `expectedRevision == 7` (equal to the document revision the page had loaded);
  body keys `expectedRevision, methodTranslations, regionGroups`; both retained groups (`…7777-1111`,
  `…7777-2222`) and retained rate row `…8888-1111` present; exact raw number tokens `0.50` and
  `1850000.00` present; `methodTranslations` carried both edited names with their own `languageId`
  (FA `ارسال استاندارد R1`, EN `Standard shipping R1`). Not present in the body: `currencyCode`
  (server-owned/read-only, expected) and `weeklySchedule` — an **unchanged** week is omitted from the
  write body, so the fractional start/peer-intact assertion could not be exercised in this run (the week
  was never edited).
- **Outcome-unknown path (observed, genuine):** the prepared response was accepted as JSON but was
  **not** decodable against the injected `regionGroups` (the response's `destinationFacts` no longer
  matched the selected tree), so the editor correctly refused to claim success and showed
  «نتیجهٔ ذخیره مشخص نیست… ابتدا وضعیت سرور را بخوانید» with an explicit «خواندن وضعیت از سرور» action,
  kept the typed work dirty, and **did not** send any second PUT. That is the fail-closed behaviour the
  feature is supposed to have; the missing piece is *my* prepared response, not the product.
  **Save adoption (new revision/ids taken from the server response) is therefore NOT proven.**
- Not executed: group add, geography traversal through real option rows, chip add/remove, nested `0.50`
  row, null-admin vs country-wide city through the picker, inactive-option disable, group move/remove
  isolation, week edit, scripted 409, PUT transport failure + explicit reconcile.

## 4. Visual evidence (review only)

Six synthetic screenshots; five committed here, all FA/RTL:

| File | Viewport | Tab |
| --- | --- | --- |
| `shots/11-fa-1440-general.png` | 1440×900 | General (edited) |
| `shots/12-fa-1440-regions.png` | 1440×900 | Regions |
| `shots/13-fa-1440-after-apply.png` | 1440×900 | Regions after Apply (outcome-unknown state) |
| `shots/14-fa-390-regions.png` | 390×844 | Regions |
| `shots/15-fa-390-general.png` | 390×844 | General |

Observations actually made: the two-part General/Regions structure renders; the owner line, type select,
payment/calculation radios, language tabs with default markers and the group list with retained
`گروه ۱`/`گروه ۲`, geography selectors, «افزودن این مقصد», rate rows and one root week are all present
and RTL on desktop; at 390×844 the layout reflows to one column with no horizontal overflow observed in
the captured frame and the tab strip still reachable. **EN/LTR was not captured** — the interface locale is
cookie-driven (`tooba_locale`) and the page stayed `dir="rtl"`, `lang="fa"`; no EN screenshot is claimed.
No design-system/CSS change was made. Screenshots are review evidence, **not** user Visual ACCEPT.

## 5. Runtime, limits, cleanup

Task-owned frontend-only `next dev` on `127.0.0.1:3517` from the previously created copy
`D:\temp\tb205-runtime\frontend` (node_modules junction, `.env.local` =
`TOOBA_HOST_ORIGIN=http://127.0.0.1:1`); installed Chrome `--headless=new` with task profile and CDP
`:9555`. No install/download, no shared `.next`/tsconfig/next-env mutation, the pre-existing `:3000`
dev server untouched.

Corrections used: 2 harness/fixture iterations (the allowed maximum) — the multi-instance diagnostic
churn was my own process hygiene, not a product or fixture issue, and is disclosed rather than hidden.
No production file changed; no backend/Contracts/Host/schema/flag/package/config/baseline/skill/AGENTS
change; no typecheck-as-visual claim.

Cleanup: all harness `node` processes, headless Chrome under the task profile and the task-owned
`next dev :3517` tree stopped; ports 3517 and 9555 verified free afterwards.

Shared-Database-Data-Flag-Changes: NONE
Production-Functional-Acceptance: PENDING
Visual: REVIEW_EVIDENCE_ONLY
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
