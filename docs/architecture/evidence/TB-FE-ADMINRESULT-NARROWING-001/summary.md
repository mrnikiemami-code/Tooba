# TB-FE-ADMINRESULT-NARROWING-001 — Discriminated AdminResult narrowing evidence

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Branch:** `main`
**BASE / expected starting HEAD:** `48a59532c1f5e7e961f047518f5bfcea1313a4c4` (verified `HEAD == origin/main` at preflight)

## 1. Contract inspection before editing (real declarations, not assumptions)

Two **different** `AdminResult` shapes were involved, and no competing definition was introduced:

- `app/admin/landing-pages/admin-landing-pages-api.ts:57`
  `AdminResult<T> = { ok: true; data: T } | { ok: false; message: string; denied?: boolean }` — a full discriminant union whose failure branch always carries `message`.
- The settings modules (`quantity-settings-api.ts:32,51`, `hold-policy-settings-api.ts`, `checkout-identity-settings-api.ts`, `checkout-abuse-settings-api.ts`, `appearance-settings-api.ts`) use
  `{ ok: true; data: X } | { ok: false; denied?: boolean; message?: string }` — so on the success branch neither `denied` nor `message` exists.

`ok` is the discriminant in both. Reading `denied`/`message` before testing `ok` is therefore a genuine narrowing defect, not a type-definition gap.

## 2. Changes (minimal, semantics-preserving)

### 2.1 `src/frontend/app/admin/settings/page.tsx` — 5 sites

`if (result.denied)` → `if (!result.ok && result.denied)` at lines 244, 277, 297, 330, 355 (quantity rounding, hold policy, checkout identity, checkout abuse, appearance).

Behaviour preserved exactly: `denied` is never present on a success payload at runtime or in the type, so the guard still fires only for the 401/403 path; the subsequent `if (!result.ok)` generic-error and message branches are untouched, and the success path is unchanged.

### 2.2 `src/frontend/app/admin/landing-pages/admin-landing-pages-screen.tsx` — message sourcing

Before:

```ts
if (!pageResult.ok || !sectionResult.ok) {
  setBusy(false);
  setMessageTone("error");
  setMessage(pageResult.ok ? sectionResult.message : pageResult.message);
  return;
}
```

After:

```ts
if (!pageResult.ok) {
  setBusy(false);
  setMessageTone("error");
  setMessage(pageResult.message);
  return;
}
if (!sectionResult.ok) {
  setBusy(false);
  setMessageTone("error");
  setMessage(sectionResult.message);
  return;
}
```

The nested `||` gave TypeScript no way to correlate the two results, so the inner ternary could not narrow `sectionResult` to its failure branch (the reported `TS2339`). The rewrite makes each discriminant explicit. **Message precedence is identical**: a failed page fetch still wins over a failed section fetch, and when both fail the page message is shown — the same result the original expression produced. `setMessageTone("error")` and `setBusy(false)` still run exactly once per failure. No new user-facing text was invented. Downstream `pageResult.data.*` / `sectionResult.data` narrowing is unchanged (both are narrowed to `ok: true` after the guards, exactly as the previous combined guard did).

### 2.3 `src/frontend/app/admin/landing-pages/admin-landing-pages-screen.tsx:431` — MouseEvent leak

`onClick={refresh}` → `onClick={() => refresh()}` on the denied-state "تلاش دوباره" (retry) button.

`refresh` is `(opts?: { notice?: string; tone?: "error" | "success" }) => …`. Passing it directly made React hand it a `MouseEvent`, so `opts.notice` was silently undefined and the option object was a lie; the retry itself still worked by accident. The zero-argument wrapper calls the same function with the same absence of options, so **the runtime retry behaviour is unchanged** and no notification is lost — button handlers were never able to supply `notice`/`tone` here anyway.

### 2.4 Focused regression coverage (2 existing files amended)

- `app/admin/landing-pages/admin-landing-pages.guard.test.ts` — 2 new source-guard tests: (a) the correlated `pageResult.ok ? sectionResult.message : pageResult.message` pattern is gone and each result has its own explicit failure guard; (b) `onClick={refresh}` is gone in favour of `onClick={() => refresh()}`. This file already reads the screen source and is the established guard seam for this surface.
- `app/admin/admin-appearance-settings.test.ts` — 1 new source-guard test: no bare `if (result.denied)` remains in `settings/page.tsx` and exactly 5 `if (!result.ok && result.denied)` narrowings exist. This file already reads `settings/page.tsx`.

