# TB-FE-ADMIN-ORDER-DETAIL-TEST-FIXTURE-001 — stale admin order-detail fixture (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Expected-Starting-HEAD:** `c204c65b467c4548de44ca8223a6340854ee58b8` — verified `HEAD == origin/main == Expected-Starting-HEAD`, no conflicting active change, pre-existing untracked artifacts left untouched.

## 1. The recorded diagnostic

```text
app/admin/admin-api.test.ts(114,43): error TS2345: Argument of type '{ … payment: null; }' is not assignable to parameter of type 'AdminOrderDetail'.
  Property 'consolidatedPackages' is missing in type '{ … }' but required in type 'AdminOrderDetail'.
```

The test is `enrichAdminOrderDetail keeps explicit server projections`, and the object literal is the argument to `enrichAdminOrderDetail`.

## 2. Source-grounded contract decision (the STOP gate)

The gate question was: **is the required declaration correct, or is this a wire-optionality mismatch that must be reported instead of edited?** Answered from source, not assumed:

| Evidence | What it establishes |
| --- | --- |
| `enrichAdminOrderDetail(detail: AdminOrderDetail)` — `admin-api.ts:387` | Its parameter is the **complete domain read model**, not a raw wire payload. A caller must therefore already hold a valid `AdminOrderDetail`; the fixture is not modelling a partial wire shape. |
| `AdminOrderDetail.consolidatedPackages: AdminConsolidatedPackage[]` — `admin-api.ts:192` (required; **not** `?:`) | The contract states the projection is always present on the read model. |
| `mapAdminOrderDetail(value: unknown)` — `admin-api.ts:541`, building `consolidatedPackages` at `:624` from `array(prop(item, "consolidatedPackages", "ConsolidatedPackages"))` and emitting it at `:679` | The **only** producer of this type always emits the array, defaulting absent wire data to `[]`. So there is no path where the domain read model lacks the field: the required declaration is accurate, and raw wire optionality is already absorbed by `array(...)` inside the mapper. |
| `Props.detail: AdminOrderDetail` in `admin-consolidated-package-section.tsx:40` | Sibling consumers also type it as the required read model. |
| `admin-order-items-shipping.test.ts:367` already supplies `consolidatedPackages: [{ … }]` for the same type | The required contract is honoured consistently elsewhere; this one fixture is the outlier, not the contract. |
| The two `?? []` reads (`admin-consolidated-package-section.tsx:112`, `admin-api.ts:423`) | These are **defensive redundancy on an already-required field**, not evidence that the field is optional; neither is a producer-side omission and neither justifies relaxing the declaration. |

**Decision: stale test setup, not a wire-contract mismatch.** The fix belongs in the fixture. No type, DTO or mapper change is justified, and the STOP/`BLOCKED` branch for "legitimate optionality mismatch" is **not** triggered.

## 3. Is the fixture's empty array the truthful value?

Yes — `[]` is what this scenario genuinely models, checked rather than assumed:

- `sellerOrders: []` — the fixture contains no seller orders at all, and `AdminConsolidatedPackage` members are consolidated from seller-order shipments, so with zero seller orders there is nothing to consolidate.
- The fixture carries no package data anywhere (no `consolidatedPackages`, no `members`, no `packageNumber`), and it deliberately supplies the sibling empty projections `sellerOrders: []` and `financialEvents: []` to prove `enrichAdminOrderDetail` preserves explicit server projections.
- Therefore a **representative non-empty package fixture** would be wrong here: it would inject data the scenario does not have and silently improve a test that is specifically about *preserving empty server projections*.

`[]` is contract-faithful, not a type-appeasing placeholder.

## 4. Exact change — test fixture only, no production change

`src/frontend/app/admin/admin-api.test.ts`:

```diff
     sellerOrders: [],
+    // این سناریو هیچ بستهٔ تلفیقی ندارد (بدون sellerOrder)، و AdminOrderDetail این projection را الزامی می‌داند.
+    consolidatedPackages: [],
     sellerFinancials: [{ sellerOrderId: "so3", … }],
```

Production changes: **NONE.** No change to `AdminOrderDetail` or any DTO, to `mapAdminOrderDetail` / `enrichAdminOrderDetail`, to backend/schema, to any other test, to tsconfig/exclusions, or to any guard or baseline. No `any`, cast, `@ts-ignore` or suppression. No new mock-only production seam. Existing assertions are untouched (`lineCount`, `sellerCount`, `sellerFinancials[0].commissionAmount`).

## 5. Verification — measured against the committed baseline

Baseline copied verbatim from `docs/architecture/evidence/TB-FE-WALLET-GRID-QUERY-TYPE-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **24** |
| After | **23** |
| Delta | **−1** (exactly the targeted TS2345; the `Compare-Object` also shows its two elaboration lines removed and **no** added line) |
| Introduced diagnostics | **0** |
| Diagnostics left in `admin-api.test.ts` | **none** |

Overall `tsc --noEmit` still exits non-zero with **23 errors**; no greenness is claimed. Raw output: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

| Focused test | Result |
| --- | --- |
| `app/admin/admin-api.test.ts` (the directly related suite, run once) | **7/7 pass, 0 fail** |

Raw output: `focused-test-admin-api.txt`. **Pre-existing failures observed: none** in that run, so no baseline failure had to be documented or chased. Nothing was added to any test beyond the one contract-required fixture field.

**Not run (deliberately):** the full `npm test` / `test:discovered` suites, dependency install, build, backend suites, browser/visual verification, AMSC, and every other guard or characterization suite. No unrun test is reported as pass.

## 6. Scope, risk, rollback

- **Scope:** one test file, one required field plus one clarifying comment. No production artefact changed at all.
- **Risk:** none to production — the diff cannot affect shipped code or runtime. The only behavioural effect is on the fixture under test, which now models the read model's mandated shape so that the test exercises what it claims; the three existing assertions are unchanged and still pass.
- **Rollback:** revert the single commit; TS2345 at line 114 returns.

## 7. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
