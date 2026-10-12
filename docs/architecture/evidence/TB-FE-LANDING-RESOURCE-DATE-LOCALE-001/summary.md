# TB-FE-LANDING-RESOURCE-DATE-LOCALE-001 — landing resource selector date locale (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Expected-Starting-HEAD:** `6f8f6ff4aae1df4be14d7c25a268e5502584c550` — verified `HEAD == origin/main == Expected-Starting-HEAD`, tracked worktree clean, no untracked artifact touched.

## 1. The recorded diagnostic

```text
app/admin/landing-pages/admin-resource-selector.tsx(741,43): error TS2554: Expected 2 arguments, but got 1.
```

Canonical formatter (`design-system/app-data-grid/jalali.ts:7`) requires two arguments, and unlike its sibling it has **no default**:

```ts
export function formatJalaliDate(iso: string | null | undefined, locale: "fa" | "en"): string { … }
export function formatJalaliDateTime(iso: string | null | undefined, locale: "fa" | "en" = "fa"): string { … }
```

## 2. Locale determination — from evidence, not guesswork

**Answer: `"fa"`.** Five independent lines of evidence, all pointing the same way:

1. **Application-wide convention is unanimous.** Every one of the ~15 concrete call sites of `formatJalaliDate` in `src/frontend` passes `"fa"`: `admin-landing-pages-screen.tsx:395`, `admin-campaigns-screen.tsx:232,243,276`, `content-author-list.tsx:157,298`, `content-list.tsx:258,566`, `content-category-admin-screen.tsx:539,543`, `content-author-admin-screen.tsx:387,391`, `product-list.tsx:177,311`, `category-products-panel.tsx:473`, `storefront-shipping.tsx:581`, `storefront-shipping-format.ts:30`. A search for `formatJalaliDate(…"en"` returns **zero** call sites in the whole frontend — the only match is the declaration itself. There is no precedent anywhere for an English rendered Jalali date.
2. **The design-system default for this exact grid is `"fa"`.** `AppDataGrid.tsx:108` (`locale = "fa"`), `legacy-grid-adapters.ts:35` (`locale = "fa"`), `app-column-header.tsx:36` (`ctx.locale ?? "fa"`), `jalali-date-picker.tsx:34` (`locale = "fa"`), `status-header-filter-panel.tsx:10` (`locale = "fa"`), `formatJalaliDateTime(… = "fa")`. `admin-resource-selector.tsx` **never passes a locale**, so its grid runs on the `"fa"` default.
3. **The sibling column in the same feature uses `"fa"`.** `admin-landing-pages-screen.tsx:395`: `cell: (row) => formatJalaliDate(row.updatedAt, "fa")` — same landing-pages admin feature, same kind of date column.
4. **This selector is Persian-facing and has no locale parameter.** Its headers are Persian (`"تاریخ انتشار"`, `نویسنده`, `وضعیت`), and a search of the file for `locale` / `dir=` / `lang` / `i18n` returns **no matches** — there is no per-instance locale to honour, so no product decision is required (therefore `BLOCKED_ARCHITECT_DECISION` is **not** raised). Nor does any Admin locale switch/provider exist.
5. **The omitting call was internally inconsistent.** The column carries `filterKind: "date"`, so its own filter panel already renders Jalali via `ag-filter-mapper.ts:366-368` (`formatJalaliDate(value.iso, locale)` with the grid's `"fa"`). The cell alone rendered Gregorian — proof that the missing argument was an oversight (which is exactly what TS2554 reports), not an intentional English rendering.

**Explicit behaviour disclosure (not silent):** the argument was omitted, so at runtime `locale === "fa"` was false and the cell fell through to `d.format("YYYY-MM-DD")` — i.e. it was rendering a **Gregorian** date under a Persian "تاریخ انتشار" header while its own filter panel showed Jalali. Supplying `"fa"` therefore **intentionally changes the rendered cell text from `YYYY-MM-DD` to Jalali `YYYY/MM/DD`**, aligning it with the canonical formatter contract, the sibling column, the grid default and the date filter of the same column. This is the repair, recorded deliberately rather than presented as a no-op.

## 3. Exact change — one file, one argument

```diff
@@ -738,7 +738,7 @@ function ArticleResourceGrid({
         id: "publishDate",
         header: "تاریخ انتشار",
         accessor: (row) => row.publishDate,
-        cell: (row) => (row.publishDate ? formatJalaliDate(row.publishDate) : "—"),
+        cell: (row) => (row.publishDate ? formatJalaliDate(row.publishDate, "fa") : "—"),
         width: 140,
         minWidth: 120,
         filterKind: "date",
```

Preserved exactly, verified by the diff and by `git diff --name-only` returning only this file:

- the `row.publishDate ? … : "—"` **null fallback** (kept as-is, belt-and-braces), the `accessor` (so **date data and sorting/filtering keys are unchanged**), `id`, `header`, `width: 140`, `minWidth: 120`, `filterKind: "date"`, `sortable: true`;
- every other column, the modal/resource-selection markup, `ResourceSelectorTrigger`, and the Shopeiva/Home/PDP contract (no storefront/composition file touched).

No formatter signature/default change; no new default invented on `formatJalaliDate`; no i18n refactor; no `any`, cast, `!`, or suppression; no tsconfig/CI/baseline/guard edit; no backend/schema change.

## 4. Diagnostics — 20 → 19, normalized comparison

Baseline copied verbatim from the previous task's evidence, exactly as the task specified: `docs/architecture/evidence/TB-FE-RESERVATION-POLICY-NUMERIC-FIELD-KEY-001/typecheck-after-tsc.txt`.

| | Errors |
| --- | --- |
| Before (baseline) | **20** |
| After | **19** |
| Delta | **−1** |
| Introduced diagnostics | **0** |

Compared **normalized by file/line/code** (as the task required, to remove TS union-print-order noise). The normalized diff contains exactly one removed entry:

```text
<= app/admin/landing-pages/admin-resource-selector.tsx(741,43) TS2554
```

and **no added entries**. Per-file tallies (before → after) confirm only the target file cleared:

```text
app/admin/landing-pages/admin-resource-selector.tsx   1 → 0    ← target TS2554 gone
app/admin/category-admin-screen.tsx                   1 → 1
app/admin/content-list.tsx                            1 → 1
app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts 1 → 1
app/admin/product-rich-text-editor.tsx                1 → 1
app/storefront/storefront-landing-blocks.tsx          1 → 1
app/template-preview/fashion/fashion-template-preview-view.tsx 1 → 1
features/admin-languages/components/language-list.tsx 3 → 3
lib/storefront-composition/fashion-demo-preview.ts    1 → 1
lib/storefront-composition/shared-composition-renderer.tsx 9 → 9
```

`tsc --noEmit` still exits non-zero with **19 errors**; no greenness is claimed. Because the normalized comparison is clean, the previous task's union-print-order artefact did not recur as a false difference. Raw: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

## 5. Focused tests — 3 failures, ALL proven pre-existing and unrelated

Two runs, both recorded in `focused-tests.txt`.

**Run A — nearest suites, including the one that reads the changed file:**

| Suite | Result |
| --- | --- |
| `admin-builder-ux-r1.guard.test.ts` (the only suite that reads `admin-resource-selector.tsx`, line 13) + `jalali-date-ui.test.ts` + `ag-filter-mapper.test.ts` | **13 tests, 12 pass, 1 fail** |

The single failure is at `admin-builder-ux-r1.guard.test.ts:73` — `assert.match(previews, /data-layout-aware/)`, where `previews` is `layout-aware-previews.tsx` (test line 14). **That file is not in my diff.** Critically, the assertions in this same suite that *do* read the changed file passed: line 41 (`selector` → `/resource-selector-tab-selected/`) and lines 80-82 (`selector` → `/AppGridRowActionsCell/`, `/compact/`, `/label:/`).

**Run B — landing-pages guard suite** (`admin-landing-pages.guard.test.ts`, which **never reads** `admin-resource-selector.tsx` — its 7 `readFileSync` targets at lines 8-14 are `admin-landing-pages-screen.tsx`, `admin-landing-page-composer.tsx`, `admin-landing-section-forms.tsx`, `landing-section-catalog.ts`, `admin-composition-catalog.ts`, `admin-landing-pages-api.ts`, `admin-section-wizard.tsx`):

| Suite | Result |
| --- | --- |
| `admin-landing-pages.guard.test.ts` | **6 tests, 4 pass, 2 fail** |

**Proof the 3 failures are pre-existing and unrelated** — the decisive, non-destructive argument:

1. `git diff --name-only` returns **exactly one** path: `src/frontend/app/admin/landing-pages/admin-resource-selector.tsx`.
2. Each failing assertion reads a file that is **not** that path: line 73 reads `layout-aware-previews.tsx`; line 41 reads `landing-section-catalog.ts`; line 51 reads `admin-landing-page-composer.tsx`.
3. Therefore the pass/fail state of those three assertions is **provably independent** of this change; nothing I edited is read by them, and no file they read was modified. A single added string argument in `admin-resource-selector.tsx` cannot alter source-text regexes evaluated against three other, byte-identical files.

Exact pre-existing failures (disclosed, not chased — no cascade, no fix, no retry):

```text
app/admin/landing-pages/admin-builder-ux-r1.guard.test.ts:73   assert.match(previews, /data-layout-aware/)          — layout-aware-previews.tsx
app/admin/landing-pages/admin-landing-pages.guard.test.ts:41   assert.match(catalog, /بنر اصلی/)                    — landing-section-catalog.ts
app/admin/landing-pages/admin-landing-pages.guard.test.ts:51   assert.doesNotMatch(composer, /breakpoint|JSON\.stringify|customCss/i) — admin-landing-page-composer.tsx
```

Not run (deliberately): full `npm test` / `test:discovered`, build, browser/visual verification, backend suites, AMSC, other guards. No unrun test is reported as pass. No test was added or amended — the task preferred existing suites and forbade ceremonial tests, and the compiler plus the `"fa"` contract evidence are the verification here (a `"fa"`-only locale argument has no new behaviour of its own to assert beyond the formatter's already-tested Jalali output in `jalali-date-ui.test.ts`, which passed).

## 6. Limitations

- The focused evidence is `node:test` guard/source suites plus the compiler; **no browser render was performed**, so the Jalali cell text was not visually confirmed in a live grid. The locale→format mapping itself is covered by the pre-existing, passing `jalali-date-ui.test.ts` (`formatJalaliDate(iso!, "fa")` → `/^1404\/01\/01$/`).
- Three pre-existing failures in two landing-pages guard suites remain red; out of this task's scope and proven unrelated above.
- `tsc` still reports 19 pre-existing errors elsewhere (notably 9 in `shared-composition-renderer.tsx` and 3 in `admin-languages/language-list.tsx`).

## 7. Risk and rollback

- **Risk: low and deliberate.** The compiler fix and the correct locale are the same edit. The only user-visible effect is the intended one disclosed in §2: the `تاریخ انتشار` cell now shows Jalali instead of Gregorian, matching its own date filter, its sibling column and the grid default. No schema, data, sorting, filtering, accessor or storefront change.
- **Rollback:** revert the single commit; TS2554 returns and the cell reverts to Gregorian `YYYY-MM-DD`.

## 8. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
