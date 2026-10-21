# TB-SHIPPING-ADMIN-UI-PREVIEW-186 — Result evidence

Task: `TB-SHIPPING-ADMIN-UI-PREVIEW-186` (Parent: `TB-STOREFRONT-PERF-BROWSER-ACCEPTANCE-185`)
Program: `TOOBA-SHIPPING-ADMIN-UI-ACCEPTANCE`
Mode: `BOUNDED_MULTILINGUAL_ADMIN_VISUAL_PREVIEW_ONLY`
Date: 2026-10-21 (Asia/Tehran)

## 1. SHAs

| Field | Value |
| --- | --- |
| Expected-Base-SHA | `a889b0190322f13ee9b983a614011f24e132dd90` |
| Base-SHA (verified before edits) | `a889b0190322f13ee9b983a614011f24e132dd90` |
| Code-SHA | `1bd2ad26a2215f50d7507acd667bc703bc812e53` |
| Evidence-SHA | `1bd2ad26a2215f50d7507acd667bc703bc812e53` (code + evidence in one scoped commit) |

`git fetch origin` was run first; local `main` was 3 commits behind (`5a66b8b44`) and was fast-forwarded
normally (no reset/rebase/stash) to the expected base before any edit.

## 2. Architecture ownership

| Concern | Owner |
| --- | --- |
| Route composition | `app/admin/shipping-services/design-preview/page.tsx` (thin server component, no data access) |
| Feature state / flow | `design-preview-screen.tsx` (client; in-memory draft only) |
| Typed preview view-models + pure rules | `preview-model.ts` |
| Explicit SAMPLE fixtures | `preview-fixtures.ts` |
| Preview-scoped dictionary + locale-aware formatting | `preview-i18n.ts` |
| Feature presentation | `preview-method-list.tsx`, `preview-general-tab.tsx`, `preview-regions-tab.tsx`, `preview-region-picker.tsx`, `preview-rate-row-editor.tsx` |
| Focused test | `preview-model.test.ts` |

Reused repository facilities (no second design system):

- `design-system` `AppDataGrid` via `useLegacyAdminGridDirectProps` + `createClientGridQueryAdapter`
  and `AppGridRowActionsCell` (same pattern as the existing `flat-screen.tsx`).
- `design-system` primitives `Field`, `Input`, `Select`, `Textarea`.
- Admin chrome: `app/admin/layout.tsx` + `AdminShell` (unchanged; no auth bypass, no shell rewrite).
- i18n core: `lib/i18n/locale.ts` semantics (fa = RTL, en = LTR) reproduced through
  `previewDirection`; locale-aware formatting via `Intl` with the same `fa-IR` / `en-US` tags the
  storefront uses. The preview dictionary is task-scoped only; no shared/global dictionary was edited.

No production DTO, API client, backend module, schema, migration, feature flag or config was touched.

## 3. Exact URL and start command

- URL: `http://127.0.0.1:3419/admin/shipping-services/design-preview`
  (the Admin route is not locale-prefixed; the interface language is toggled inside the page and starts at fa/RTL.)
- Repo-native start command used for this review listener:

```powershell
cd src/frontend
npx next dev -p 3419
```

- Task-owned listener: port `3419` (loopback), started by this task, log at
  `.tmp-186-preview/next-3419.log` (git-ignored). Shared ports `:3000`, `:5088`, `:5432` were not used
  or restarted; the Host was not running and was not started.
- `.next` is the repository's existing git-ignored Next dev cache; it was not cleaned, deleted or
  shared with another running process (no other frontend process existed).
- The listener was left running only for the user's requested visual review.

## 4. Business-network-write check

- Static: `preview-model.test.ts` scans every non-test preview source with comments stripped and
  asserts absence of `fetch(`, `XMLHttpRequest`, `localStorage`, `sessionStorage`, `document.cookie`,
  `method: POST|PUT|PATCH|DELETE`, and the Host client (`hostBaseUrl`, `buildUpstreamAuthHeaders`).
  Result: PASS (no matches).
