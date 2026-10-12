# TB-FE-ADMIN-RECEIPTS-ACTIONS-COLUMN-WIDTH-001 — receipts actions column dimensions (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Expected-Starting-HEAD:** `82dcb78aba67632ceeb16420607798b757d2fcbc` — verified `HEAD == origin/main == Expected-Starting-HEAD`, tracked worktree clean, pre-existing untracked artifacts left untouched.

## 1. The recorded diagnostic

```text
features/admin-receipts/components/receipts-screen.tsx(222,3): error TS2739: Type '{ id; header; accessor; cell; exportable: false; sortable: false; }'
is missing the following properties from type 'GridColumnDef<AdminReceiptRow>': width, minWidth
```

## 2. Does the missing dimension actually break the rendered grid? — No (key finding)

Traced the full render path for this screen:

```text
receipts-screen.tsx  ServerGridPage  (line 49)
  → useLegacyAdminGridDirectProps({ gridId, columns, queryAdapter, savedViewStore })   (line 80)
  → buildLegacyGridBridge(columns, direction)                     design-system/app-data-grid/legacy-grid-adapters.ts:38
  → actions column branch: width: actionsColumn.width ?? 108, minWidth: actionsColumn.minWidth ?? 100
                                                                  design-system/app-data-grid/legacy-grid-bridge.ts:97-98
```

So the bridge **already supplies finite fallbacks**, and the receipts actions column renders at **108 × 100 today**, before this change. The diagnostic was a *contract* gap — the column definition did not declare what the bridge would use — not a rendering defect. This is why the repair is a two-number contract completion rather than a behaviour change: choosing exactly those fallback values yields a **provably zero visual delta**.

## 3. Dimension rationale (and the alternative considered)

| Candidate | Basis | Verdict |
| --- | --- | --- |
| **`width: 108, minWidth: 100`** (chosen) | Exactly the values `buildLegacyGridBridge` applies to this column today (`?? 108` / `?? 100`). `width > minWidth`, both positive and finite. | **Chosen** — satisfies the required contract with zero change to the rendered grid, which is this task's stated preservation constraint. |
| `width: 88, minWidth: 80` | The nearest single-action precedent uses it for the identical cell (`receiptRowActions` has exactly one entry; `admin-resource-selector.tsx:606-607` uses 88/80 for a single `AppGridRowActionsCell … compact`). | Not chosen: it would *shrink* the column from today's 108 to 88, i.e. introduce a visible layout change the task did not authorise. Recorded as an Architect option. |
| `width: 190, minWidth: 170` | The landing-pages actions column value (`admin-landing-pages-screen.tsx:407-408`). | Rejected: that column carries **five** row actions (`edit`, `preview`, `publish`, `unpublish`, `home`, screen lines 231-274), whereas receipts carries exactly one (`view`, line 111-118). |

Bounded-check: the cell is `<AppGridRowActionsCell row={row} actions={receiptRowActions} compact />` — a single compact icon action — so 108 comfortably fits and `maxWidth` was deliberately **not** added, matching the bridge's "no forced cap" contract for actions columns (`legacy-grid-bridge.ts:100`, and the existing `legacy-grid-bridge.test.ts` case that asserts actions `maxWidth` stays open when not provided).

## 4. Exact change — one file, two numbers plus a comment

```diff
   {
     id: "actions",
     header: "عملیات",
     accessor: () => "",
     cell: (row) => <AppGridRowActionsCell row={row} actions={receiptRowActions} compact />,
+    // GridColumnDef این دو را الزامی می‌داند؛ همان مقادیری که پل گرید برای ستون actions
+    // به‌کار می‌بَرد (width ?? 108 و minWidth ?? 100) صریح می‌شوند تا رفتار visible عوض نشود.
+    width: 108,
+    minWidth: 100,
     exportable: false,
     sortable: false,
   },
```

Untouched and verified by the diff: the column `id`, `header`, `accessor`, `cell`, `exportable`, `sortable`, `receiptRowActions`, `maxWidth` (still absent), every other column's dimensions, the `ServerGridPage` wiring, data loading, filters and sort behaviour. No other column was resized; no blank extra column is introduced (the column already existed and was already rendered — it was never excluded or overridden by a separate actions implementation, which the `buildLegacyGridBridge` actions branch above proves).

## 5. Verification — measured against the committed baseline

Baseline copied verbatim from `docs/architecture/evidence/TB-FE-ADMIN-ORDER-DETAIL-TEST-FIXTURE-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **23** |
| After | **22** |
| Delta | **−1** (exactly the targeted TS2739; `Compare-Object` shows only that removed line and **no** added line) |
| Introduced diagnostics | **0** |
| Diagnostics left in `receipts-screen.tsx` | **none** |

Overall `tsc --noEmit` still exits non-zero with **22 errors**; no greenness is claimed. Raw output: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

| Focused test | Result |
| --- | --- |
| `features/admin-receipts/admin-receipts.characterization.test.ts` — the nearest existing focused receipts suite, and it **reads this screen's source** (`fs.readFileSync(path.join(featureRoot, "components/receipts-screen.tsx"), "utf8")`, line 44) | **5/5 pass, 0 fail** |

Raw output: `focused-test-receipts.txt`. **Pre-existing failures observed: none** in that run, so nothing had to be documented or chased. No test was added or amended — the task forbade ceremonial tests, and the two required contract fields need no new assertion; the source-reading characterization suite plus `tsc` are the appropriate proof for a dimensions-only contract completion.

**Note on a failed command attempt (disclosed for honesty):** the first invocation used an incorrect path (`components/admin-receipts.characterization.test.ts` — the suite actually lives one directory up) and Node reported `Could not find '…'`. That was a path typo, not a test failure; the corrected single run above is the evidence, and no retry loop was performed.

**Not run (deliberately):** the full `npm test` / `test:discovered` suites, dependency install, build, backend suites, browser/visual verification, AMSC, and every other guard or characterization suite. No unrun test is reported as pass.

## 6. Scope, risk, rollback

- **Scope:** the only production change is `src/frontend/features/admin-receipts/components/receipts-screen.tsx`. No `any` / `unknown-as` / `@ts-ignore` / suppression, no optionalizing of the shared `GridColumnDef` dimensions, no tsconfig/CI/baseline change, no backend/schema/storefront/Shopeiva surface, no grid redesign, no unrelated fix.
- **Risk:** none to rendering — the two declared numbers equal the fallbacks the bridge applied before this change, so the pre- and post-change column geometry is identical by construction. The change's only effect is that the definition now states the contract truthfully instead of relying on a renderer fallback.
- **Rollback:** revert the single commit; TS2739 returns and rendering is unchanged (the bridge fallback resumes).

## 7. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
