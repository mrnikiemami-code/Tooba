# TB-FE-MIDDLEWARE-LOCALE-TYPE-IMPORT-001 — Locale type ownership fix evidence

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Branch:** `main`
**BASE / expected starting HEAD:** `60e2623602bdeeb452f3aa3cc65703595bc6521d` (verified `HEAD == origin/main == Expected-Starting-HEAD`, clean tracked worktree at preflight)

## 1. Source proof — the real ownership mismatch

- `src/frontend/lib/i18n/locale.ts:6,7` is the canonical owner:
  `export const LOCALES = ["fa", "en"] as const;` / `export type Locale = (typeof LOCALES)[number];`
- `src/frontend/lib/i18n/routing.ts:8` **imports** that type (`import { … type Locale, } from "./locale.ts"`) and never re-exports it, so `type Locale` is declared locally in `routing.ts` while not being exported.
- `middleware-locale.ts` therefore produced `TS2459: Module '"./routing.ts"' declares 'Locale' locally, but it is not exported.`

The correct repair is to take the type from its true owner, **not** to add a re-export in `routing.ts` (which would have concealed the misownership and widened `routing.ts`'s public surface for a compiler convenience).

## 2. Change — one production file, one import move

`src/frontend/lib/i18n/middleware-locale.ts`:

```diff
 import {
   isExcludedFromLocalePrefix,
   isPublicStorefrontPath,
   parseInvalidLocalePrefix,
   parseLocalePrefix,
   resolvePreferredLocale,
-  type Locale,
 } from "./routing.ts";
+import type { Locale } from "./locale.ts";
```

- All five **runtime** imports stay on `./routing.ts` — routing behaviour, middleware decisions, URL prefixes, cookies, `Accept-Language`, fallback and the public routing surface are untouched.
- `Locale` stays a **type-only** import (`import type`), so no runtime module edge is added and no bundling behaviour changes.
- `Live` / `LocaleMiddlewarePlan` and the function bodies are unchanged; the file's lint-relevant shape only moves a type specifier.

**No other file was modified.** No `routing.ts` re-export, no locale inventory change, no `tsconfig`, no baseline, no guard, no test change (no test amendment was needed — the existing i18n tests are directly relevant and pass unmodified).

## 3. Measured diagnostics vs the saved 42-error baseline

`cd src/frontend && npx tsc --noEmit` (TypeScript 5.9.3), baseline captured from `docs/architecture/evidence/TB-FE-CATEGORY-METAKEYWORDS-CONTRACT-001/typecheck-after-tsc.txt` into `typecheck-before.txt`:

| | Errors | `TS2459` |
| --- | --- | --- |
| Before (saved baseline) | **42** | 1 |
| After | **41** | **0** |
| Delta | **−1** | removed |
| New diagnostics | **0** | — |

Exactly the expected `TS2459` removed, matching the Architect's prediction of `42 -> 41`, with no unexpected or relocated diagnostics. No diagnostic remains anywhere under `lib/i18n`. Overall `typecheck` is **NOT green and is not declared green** — 41 errors remain in the other audited clusters.

## 4. Focused test actually executed

| Command | Result |
| --- | --- |
| `npm run test:i18n` (`lib/i18n/locale.test.ts`, `lib/i18n/routing.test.ts`, `lib/i18n/middleware-locale.test.ts`) | **19/19 pass, 0 fail** |

This is the directly relevant existing suite for the touched module and covers the middleware locale plan, routing prefix behaviour and locale inventory — so the change was verified against real behaviour, not only the compiler. **Pre-existing failures: none observed.**

**Tests deliberately NOT run:** no broad suites (`test:discovered`, `test:admin*`, `test:critical-storefront`), no `build`, no backend build/tests, no browser/live check, no AMSC wave. No unrun test is reported as PASS. **Critical-storefront scope note:** a single `lib/i18n` type import changed; no Home/PDP/listing/storefront or shared-composition surface and no visual primitive is in scope.

## 5. Scope, risks, rollback

- **Risk: negligible.** One type-specifier relocation in one file, with zero runtime delta and a fully green, directly relevant i18n suite.
- **Rollback:** revert the single commit; the change is a one-line import move plus docs.

## 6. Stop state

`automaticNextImplementationTask = NONE`. 41 diagnostics remain in the other audited clusters (notably `shared-composition-renderer` 9, `admin-create-shipment-modal` 4, `menus/admin-menus-api` 3) and require their own bounded tasks and Architect decisions.