- Runtime (browser): resource-timing diff across all preview interactions (open editor, tab switch,
  add region, add 2 rows, apply, back) showed **zero** new requests. The only `/v1/*` entries were the
  pre-existing Admin shell reads at load (`/v1/admin/dev-context`,
  `/v1/admin/access-control/me/capabilities`), unrelated to the preview and failing because the Host
  was intentionally not started.
- `Business-Network-Writes: ZERO`.

## 5. Real-browser proof (DOM/layout, not screenshots)

Browser: Cursor IDE browser (Chromium), route rendered by the running Next dev server.

| Width | Document scrollWidth | Page-wide horizontal overflow | Rates table |
| --- | --- | --- | --- |
| 1440 | 1425 | none | bounded `overflow-x-auto` |
| 1024 | 1009 | none (after `min-w-0` fix on region-group cards) | bounded scroll, wrapper 613px |
| 390 | 390 | none | bounded scroll, wrapper 298px |

- Parallel script blank screenshots were produced by the headless compositor (CDP
  `Page.captureScreenshot` returned empty `data` while the tab was not painted), so verification used
  DOM/text/computed-layout reads from the real rendered page instead of image galleries. Text, control
  values, `aria-*` state, disabled state and bounding boxes were read from the live DOM.
- A first 1024px measurement exposed a real defect: the region-group card (grid item,
  `min-width: auto`) expanded to 746px inside a 641px container. Fixed by `min-w-0` on the card and
  switching the regions root to `flex min-w-0 flex-col`; re-measured: no overflow.
- Language/direction: `dir="rtl" lang="fa"` in Persian; after the interface switch `dir="ltr"
  lang="en"`, all interface strings English, grid headers mirrored (`Actions`, `Method name`, ...).

## 6. Interaction checks (all executed in the real browser)

| Check | Observed result |
| --- | --- |
| List → Edit → both tabs → back | Editor opens, both primary tabs present (`اطلاعات روش`, `مناطق و هزینه‌ها`), back returns to the list and the applied draft is visible |
| Two primary tabs only | `[role=tab]` returns exactly the two primary tabs; the content-language switcher is a `role=group` with `aria-pressed` (no nested tablist) |
| One geography, many rate rows | One group `تهران و قم` with 3 fixture rows; adding 2 rows via the row editor produced 5 rows under the same region selection |
| Row editor has no geography | `select` count inside the row editor = 0 |
| Duplicate selection | blocked (`duplicate`) |
| Adding a child first, then its ancestor | گلپایگان added → `افزودن` disabled for اصفهان with «والدِ «گلپایگان» است که قبلاً اضافه شده است.» |
| Disjoint sibling allowed | with گلپایگان present, city قم added successfully |
| Adding a parent, then a descendant | تهران (province) added → city تهران disabled with «زیرمجموعهٔ «تهران» است که قبلاً اضافه شده؛ ابتدا آن را حذف کنید.» |
| Selected areas render readable paths | `ایران › اصفهان › گلپایگان`, `ایران › قم › قم`, `ایران › تهران` (no GUID) |
| Content language independence | editing the English name left the Persian value untouched and survived tab round-trip; translation summary stayed truthful (`۲ از ۲`) |
| Interface vs content language independence | switching the interface to EN kept the open draft and the Persian content values |
| View mode read-only | View: all 8 general-tab inputs disabled, read-only notice shown, Add-row / remove / Apply / Reset hidden |
| Apply feedback | `فقط در پیش‌نمایش همین صفحه اعمال شد؛ چیزی به سرور ارسال نشد.` |
| Dirty state | `دادهٔ نمونه · تغییر اعمال‌نشده دارید.`; leaving with unsaved changes asks for confirmation |
| Owner switch | marketplace (4 methods) vs seller sample (1 method, own region group) — conflict domain stays per owner/method |
| Locale-aware output | fa: `۸۰۰٬۰۰۰ ریال`, `۱ کیلوگرم تا کمتر از ۲ کیلوگرم`, `۲ از ۲ زبان`; en: `IRR 800,000`, `1 kg to under 2 kg`, `1 item to under 4 items` |

