# TB-FE-RESERVATION-POLICY-NUMERIC-FIELD-KEY-001 — narrow numeric-text field key (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR_WITH_STOP_GATE` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY`
**Branch:** `main` · **Expected-Starting-HEAD:** `fd2ad0e82ed916a515756dacc876151605358353` — verified `HEAD == origin/main == Expected-Starting-HEAD`, no conflicting tracked changes, no untracked artifact touched.

## 1. The recorded diagnostic

```text
app/admin/reservation-policy-editor.tsx(112,17): error TS2322: Type 'string | boolean' is not assignable to type 'string | number | readonly string[] | undefined'.
  Type 'false' is not assignable to type 'string | number | readonly string[] | undefined'.
```

## 2. Root cause — the task's hypothesis is CONFIRMED

The declared shape splits cleanly into two groups (`reservation-policy-editor.tsx:10-18`):

```ts
export interface ReservationPolicyDraft {
  initial: string;          // ── string group
  retry: string;
  max: string;
  inheritInitial: boolean;  // ── boolean group
  inheritRetry: boolean;
  inheritMax: boolean;
}
```

The descriptor array at line 57 is the runtime consumer that drives both the text input and the checkbox, and it declared its key as the **whole** draft key space:

```ts
const fields: Array<{ key: keyof ReservationPolicyDraft; … }> = [
  { key: "initial", field: view.initialHold, … },
  { key: "retry",   field: view.retryHold,   … },
  { key: "max",     field: view.maxCycles,   … },
];
```

`keyof ReservationPolicyDraft` includes `inheritInitial | inheritRetry | inheritMax`, so `draft[key]` is `string | boolean`. At line 112 that value is fed to `value={draft[key]}` on `<input type="text">`, whose `value` accepts `string | number | readonly string[] | undefined` — booleans are rejected, hence TS2322. Note the compiler promptly demonstrates the impossible branch itself: *"Type 'false' is not assignable…"*. The three descriptors in the literal are **always** string keys at runtime; the widening was purely a declaration defect. Verified all three descriptors and every runtime consumer before editing (see §5).

**No `BLOCKED_ARCHITECT_DECISION` was needed:** the mapping supports the repair without touching runtime behavior, as §4 proves.

## 3. Exact change — a local, narrower key type

```diff
@@ -17,6 +17,13 @@ export interface ReservationPolicyDraft {
   inheritMax: boolean;
 }
 
+/**
+ * کلیدهای مقدار متنی در draft (سه فیلد عددی). این سه فیلد همیشه رشته‌اند و
+ * کلیدهای inherit… بولین هستند؛ جدا کردن این دو گروه، جلوی رسیدن مقدار بولین
+ * به ورودی متنی را می‌گیرد و شکل واقعی ReservationPolicyDraft دست‌نخورده می‌ماند.
+ */
+type ReservationPolicyValueKey = "initial" | "retry" | "max";
+
 export function draftFromView(…)
@@ -50,7 +57,7 @@ export function ReservationPolicyEditor({
   const dir = locale === "en" ? "ltr" : "rtl";
-  const fields: Array<{ key: keyof ReservationPolicyDraft; field: … }> = [
+  const fields: Array<{ key: ReservationPolicyValueKey; field: … }> = [
     { key: "initial", field: view.initialHold, unitFa: "دقیقه", unitEn: "minutes" },
     { key: "retry", field: view.retryHold, unitFa: "دقیقه", unitEn: "minutes" },
     { key: "max", field: view.maxCycles, unitFa: "دفعه", unitEn: "cycles" },
```

`ReservationPolicyValueKey` is a **local, unexported, erased type alias** — it emits no JavaScript and is not part of any cross-module contract. `ReservationPolicyDraft`'s true shape is **unchanged** (the `inherit…` booleans still exist and are still booleans).

**Why the literal union rather than a mapped derivation** (`{[K in keyof Draft]: Draft[K] extends string ? K : never}[keyof Draft]`): the mapped form would silently admit *any future string-valued field* into the descriptor key space, which is exactly the class of drift that caused this bug. The explicit union keeps the descriptor list and the numeric-text intent tightly coupled.

## 4. Runtime invariants — why this is type-only

| Concern | Proof |
| --- | --- |
| Descriptor array literal | Byte-identical: same three objects, same order, same `field` bindings (`view.initialHold`, `view.retryHold`, `view.maxCycles`), same `unitFa`/`unitEn`. Only the *annotated type* of the already-present `key: "initial" \| "retry" \| "max"` literals narrowed. |
| Inherit-key mapping (line 86-ish) | Unchanged: `key === "initial" ? "inheritInitial" : key === "retry" ? "inheritRetry" : "inheritMax"`. For the three actual keys the branches are identical; narrowing only makes the final `else` provably `"max"`. |
| Checkbox handler | `[inheritKey]: event.target.checked` plus `[key]: event.target.checked ? "" : draft[key]` — `draft[key]` is now *typed* `string` and is still the **same runtime string** from the same object. |
| Text `onChange` | `{ ...draft, [key]: event.target.value }` unchanged. |
| Save contract | `readReservationWrite(draft)` and `draftFromView(view)` untouched; the emitted request body (`initialReservationHoldMinutes` / `retryReservationHoldMinutes` / `maxReservationCycles`) is untouched. |
| Draft shape | `ReservationPolicyDraft` not modified, not made partial, not optionalised. |
| Type erasure | `type` alias → zero runtime output; no `as`, no `!`, no `any`, no `ts-ignore`, no `unknown` cast, nothing stringified. |

