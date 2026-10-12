# TB-FE-CONTENT-ACTIONS-SLOTS-CONTRACT-001 — content actions column slot contract (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Expected-Starting-HEAD:** `345a87b5db6b8dbaab123543af2628007957e040` — verified `HEAD == origin/main == Expected-Starting-HEAD`, tracked worktree clean, no untracked artifact touched.
**Gate outcome:** the stop gate **PASSED** on source-backed proof of zero runtime impact, so a bounded repair was implemented; `BLOCKED_ARCHITECT_DECISION` was **not** required.

## 1. The recorded diagnostic

```text
app/admin/content-list.tsx(262,7): error TS2322: Type '5' is not assignable to type '3 | 2 | 1 | undefined'.
```

## 2. STOP GATE — exactly what `actionSlots` means at runtime

The whole owner is 50 lines (`design-system/app-data-grid/app-grid-pinned-actions.ts`). `actionSlots` has **one** consumer and **one** purpose:

```ts
  /** تعداد جایگاه عملیات (۱–۳) برای عرض پیشفرض جمعتر؛ کاربر همچنان میتواند resize کند. */
  actionSlots?: 1 | 2 | 3;                       // :13
…
function widthForActionSlots(slots: 1 | 2 | 3) { // :17
  const perSlot = 40;
  const pad = 28;
  const width = pad + slots * perSlot;
  return { width, minWidth: width - 8, maxWidth: width + 48 };
}
…
  actionSlots = 2,                               // :31
…
  const slotWidths = widthForActionSlots(actionSlots);          // :33
  return {
    …
    width:    width    ?? slotWidths.width,                     // :38
    minWidth: minWidth ?? slotWidths.minWidth,                  // :39
    maxWidth: maxWidth ?? slotWidths.maxWidth,                  // :40
```

**Answer: a compact-default width hint — not a count, not a reservation, not a cap, not an overflow trigger, not a rendering switch.** Its derived numbers are used **only as `??` fallbacks** for the column's three dimensions. Every other option (`sortable`, `filter`, `resizable`, `lockVisible`, `lockPinned`, `lockPosition`, `pinned`, `cellClass`, `cellRenderer`, `colId`) is independent of it.

**Proof it is inert for this specific caller.** `content-list.tsx:260-268` supplies **all three** dimensions explicitly — `width: 200`, `minWidth: 180`, `maxWidth: 200` — so every `??` short-circuits to the explicit value and `slotWidths` is computed and then **entirely discarded**. For `5` slots the derived numbers would have been `{width: 228, minWidth: 220, maxWidth: 276}`, none of which was ever used. `actionSlots: 5` therefore had **zero runtime effect** before this change.

Repo-wide confirmation that nothing else consumes it:

```text
actionSlots occurrences (production, whole repo):
  design-system/app-data-grid/app-grid-pinned-actions.ts:13   declaration
  design-system/app-data-grid/app-grid-pinned-actions.ts:31   destructure default
  design-system/app-data-grid/app-grid-pinned-actions.ts:33   the single use -> widthForActionSlots
  app/admin/category-products-panel.tsx:477                   caller (2 | 1)
  app/admin/content-author-list.tsx:161                       caller (3)
  app/admin/content-list.tsx:262                              caller (5)  <-- in-contract violation, the target
  app/admin/product-list.tsx:192                              caller (3)
No test asset in the repository references actionSlots.
```

## 3. STOP GATE — are all action definitions preserved and accessible?

**Yes, provably, and none of the four prohibited repair shapes was used.** The renderer is `AppGridRowActionsCell` (`design-system/app-data-grid/app-grid-row-actions.tsx:32-101`):

```tsx
const visibleActions = actions.filter((action) => action.visible?.(row) ?? true);  // :37
…
{visibleActions.map((action) => { … })}                                            // :61
```

- It renders **every** visible action — `map`, with **no cap, no slice, no index limit, no overflow menu, no "more" button** — and it **never reads `actionSlots`** (that value never reaches this component; it is not a prop of it).
- The six action definitions in `buildContentRowActions` (`content-list.tsx:149-217`) are **byte-identical before and after** — verified by the diff in §4: `view` (Eye → `viewHref`, unfiltered), `edit` (Pencil → `editHref`, `visible: canEdit && !isArticleArchived`), `delete` (Trash2, destructive, `canEdit && canHardDeleteArticle`), `archive` (Archive, destructive, `canEdit && canArchiveArticle`), `publish` (Upload, `canPublish && !isPublished && !isArticleArchived`), `unpublish` (Undo2, destructive, `canPublish && isPublished`). Labels, icons, `variant`, `href`/`onClick` navigation targets and `testId`s are untouched.
- Permissions path untouched: `caps` → `allowedCapability(caps, "content.edit" / "content.publish")` unchanged; `rowActions` memo deps unchanged.

**Why no geometry change.** The emitted `ColDef` is `width: 200, minWidth: 180, maxWidth: 200` before and after — the same literals via the same `??` short-circuit. Only the unused derivation input is gone.

**Why no shared-contract change.** `AppGridPinnedActionsColumnOptions.actionSlots?: 1 | 2 | 3` is **not** widened, not relaxed, and no design-system file is edited (production scope was one file). The reference callers were inspected to confirm the parameter's real purpose: `category-products-panel.tsx:477` passes `actionSlots: canEdit && assignable ? 2 : 1` **without** explicit dimensions — that is the case where the parameter is genuinely load-bearing — whereas `content-author-list.tsx:161` (3) and `product-list.tsx:192` (3) also override it. This confirms `actionSlots` is a *default-width* input, which is precisely why an out-of-union `5` was never a way to request a wider column.

