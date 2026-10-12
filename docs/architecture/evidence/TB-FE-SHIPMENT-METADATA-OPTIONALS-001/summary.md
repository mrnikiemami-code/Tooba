# TB-FE-SHIPMENT-METADATA-OPTIONALS-001 — Optional shipment metadata (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY` · **Branch:** `main`
**BASE / expected starting HEAD:** `f9b66c15754188f6d9614130978f459a883110d6` — preflight verified `HEAD == origin/main == Expected-Starting-HEAD`, no tracked modification, no divergence.

## 1. The four recorded diagnostics

All four are TS2322 in `src/frontend/app/admin/admin-create-shipment-modal.tsx`, and all four are the same shape: an explicit `undefined` written into an object literal whose declared type is `Record<string, string | number>`.

| Line (baseline) | Expression | Cluster |
| --- | --- | --- |
| 251 | `weightKg: num("weightKg") ?? undefined` | `post` |
| 253 | `declaredValue: num("declaredValue") ?? undefined` | `post` |
| 263 | `weightKg: num("weightKg") ?? undefined` | `tipax` |
| 265 | `declaredValue: num("declaredValue") ?? undefined` | `tipax` |

`num()` already returns `number | undefined`, so `?? undefined` is a pure no-op that only exists to *look* like the `str(...) ?? ""` siblings.

## 2. Wire contract verified before choosing the repair (no invention)

The metadata is **opaque** all the way to persistence — no validator requires these keys:

- Producer: `buildMetadata()` → `Object.fromEntries(Object.entries(metadata).filter(([, v]) => v !== undefined && v !== ""))` → `JSON.stringify(cleaned)` into `providerMetadataJson` (`admin-create-shipment-modal.tsx:309-329`).
- Frontend transport: `src/frontend/app/admin/admin-order-operations.ts:90,289` → `providerMetadataJson?: string | null`.
- Order module: `AdminOrderOperationsModels.cs:81` `string? ProviderMetadataJson = null` → passed through verbatim by `AdminOrderOperationsOrchestrator.Fulfillment.cs:233`.
- Fulfillment module: `FulfillmentAdminOperationsContracts.cs:84,228`, `FulfillmentUnit.cs:339,360`, `FulfillmentDirectory.cs:69`, `Shipment.cs:38,90` — `string?`, stored in a `text` column (`FulfillmentDbContext.cs:125`) after `string.IsNullOrWhiteSpace(...) ? null : providerMetadataJson.Trim()`.

There is **no** key-level validation of `weightKg` / `declaredValue` anywhere in the chain, and **omitted keys are already the contract**: the existing `cleaned` filter has always dropped them at runtime. So the four diagnostics were a *type-honesty* defect, not a behaviour defect — and no default weight/declared value may be invented.

## 3. Chosen repair — smallest contract-honest option, keys omitted by construction

Of the two options the task offers, this repair takes *"construct optional keys only when values exist"* and leaves the declared type `Record<string, string | number>` **true by construction** rather than widening an intermediate to admit `undefined`:

```diff
+    // فیلدهای اختیاری عددی: نبودِ مقدار معتبر یعنی نبودِ کلید، نه درجِ undefined.
+    // این همان قراردادی است که فیلتر نهایی روی metadata تضمین می‌کند.
+    const optionalNum = (key: string): Record<string, number> => {
+      const value = num(key);
+      return value === undefined ? {} : { [key]: value };
+    };
+
       case "post":
         return {
           serviceType: str("serviceType") ?? "",
           packageCount: num("packageCount") ?? 1,
-          weightKg: num("weightKg") ?? undefined,
+          ...optionalNum("weightKg"),
           dimensions: str("dimensions") ?? "",
-          declaredValue: num("declaredValue") ?? undefined,
+          ...optionalNum("declaredValue"),
```

…and the identical two-line substitution in the `tipax` branch.

- **Zero `any`, `@ts-ignore`, assertion or suppression.** `{ [key]: value }` with `value: number` types as `{ [x: string]: number }`, assignable to `Record<string, number>` — no cast needed.
- **Wire-byte-identical.** Before: the key existed with value `undefined` and was then removed by the `cleaned` filter. After: the key is absent from the start. `JSON.stringify` output is the same string, so the Host sees exactly the same `providerMetadataJson`.
- **Omission semantics preserved:** blank or non-finite input → `num()` returns `undefined` → no key. A supplied valid number → `{ weightKg: 12.5 }`, still **numeric** (never stringified).
- **Everything else untouched:** `packageCount ?? 1`, all `str(...) ?? ""` required fields, `snapp_courier` / `store_courier` / `in_person` branches, `selections`, `carrierDisplayName`, `shippingMethodCode`, the `cleaned` filter, the `result.state !== "ok"` error mapping and toasts.

## 4. TypeScript — measured, one check

`cd src/frontend && npx tsc --noEmit` against the committed baseline copied from `docs/architecture/evidence/TB-FE-ADMIN-MENUS-HELPER-SIGNATURES-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **38** |
| After | **34** |
| Delta | **−4** (expected `38 → 34`) |
| New diagnostics | **0** |
| `Compare-Object` of both outputs | no added line |
| Diagnostics left in the shipment modal | **none** |

Exactly the four targeted TS2322 diagnostics disappeared. Overall `typecheck` remains **not green (34 errors)** and no greenness is claimed.

## 5. Focused test executed — minimal amendment to an existing test

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test app/admin/admin-order-items-shipping.test.ts` | **17/17 pass, 0 fail** |

The existing focused shipment suite already guards this modal by source contract (`create shipment modal sends selections without deferred T005 block`, line 258) and is discovered by `test:discovered` (thus discovery-compatible). Per the task's explicit allowance, it was amended with **one** additional test rather than adding a new file:

```ts
test("optional shipment metadata is omitted, never serialized as undefined", () => {
  const modal = readFileSync(join(dir, "admin-create-shipment-modal.tsx"), "utf8");
  assert.doesNotMatch(modal, /\?\?\s*undefined/);
  assert.match(modal, /v !== undefined && v !== ""/);
});
```

**Why this was needed:** `buildMetadata()` is module-internal and unexported, so there is no behaviour-level seam for the omission contract; and the alternative (exporting it) would create a production seam, which is out of scope. This guard pins the two invariants that actually define the repair — the metadata literal must never again carry an explicit `undefined`, and the canonical `v !== undefined && v !== ""` filter that removes blanks must stay — and it matches the source-contract test style already used throughout this file. It adds **0** new TypeScript diagnostics.

**Pre-existing failures:** none observed in this file. **Tests NOT run:** the full `npm test` / `test:discovered`, `test:admin*` and `test:critical-storefront` suites, any build, backend suite, browser/live check, and any AMSC wave. No unrun test is reported as pass.

## 6. Scope, risks, rollback

- **Scope:** production change is exactly `src/frontend/app/admin/admin-create-shipment-modal.tsx`. The only other change is the minimal amendment to the existing focused shipment test, plus the receipt and this evidence. No generic form control, no API/DTO/backend contract, no type outside the module, no Home/PDP/Shopeiva surface, no `tsconfig`, guard, CI or baseline was touched.
- **Residual risk: negligible and behaviour-free.** The Host-facing JSON string is unchanged for every branch, verified against the whole producer→persistence chain above; the change is a type-honesty refactor plus one focused guard test.
- **Rollback:** revert the single commit; two substitution sites plus one test hunk.

## 7. Stop state

`automaticNextImplementationTask = NONE`. 34 frontend diagnostics remain in the other audited clusters and require their own bounded tasks.