**Verification of the "always string keys" premise:** the only place `key` is *read* is the single `fields.map` inside `ReservationPolicyEditor`, and all three descriptors are string literals; nothing ever pushes a boolean key into `fields`. The narrowed union is therefore a strictly faithful annotation, not a behavioural narrowing.

## 5. Diagnostics — 21 → 20, verified per file

Baseline copied verbatim from the task-specified file `docs/architecture/evidence/TB-FE-CONTENT-CATEGORY-MEDIA-PREVIEW-NULL-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **21** |
| After | **20** |
| Delta | **−1** |
| Introduced diagnostics | **0** |

Per-file tallies (before → after) prove the *only* vanished file is the target:

```text
reservation-policy-editor.tsx        1 → 0     ← the targeted TS2322, gone entirely
category-admin-screen.tsx            1 → 1
content-list.tsx                     1 → 1
landing-pages/admin-builder-ux-r8.guard.test.ts  1 → 1
landing-pages/admin-resource-selector.tsx        1 → 1
product-rich-text-editor.tsx         1 → 1
storefront/storefront-landing-blocks.tsx         1 → 1
template-preview/fashion/fashion-template-preview-view.tsx 1 → 1
features/admin-languages/…/language-list.tsx     3 → 3
lib/storefront-composition/fashion-demo-preview.ts 1 → 1
lib/storefront-composition/shared-composition-renderer.tsx 9 → 9
```

**Existing-failure disclosure (required by the task):** `Compare-Object` also reports one *text-difference* on the pre-existing `category-admin-screen.tsx(1801,5)` line. It is the **same pre-existing error** — same file, same line/column, same `TS2322`, same `EditSurface | null` target — differing only in the order TypeScript prints members of the union literal:

```text
before: Type '"translations" | "facets" | "general" | "attributes" | "mega-menu" | "reservation"'
after:  Type '"general" | "translations" | "attributes" | "facets" | "mega-menu" | "reservation"'
```

TypeScript's union-member printing order is not stable across runs. The per-file tally above (`category-admin-screen.tsx 1 → 1`) confirms this is a cosmetic reordering of an untouched pre-existing error, not a new diagnostic and not caused by this edit — this file is not in the repair's scope and was not opened or modified. `tsc --noEmit` still exits non-zero with **20 errors**; no greenness is claimed.

Raw: `typecheck-after-tsc.txt`, baseline: `typecheck-before.txt`.

## 6. Focused tests

| Focused suite | Result |
| --- | --- |
| `app/admin/reservation-policy-admin.test.ts` + `app/admin/admin-reservation-cycle.test.ts` (both directly relevant; the first reads this very file, `readFileSync(join(dir, "reservation-policy-editor.tsx"), "utf8")` line 16) | **11 tests, 11 pass, 0 fail** |

**Pre-existing failures observed: none**, so nothing had to be documented or chased; exactly one run, no test loop.

Per the task's own caveat, **these are source-reading guards, so they are corroborating evidence, not behavioural proof** — they assert the reservation-policy surfaces and helpers still exist/wire up (e.g. `reservation-policy-inherit-${key}`), which the unchanged descriptors and handlers satisfy. No test was added or amended: the change is erased typing only, so a new assertion would be ceremonial, which the task forbade. Raw: `focused-tests.txt`.

Not run (deliberately): full suite / `test:discovered`, build, browser/visual verification, backend suites, AMSC, other guards. No unrun test is reported as pass.

## 7. Changed files

```text
src/frontend/app/admin/reservation-policy-editor.tsx   (M)  — the only production change (task ALLOWED scope)
docs/ai/tasks/TB-FE-RESERVATION-POLICY-NUMERIC-FIELD-KEY-001.task.md  (A)
docs/architecture/evidence/TB-FE-RESERVATION-POLICY-NUMERIC-FIELD-KEY-001/{summary.md,typecheck-before.txt,typecheck-after-tsc.txt,focused-tests.txt}  (A)
```

No backend, DTO/schema, business-policy, inventory-reservation, storefront/Home/PDP/Shopeiva, tsconfig/CI/baseline/guard change; no cross-module cleanup; no unrelated fix; no AMSC; no follow-on task started.

## 8. Risk and rollback

- **Risk: very low.** The change is a local type annotation on an array literal whose runtime contents are byte-identical, so no value, toggle, tab, request body or rendered markup can change. The only conceivable risk is a compile-time one — a future boolean-keyed descriptor would now be rejected at the annotation instead of silently reaching an `<input>`; that is the intended guard, not a regression.
- **Rollback:** revert the single commit; TS2322 returns and the annotation re-widens to `keyof ReservationPolicyDraft`. No data, migration or contract rollback is involved.

## 9. Stop state

`Automatic-Next-Implementation-Task: NONE`. Awaiting independent Architect review / ACCEPT.
