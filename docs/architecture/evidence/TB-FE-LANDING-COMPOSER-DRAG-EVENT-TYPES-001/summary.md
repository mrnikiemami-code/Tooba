# TB-FE-LANDING-COMPOSER-DRAG-EVENT-TYPES-001 — drag-event element-type repair (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Expected-Starting-HEAD:** `20c013e5e69cca80bcb240679b241b8c68b966cf` — verified `HEAD == origin/main == Expected-Starting-HEAD`, no tracked modification, pre-existing untracked artifacts left untouched.

## 1. The two recorded diagnostics

```text
app/admin/landing-pages/admin-landing-page-composer.tsx(1024,64): error TS2345: Argument of type 'DragEvent<HTMLDivElement>' is not assignable to parameter of type 'DragEvent<HTMLLIElement>'.
app/admin/landing-pages/admin-landing-page-composer.tsx(1025,61): error TS2345: Argument of type 'DragEvent<HTMLDivElement>' is not assignable to parameter of type 'DragEvent<HTMLLIElement>'.
```

## 2. Which element actually owns each handler

Read from the JSX in the same file:

```text
1015|                  <li key={row.pageSectionId}>
1016|                    <div
...
1024|                      onDragOver={(event) => onSectionDragOver(event, row.pageSectionId)}
1025|                      onDrop={(event) => void onSectionDrop(event, row.pageSectionId)}
1026|                      onDragLeave={() => { ... }}
1027|                      onClick={() => setFocusedSectionId(row.pageSectionId)}
...
1036|                          <button
...
1041|                            onDragStart={(event) => onSectionDragStart(event, row.pageSectionId)}
```

- `onDragOver` / `onDrop` are bound to the inner **`<div>`** → the event's `currentTarget` is `HTMLDivElement`. The declared parameter was `DragEvent<HTMLLIElement>`, i.e. the surrounding `<li>` — an element that receives neither handler. The compiler was right; the annotation was wrong.
- `onDragStart` is bound to the **`<button>`** (`data-testid="section-drag-handle"`) → `DragEvent<HTMLButtonElement>` is correct and was left byte-identical, as the task required.

Both receivers are used only for `preventDefault()`, `event.dataTransfer.dropEffect`, `dataTransfer.getData("text/plain")` and the local `dragId` / `dragOverId` state, so `HTMLDivElement` is a complete, accurate fit and no cast, `any`, or suppression is needed.

## 3. Exact change — one file, two annotations, no runtime edit

```diff
+  // گیرندهٔ واقعی dragover/drop کارت بخش، <div> داخلی لیست است (نه خود <li>)؛
+  // هر دو هندلر روی همان div بایند شده‌اند، پس نوع رخداد باید HTMLDivElement باشد.
-  const onSectionDragOver = (event: DragEvent<HTMLLIElement>, sectionId: string) => {
+  const onSectionDragOver = (event: DragEvent<HTMLDivElement>, sectionId: string) => {
-  const onSectionDrop = async (event: DragEvent<HTMLLIElement>, targetId: string) => {
+  const onSectionDrop = async (event: DragEvent<HTMLDivElement>, targetId: string) => {
```

Unchanged, verified by inspecting the diff: handler bodies, `preventDefault`, `dataTransfer` reads/writes, `dragId` / `dragOverId` state, `findIndex`/`splice`/`persistOrder` reorder algorithm, `data-canonical-order` persistence, markup, `data-testid`s, aria/`title`/`aria-label`, class names, styles, event propagation and `stopPropagation`. No handler was moved and no markup was edited.

## 4. TypeScript — measured against the committed baseline