**Conclusion:** changing/removing `actionSlots` here loses **no** visible control, changes **no** geometry, violates **no** shared contract, and requires **no** product/architecture decision. Therefore the gate passed and the smallest contract-correct local repair was applied.

## 4. Exact change — one line removed, intent documented

```diff
@@ -259,7 +259,10 @@ function buildColumnDefs(
     }),
     buildPinnedActionsColumnDef<AdminContentArticle>({
       direction: "rtl",
-      actionSlots: 5,
+      // actionSlots فقط عرض پیشفرض فشرده را میسازد (۱–۳ جایگاه) و اینجا هندسهٔ ستون
+      // کاملاً با width/minWidth/maxWidth صریح تعیین شده است؛ پس اشارهگر جایگاه نه
+      // اعمال میشود و نه معتبر است. تعداد واقعی دکمهها را AppGridRowActionsCell
+      // بدون سقف رندر میکند.
       width: 200,
       minWidth: 180,
       maxWidth: 200,
```

**Repair shape chosen:** remove the inert, out-of-contract hint (its union covers only the compact 1–3 presets, so no honest in-contract value exists for this column) and leave a comment recording *why*, so the intent survives for maintainers. This is **not** "silencing a type error":

| Prohibited by the task | Used? |
| --- | --- |
| mechanically replace `5` with `3` | **No** — no numeric value substituted |
| widen the shared union to `5` | **No** — design system untouched |
| hide or remove actions | **No** — all six definitions and their rendering intact |
| silence the error (suppression/cast/`any`/`!`/ts-ignore) | **No** — the invalid property was eliminated, not suppressed |
| change geometry | **No** — 200/180/200 identical literals |

No test was added or amended: the task permitted a focused-test change "only if it tests a meaningful observed contract", and the observed contract (column dimensions + action rendering) is already covered by the pre-existing suites in §6, which pass. A new assertion would be ceremonial, which the task forbade.

## 5. Diagnostics — 19 → 18, normalized

| | Errors |
| --- | --- |
| Before (baseline) | **19** |
| After | **18** |
| Delta | **−1** |
| Introduced diagnostics | **0** |

Baseline is the previous task's committed evidence, as the repo convention requires: `docs/architecture/evidence/TB-FE-LANDING-RESOURCE-DATE-LOCALE-001/typecheck-after-tsc.txt`. Compared **normalized by file/line/code**; the diff contains exactly one removed entry and no added entries:

```text
<= app/admin/content-list.tsx(262,7) TS2322
```

Per-file tallies (before → after) — only the target file cleared, nothing changed state otherwise:

```text
app/admin/content-list.tsx                             1 → 0   ← target TS2322 gone
app/admin/category-admin-screen.tsx                   1 → 1
app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts 1 → 1
app/admin/product-rich-text-editor.tsx                1 → 1
app/storefront/storefront-landing-blocks.tsx          1 → 1
app/template-preview/fashion/fashion-template-preview-view.tsx 1 → 1
features/admin-languages/components/language-list.tsx 3 → 3
lib/storefront-composition/fashion-demo-preview.ts    1 → 1
lib/storefront-composition/shared-composition-renderer.tsx 9 → 9
```

`tsc --noEmit` still exits non-zero with **18 errors**; no greenness is claimed. Raw: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

## 6. Focused tests — closest content/action guard suites

```text
node --experimental-strip-types --test app/admin/content-list.test.ts \
  design-system/app-data-grid/app-grid-canonical.test.ts \
  design-system/app-data-grid/pinned-column.test.ts
→ tests 14, pass 14, fail 0
```

- `content-list.test.ts` **reads the changed file directly** (`readFileSync(join(import.meta.dirname, "content-list.tsx"), "utf8")`, line 6) and its action-column guard still passes: it asserts `/buildPinnedActionsColumnDef/`, `/direction:\s*"rtl"/`, `/AppGridRowActionsCell/` and `assert.doesNotMatch(source, /AgGridReact/)`. So the pinned-actions contract this task touched is explicitly re-verified against the edited source.
- `app-grid-canonical.test.ts` and `pinned-column.test.ts` are the design-system action-column guards (they read `index.ts`, `AppDataGrid.tsx`, `product-list.tsx`, `theme.css`, `legacy-grid-bridge.ts` and assert `buildPinnedActionsColumnDef` is exported/used) — confirming the shared helper and its union were **not** altered.

**Pre-existing failures observed: none** in these suites, so nothing needed to be recorded or chased; exactly one run, no cascade. Raw: `focused-tests.txt`.

Not run (deliberately): full `npm test` / `test:discovered`, build, browser/visual verification, backend suites, AMSC, other guards. No unrun test is reported as pass.

## 7. Behaviour delta and uncertainty

- **Exact behaviour delta: none.** The removed property had no runtime effect (§2), the six actions are unchanged and still rendered without a cap (§3), and the column dimensions are the same literals (200/180/200).
- **Outstanding uncertainty: low.** The only judgement call is whether the original author intended `actionSlots: 5` as *documentation of five slots*; either reading yields identical rendering because explicit dimensions win, and the retained comment preserves the reasoning for the next maintainer.
- **Limitations:** no browser render was performed, so the actions column was not visually inspected live; the confident statements above rest on the shared helper's `??` semantics, the renderer's unbounded `map`, the byte-identical action definitions and the passing guards.

## 8. Risk and rollback

- **Risk: negligible.** A single inert, out-of-contract option removed from one column definition; geometry, ordering, pinning, resizability, actions, permissions and navigation are all unchanged, and no shared contract or storefront surface was touched.
- **Rollback:** revert the single commit; TS2322 returns in `content-list.tsx` with rendering still identical.

## 9. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
