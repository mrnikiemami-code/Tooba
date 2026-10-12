# TB-FE-WALLET-GRID-QUERY-TYPE-001 — gift-card query adapter parameter type (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Expected-Starting-HEAD:** `ed0c0ba3b4243d58542b20b6bf95d044e2b0251b` — verified `HEAD == origin/main == Expected-Starting-HEAD`, no tracked modification, pre-existing untracked artifacts left untouched.

## 1. The single recorded diagnostic

```text
app/wallet/wallet-ui.tsx(577,12): error TS7006: Parameter 'query' implicitly has an 'any' type.
```

## 2. Ownership diagnosis — the canonical type already existed

The callback is the wallet screen's adapter for the gift-card grid:

```text
576|  const giftCardQueryAdapter = useCallback(
577|    async (query) => createClientGridQueryAdapter(giftCardGridRows, giftCardColumns)(query),
578|    [giftCardGridRows, giftCardColumns],
579|  );
```

The untyped parameter was not a missing contract — the contract exists on both ends and the file simply omitted the annotation:

- `createClientGridQueryAdapter` is declared as `(rows, columns) => async (query: GridServerQuery) => executeGridQuery(rows, columns, query)` (`design-system/app-data-grid/legacy-grid-adapters.ts:10-14`), so the inner call already demands `GridServerQuery`;
- `useLegacyAdminGridDirectProps` consumes it as `queryAdapter: (query: GridServerQuery) => Promise<{ rows: T[]; total: number }>` (`legacy-grid-adapters.ts:18`), and that is exactly where `giftCardQueryAdapter` is passed (`wallet-ui.tsx:583`).

So the correct type is the design-system's own export, not a new or duplicated interface. The file **already** imported its sibling type from that same barrel (`import type { GridColumnDef } from "../../design-system/data-grid"`, line 6) and `design-system/data-grid/index.ts` re-exports `GridServerQuery` on line 13, so this is a pure widening of an existing type-only import. The same annotation is already the accepted pattern in this codebase (`app/admin/catalog-attribute-ui.tsx`, repaired in `TB-FE-CATALOG-ATTRIBUTE-GRID-QUERY-TYPES-001`).

## 3. Exact change — one file, two lines

```diff
-import type { GridColumnDef } from "../../design-system/data-grid";
+import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";

   const giftCardQueryAdapter = useCallback(
-    async (query) => createClientGridQueryAdapter(giftCardGridRows, giftCardColumns)(query),
+    async (query: GridServerQuery) => createClientGridQueryAdapter(giftCardGridRows, giftCardColumns)(query),
     [giftCardGridRows, giftCardColumns],
   );
```

No new interface, no duplicate grid type, no cast, no `any`, no `@ts-ignore`. Untouched and verified by the diff: the `useCallback` dependency array, both `giftCardGridRows`/`giftCardColumns` definitions, `giftCardGridProps`, `useLegacyAdminGridDirectProps` wiring, the saved-view store, grid query/sort/filter/page/export behaviour, callbacks, wallet and gift-card logic, markup, class names and visual design.

## 4. TypeScript — measured against the committed baseline

Baseline copied verbatim from `docs/architecture/evidence/TB-FE-ADMIN-SCREENS-REACTNODE-IMPORT-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **25** |
| After | **24** |
| Delta | **−1** (exactly the targeted TS7006) |
| Introduced diagnostics | **0** — the `Compare-Object` output contains only the removed line (`<= app/wallet/wallet-ui.tsx(577,12)`) with no added line |
| Diagnostics left in `wallet-ui.tsx` | **none** |

Overall `tsc --noEmit` still exits non-zero with **24 errors**; no greenness is claimed. Raw output: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

## 5. Focused tests actually run

| Command | Result |
| --- | --- |
| `app/admin/admin-grid-migration.test.ts` | **7/7 pass, 0 fail** |
| `app/wallet/wallet-api.test.ts` (adjacent, unchanged file) | **5/5 pass, 0 fail** |

`admin-grid-migration.test.ts` is the **nearest pre-existing suite that genuinely reads the edited file**: it loads `app/wallet/wallet-ui.tsx` into a constant (`const walletUi = fs.readFileSync(path.join(root, "app/wallet/wallet-ui.tsx"), "utf8")`, line 10) and asserts the gift-card grid wiring it must preserve (`/admin-gift-cards-grid/`, `/AppDataGrid/`, and *not* `/LegacyAppDataGrid/`, lines 42-44). `wallet-api.test.ts` is the only wallet-focused suite in the tree; it covers the wallet API layer next to the edited screen and is included because it is directly adjacent and confirms no wallet behaviour moved.

**Pre-existing failures observed: none** in either run. Nothing was added to any test — the task forbade ceremonial tests, and a type-only annotation needs no new assertion; `tsc --noEmit` remains the primary proof.

**Not run (deliberately):** the full `npm test` / `test:discovered` suites, dependency install, build, backend tests, browser/visual verification, AMSC, and every other guard or characterization suite. No unrun test is reported as pass.

## 6. Scope, risk, rollback

- **Scope:** the only production change is `src/frontend/app/wallet/wallet-ui.tsx` (one import widened + one parameter annotated). No other production file, no shared grid or API contract rewrite, no tsconfig/exclusion/baseline/guard change, no backend/schema change, no Home/PDP/Shopeiva surface, no new dependency, and no unrelated cleanup.
- **Risk:** none. A parameter annotation and an `import type` are both erased at emit, so the compiled output and runtime behaviour are byte-identical; the query, callback identity and dependency array are unchanged.
- **Rollback:** revert the single commit; TS7006 at line 577 returns.

## 7. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