Baseline copied verbatim from `docs/architecture/evidence/TB-FE-LEGACY-GRID-FIELD-CONTRACT-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **28** |
| After | **26** |
| Delta | **−2** (exactly the Architect's expectation) |
| Introduced diagnostics | **0** (`Compare-Object` of both outputs: no added line) |
| Diagnostics left in the target file | **none** |

Overall `tsc --noEmit` still exits non-zero with **26 errors**; no greenness is claimed.

## 5. Guards and focused tests — and why two of them fail

The landing-composer guards are static source guards (they read the `.tsx` files and regex them). Because the target was a pure annotation change, they are the right regression net, and the honest way to read them is a **pristine-vs-current comparison**: the same guard files were run in a throwaway `git worktree` at the parent HEAD `20c013e5e` (i.e. *without* this edit) and again in the working tree (*with* this edit).

`guard-comparison.txt` (per-file, run-by-run):

| Guard file | Pristine (no edit) | Current (with edit) | Verdict |
| --- | --- | --- | --- |
| `admin-builder-hardening-r13.guard.test.ts` | pass 6 / fail 1 | pass 6 / fail 1 | **IDENTICAL** |
| `admin-builder-ux-r1.guard.test.ts` | pass 7 / fail 1 | pass 7 / fail 1 | **IDENTICAL** |
| `admin-builder-ux-r2.guard.test.ts` | pass 8 / fail 1 | pass 8 / fail 1 | **IDENTICAL** |
| `admin-builder-ux-r3.guard.test.ts` | pass 8 / fail 0 | pass 8 / fail 0 | **IDENTICAL** |
| `admin-store-pages-r9.guard.test.ts` | pass 4 / fail 0 | pass 4 / fail 0 | **IDENTICAL** |
| `admin-landing-pages.guard.test.ts` | pass 4 / fail 2 | pass 4 / fail 2 | **IDENTICAL** |
| `admin-store-pages-r10.guard.test.ts` | fail 1 (load error) | fail 1 (load error) | **IDENTICAL** |

Raw outputs: `focused-test-landing-guard.txt`, `focused-test-r10-guard.txt`, `guards-pristine-baseline.txt`, `guards-current-after-edit.txt`.

The three failures are **pre-existing and untouched by this task**, each with proof rather than assertion:

1. `admin-landing-pages.guard.test.ts` → *"section chooser has Persian labels and no raw JSON editor"* expects `/بنر اصلی/` in **`landing-section-catalog.ts`** (the chooser labels *"بنر نمایش"*). That file is not in this diff, and the assertion fails identically on the pristine parent tree.
2. `admin-landing-pages.guard.test.ts` → *"TB-P10-T022 no technical leakage"* asserts the composer does **not** match `/breakpoint|JSON\.stringify|customCss/i`; the composer matches at **line 430** (`JSON.stringify({ insertAt: options.insertAtAfter ?? null, mode: "create" })`, the `sessionStorage` handoff), which is 42 lines away from the edited handlers and unchanged by this diff. Fails identically on the pristine parent tree.
3. `admin-store-pages-r10.guard.test.ts` cannot even load: it `readFileSync`s `src/backend/Host/Tooba.Host/Admin/StoreLandingPageComposer.cs`, which **does not exist at the parent HEAD** (the backend file was relocated by earlier work). Fails identically on the pristine parent tree. This guard also contains the drag/drop reorder assertion (`section-drag-handle`, `onSectionDrop|onDragStart`, `data-canonical-order`, `reorderAdminLandingSections`), so its drag evidence is unavailable at baseline for an unrelated reason — recorded, not chased, and not "fixed" by this task.

`guards-pristine-baseline.txt` / `guards-current-after-edit.txt` additionally contain a directory-argument invocation of `--test app/admin/landing-pages/` which reports one aggregate failure in both trees; that was an artifact of passing the folder rather than files and is superseded by the per-file comparison above. It is kept for completeness and is not evidence of a regression (identical in both trees).

**Not run (deliberately):** the full `npm test` / `test:discovered` suites, dependency install, build, backend tests, browser/visual verification, and AMSC. No unrun test is reported as pass.

## 6. Scope, risk, rollback

- **Scope:** the only production change is `src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx` (two type annotations + a comment). No other production file, no backend, no schema, no storefront/Home/PDP/Shopeiva, no shared `DragEvent` type, no third-party typings, no tsconfig/guard/baseline change, no `any` / `@ts-ignore` / cast / suppression.
- **Risk:** none to runtime. `DragEvent<T>`'s parameter is compile-time only; the emitted JavaScript for these two arrow functions is unchanged, and `preventDefault`/`dataTransfer` behaviour is identical. Both guards and the typecheck corroborate this.
- **Rollback:** revert the single commit; the two annotations return to `DragEvent<HTMLLIElement>` and TS2345 returns.

## 7. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT. The three pre-existing guard failures above are outside this task's allowed scope and are reported, not repaired.