Both follow the repository's existing regex-guard convention (as in `column-manager.test.ts` / `admin-grid-migration.test.ts`). No new file and no new harness was created, so canonical test discovery is unaffected.

## 3. Measured typecheck delta

`cd src/frontend && npx tsc --noEmit` (TypeScript 5.9.3):

| | Errors |
| --- | --- |
| Recorded baseline | **56** |
| After this task | **49** |
| Removed | **7** |
| **Added (regressions)** | **0** |

Machine diff (file+line+code) against the 56-error raw baseline → `removed=7 added=0`. The 7 removed are exactly the six `TS2339` and one `TS2322` named by the Architect:

- `settings/page.tsx` `244`, `277`, `297`, `330`, `355` — `TS2339` `denied` on the union.
- `admin-landing-pages-screen.tsx` `130` — `TS2339` `message` on `AdminResult<AdminLandingSection[]>`.
- `admin-landing-pages-screen.tsx` `431` — `TS2322` handler signature.

No diagnostic remains in either owned file, and `TS2322` dropped 14→13 while `TS2339` dropped 16→10, with every other code count unchanged. Overall `typecheck` is **NOT green and is not declared green** (49 errors remain, all in the other audited clusters).

## 4. Tests actually run

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test app/admin/landing-pages/admin-landing-pages.guard.test.ts` | 4 pass / **2 fail** (both pre-existing, proven below) |
| `node --experimental-strip-types --test app/admin/admin-appearance-settings.test.ts` | **6/6 pass** |
| `node --experimental-strip-types --test app/admin/reservation-policy-admin.test.ts app/admin/admin-nav-integrity.test.ts` | **16/16 pass** |
| `npm run test:architecture` | **11/11 pass** |

**Pre-existing failures, recorded faithfully, not caused by this task (no retry loop):** in `admin-landing-pages.guard.test.ts`, `"section chooser has Persian labels and no raw JSON editor"` fails because `landing-section-catalog.ts` does not contain `/بنر اصلی/`, and `"TB-P10-T022 ordinary-user Admin UX…"` fails because `admin-landing-page-composer.tsx` contains `JSON.stringify(`. Proof both are pre-existing: `git status` shows only 4 files modified by this task (`settings/page.tsx`, `admin-landing-pages-screen.tsx`, and the two amended test files) — `landing-section-catalog.ts` and `admin-landing-page-composer.tsx` are untouched — and at HEAD `git show HEAD:…landing-section-catalog.ts` contains no "بنر اصلی" while `git show HEAD:…admin-landing-page-composer.tsx` does contain `JSON.stringify(`. The two failing assertions read only those untouched files; both new tests in the same file pass. These failures belong to the landing composer/catalog cluster and were deliberately **not** masked or "fixed".

**Tests deliberately NOT run / not claimed:** `npm run test:discovered` (full suite), `npm run test:admin`, `npm run test:critical-storefront`, `npm run build`, backend build/tests, browser/live verification, any AMSC wave. No unrun test is reported as PASS.

**Critical-storefront scope note:** only two Admin screens and two Admin test files were touched. No Home, PDP, listing, storefront or shared-composition surface, and no design-system primitive, was modified, so no locked visual contract is in scope and the critical visual suite is not applicable.

## 5. Scope statement and risks

- **Touched in production:** exactly the two permitted files. **Touched in tests:** two existing focused Admin test files, each explicitly explained above. Nothing else.
- **Not done:** no new `AdminResult` definition, no global contract refactor, no `any`/`unknown`-as masking, no `@ts-ignore`, no cast, no `tsconfig` change, no suppression, no guard weakening, no baseline reset, no backend/schema change, no unrelated screen, no Home/PDP surface, no auto AMSC, no auto-next task.
- **Residual risk: very low.** Both edits are consumer-side narrowing/callback corrections with provably identical runtime behaviour; no contract, payload, route, visual or persistence change. Rollback is a single-commit revert of two Admin screens plus two test files.

## 6. Stop state

`automaticNextImplementationTask = NONE`. 49 diagnostics remain in the other audited clusters (notably `shared-composition-renderer` 9, `category-admin-screen` 6, `admin-create-shipment-modal` 4) and require their own bounded tasks and Architect decisions.
