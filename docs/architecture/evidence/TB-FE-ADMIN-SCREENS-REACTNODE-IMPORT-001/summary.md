# TB-FE-ADMIN-SCREENS-REACTNODE-IMPORT-001 — missing ReactNode import (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Expected-Starting-HEAD:** `1a18bab5ca5f98258c8f34ca734186322bea91ca` — verified `HEAD == origin/main == Expected-Starting-HEAD`, no tracked modification, pre-existing untracked artifacts left untouched.

## 1. The single recorded diagnostic

```text
app/admin/admin-screens.tsx(234,33): error TS2304: Cannot find name 'ReactNode'.
```

## 2. Cause and canonical type owner

The file's only React import was the hooks line (line 4), and `ReactNode` was never imported anywhere in the file — a grep for `ReactNode` in the target returned exactly one hit, the usage itself:

```text
234:function truncatedCell(content: ReactNode, title?: string) {
```

Canonical owner confirmed as the `react` package (its own type export; `react` is the single legal owner of `ReactNode`, not a local or design-system re-export). The repo-wide idiom is to carry it inline in the existing react import, e.g. `storefront-shipping.tsx:4`, `storefront-cart.tsx:4`, `admin-shell.tsx:5`, `design-system/primitives/overlays.tsx:3`, and the `data-grid` contract itself writes `cell?: (row: T) => import("react").ReactNode` (`design-system/data-grid/types.ts:91`). The one-line existing import already carried value imports, so appending `type ReactNode` is the smallest change that matches the dominant pattern and needs no new import statement, no path change and no barrel edit.

## 3. Exact change — one line, one file

```diff
-import { useCallback, useEffect, useMemo, useState } from "react";
+import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
```

`truncatedCell`'s body, its `content: ReactNode` / `title?: string` signature, every `truncatedCell(...)` call site, the markup it renders, all `data-testid`s, column definitions and grid wiring are untouched — the diff above is the complete production change.

## 4. TypeScript — measured against the committed baseline

Baseline copied verbatim from `docs/architecture/evidence/TB-FE-LANDING-COMPOSER-DRAG-EVENT-TYPES-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **26** |
| After | **25** |
| Delta | **−1** (exactly the Architect's expectation) |
| Introduced diagnostics | **0** — the `Compare-Object` output contains only the removed line (`<= app/admin/admin-screens.tsx(234,33)`) with no added line |
| Diagnostics left in the target file | **none** |

Overall `tsc --noEmit` still exits non-zero with **25 errors**; no greenness is claimed. Raw output: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

## 5. Focused test actually run

`app/admin/admin-grid-migration.test.ts` — **7/7 pass, 0 fail** (`focused-test-admin-grid-migration.txt`).

This test was chosen because it does genuinely exercise the edited file rather than merely mention it: it reads `app/admin/admin-screens.tsx` into a constant (`const adminScreens = fs.readFileSync(path.join(root, "app/admin/admin-screens.tsx"), "utf8")`) and asserts the screen's grid wiring (`AppDataGrid`, `ServerGridPage`, the direct-grid migration and its adapter usage). A grep across the admin/dashboard/feature test files showed it is the only suite that loads this file's source, so it is the one directly relevant existing test and no other suite was run.

Nothing was added to any test: the task explicitly forbade ceremonial tests, and a type-only import needs no new assertion. A `tsc --noEmit` run is the primary proof for an erased type import, and the guard adds behavioural confirmation.

**Not run (deliberately):** the full `npm test` / `test:discovered` suites, dependency install, build, backend tests, browser/visual verification, AMSC, and no other guard or characterization suite. No unrun test is reported as pass.

## 6. Scope, risk, rollback

- **Scope:** the only production change is `src/frontend/app/admin/admin-screens.tsx` (one import line). No other production file, no `any` / cast / `@ts-ignore` / suppression, no tsconfig or exclusion change, no test baseline edit, no guard weakening, no dependency addition, no refactor, no backend/Host/schema change, no Home/PDP/Shopeiva surface, and no unrelated diagnostic was touched.
- **Risk:** none. `import type` is fully erased at emit, so the compiled output, bundle and runtime behaviour are byte-identical; the only observable effect is that the compiler can now resolve the annotation that was already written in the source.
- **Rollback:** revert the single commit; TS2304 at line 234 returns.

## 7. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