## 7. Commands actually run

```text
git fetch origin; git pull --ff-only origin main
node --experimental-strip-types --test app/admin/shipping-services/design-preview/preview-model.test.ts   → 10/10 PASS
npx tsc --noEmit                                                                                        → PASS (0 errors, ~13s / 20s runs, within the 180s bound)
node --experimental-strip-types --test lib/architecture/frontend-flat-folder.guard.test.ts \
  lib/architecture/frontend-import-boundary.guard.test.ts \
  lib/architecture/frontend-feature-boundary.guard.test.ts                                               → PASS
node --experimental-strip-types --test lib/architecture/frontend-source-size.guard.test.ts               → FAIL (pre-existing, see §8)
npx next dev -p 3419                                                                                    → task-owned listener
```

## 8. Pre-existing failures (not caused by this task, not fixed)

`frontend-source-size.guard.test.ts` fails on `main` at the base SHA for four legacy oversized files
that this task never touched:

```text
OVERSIZED_GROWTH src/frontend/app/admin/category-admin-screen.tsx: 2230 > baseline 2215
OVERSIZED_GROWTH src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx: 1442 > baseline 1440
OVERSIZED_GROWTH src/frontend/app/storefront/storefront-landing-blocks.tsx: 1033 > baseline 1026
OVERSIZED_GROWTH src/frontend/lib/storefront-composition/shared-composition-renderer.tsx: 918 > baseline 907
```

All new files are well below the 800-LOC threshold for new files. No baseline was refreshed
(out of scope).

The Host (`127.0.0.1:5088`) was down for this review; the Admin shell's existing dev-context and
capability reads therefore fail. That is the pre-existing shell behaviour, independent of this route,
which performs no data access.

## 9. Known cosmetic debt (for a later, separate task)

- English content showing Persian method names in the list is correct given the content language
  stays Persian, but the list itself has no content-language control; the reviewer can switch content
  language only inside the editor.
- The preview paginates/renders through `AppDataGrid` without a saved-view store, so saved views are
  intentionally unavailable here (no persistent browser storage by task rule).
- `deliveryValue` uses `min–max days` (does not model a single-day phrasing).

## 10. Isolation and cleanup

- Only task-scoped files added; the existing `/admin/shipping-services` page, its API clients, dialogs
  and write gates are untouched.
- No new Host/DB, no migrations, no schema, no carrier calls, no secrets or customer data in fixtures.
- No reset / stash / rebase / force-push; unrelated dirty/untracked artifacts (build output under
  `src/frontend/.next`, `obj/bin` caches) were left exactly as found.
- Task-owned listener left running for review on `:3419`; no other process was stopped.

## 11. Remaining user gate

```text
USER_VISUAL_ACCEPTANCE = PENDING
PRODUCTION_FUNCTIONAL_IMPLEMENTATION = NOT_STARTED
```

Open `http://127.0.0.1:3419/admin/shipping-services/design-preview` and:

1. فهرست: click **ویرایش** on «ارسال سریع درون‌شهری».
2. Tab **مناطق و هزینه‌ها** → **ویرایش مناطق** → add a country/province/city; observe disabled
   conflicting options and the reason text.
3. **افزودن ردیف** twice under the same region selection.
4. Switch the interface language to **EN** (top-right) and compare mirroring.
5. Switch the content language (فارسی / انگلیسی) inside **اطلاعات روش** and confirm the Persian value
   is not overwritten.
6. Click **اعمال در پیش‌نمایش** — nothing is persisted to the server.
