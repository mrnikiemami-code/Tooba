# TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001 — Frontend typecheck root-cause audit

**Mode:** `AUDIT_READ_ONLY` — no production code, test, baseline or gate was changed.
**Branch:** `main` · **BASE SHA (preflight/final, unchanged tree):** `0420bb14a88b87b1230320102cb97fd8d8f47127` (= `origin/main`)

## 1. Reproducible evidence

| Item | Value |
| --- | --- |
| Command (declared script) | `cd src/frontend && npm run typecheck` → `tsc --noEmit` |
| Script count in `typecheck` | exactly **one** check (`tsc --noEmit`); no second tool, no lint/build |
| npm-script exit code | `2` (npm wraps the compiler's non-zero exit) |
| Direct `node_modules/.bin/tsc --noEmit` exit code | `1` |
| local TypeScript | `5.9.3` (note: `package.json` devDependency range is `^5.7.3`) |
| Node / npm | `v24.15.0` / `11.12.1` |
| `tsconfig` | unchanged, not modified, not excluded |

Raw, unabridged captures kept in this directory:

- `typecheck-npm-raw.txt` — verbatim `npm run typecheck` run (207 lines, includes the npm `devdir` config warning preamble).
- `typecheck-tsc-raw.txt` — verbatim `tsc --noEmit` run (195 lines, compiler output only).

Note: the declared script emitted an unrelated `npm warn Unknown env config "devdir"` on stderr; it is environment noise, not a typecheck failure.

**Measured total: exactly 155 TypeScript errors in 31 unique files.** The reported "~155" is still accurate at BASE SHA.

## 2. Error-code distribution (sum = 155)

| TS code | Count | Meaning |
| --- | --- | --- |
| TS2741 | 97 | property missing in object literal but required in target |
| TS2339 | 16 | property does not exist on type |
| TS2322 | 15 | type not assignable |
| TS2345 | 7 | argument type not assignable |
| TS7006 | 6 | parameter implicitly `any` |
| TS2353 | 5 | excess property on object literal |
| TS2305 | 2 | module has no exported member |
| TS2554 | 2 | wrong argument count |
| TS2304 | 1 | cannot find name |
| TS2344 | 1 | type argument does not satisfy constraint |
| TS2459 | 1 | module declares type locally but does not export it |
| TS2739 | 1 | object missing multiple required properties |
| TS2740 | 1 | object missing multiple required properties (assignment) |

## 3. Root-cause clusters (sum = 155, verified)

| # | Cluster | Errors | Codes | Root classification |
| --- | --- | --- | --- | --- |
| C1 | Legacy `GridColumnDef.maxWidth` required but omitted by column literals | **96** | TS2741×95, TS2322×1 | **Direct root** — single declaration lies about runtime |
| C2 | `SectionDisplayConfig` vs `shared-composition-renderer` banner fields | **9** | TS2339×8, TS2741×1 | **Direct root** + real behaviour gap |
| C3 | Half-migrated columns use AG-Grid-only props (`flex`, `pinned`) | 5 | TS2353×5 | Contract-direction decision |
| C4 | `legacy-grid-bridge` casts `col.id` string into `ColDefField<T>` | 1 | TS2322×1 | Direct root, local |
| C5 | Discriminated-union member accessed before narrowing (`AdminResult`) | 6 | TS2339×6 | **Direct root**, consumer-side |
| C6 | Stale category meta-keyword API exports (+ stale test) | 4 | TS2305×2, TS2339×2 | Direct root + stale test |
| C7 | `Locale` not re-exported by `lib/i18n/routing.ts` | 1 | TS2459×1 | Direct root, import-only |
| C8 | Missing local annotations in admin screens (implicit `any`, missing import, enum) | 8 | TS7006×6, TS2304×1, TS2322×1 | Local consumer debt |
| C9 | Form/input value-type drift (`string \| undefined`, `string \| boolean`, `actionSlots: 5`) | 7 | TS2322×7 | Local + one unsupported case |
| C10 | Domain contract drift (order detail test, hero slide, brand, template origin) | 4 | TS2345×1, TS2740×1, TS2322×1 | Local / contract |
| C11 | Admin menus API stale shared-helper signatures | 3 | TS2345×2, TS2554×1 | Legacy module not migrated |
| C12 | Independent local defects in admin screens (mixed) | 10 | TS2322×3, TS2345×3, TS2554×1, TS2739×1, TS2344×1, TS2322×1 | Local, no shared root |
| | **Total** | **155** | | |

Cluster arithmetic: `96+9+5+1+6+4+1+8+7+4+3+10 = 155`, and per-code sums match §2 exactly.

## 4. Top 3 root causes (direct, not cascading)

### RC1 — `GridColumnDef.maxWidth` is required while the runtime treats it as optional (96 errors, 62 % of all errors)

- Owner declaration: `src/frontend/design-system/data-grid/types.ts:102` → `maxWidth: number;`
- Introduced as required in `0ba8d1cfe` ("feat add Tooba professional data grid foundation [TB-P04-T003]") and never adjusted; ~20 screens import `GridColumnDef` from `design-system/data-grid` as their live contract, and 95 column literals omit `maxWidth`.
- The declaration contradicts the real runtime in three independent places:
  1. `design-system/app-data-grid/legacy-grid-bridge.ts:76` maps `maxWidth: col.maxWidth` straight into AG Grid `ColDef.maxWidth`, which is **optional** and ignores `undefined`.
  2. The same bridge deliberately *omits* the cap for the actions column and guards `actionsColumn.maxWidth != null` (`legacy-grid-bridge.ts:96`) with the explicit comment "بدون سقف اجباری".
  3. `design-system/app-data-grid/legacy-grid-bridge.test.ts:66` already asserts "leaves actions maxWidth open when not provided".
- Consequence: every column literal without a cap is a **phantom error** — runtime behaviour is already `undefined` (no cap). This is type drift, not 95 separate UI bugs.
- Affected owners (all consumers, no cross-module coupling): `app/admin/admin-screens.tsx` (11), `admin-fulfillment-work-queue-screen.tsx` (13), `admin-returns-work-queue-screen.tsx` (12), `app/admin/landing-pages/admin-resource-selector.tsx` (16), `admin-landing-pages-screen.tsx` (9), `admin-order-detail-screen.tsx` (8), `features/admin-languages/components/language-list.tsx` (8), `features/admin-receipts/components/receipts-screen.tsx` (7), `catalog-units-screen.tsx` (6), `shipping-services-screen.tsx` (5), `design-system/app-data-grid/legacy-grid-bridge.test.ts` (2).

### RC2 — `SectionDisplayConfig` is a whitelist projection that silently drops the keys the shared renderer consumes (9 errors)

- `lib/storefront-composition/shared-composition-renderer.tsx:244-256` reads `config.bannerImageUrl`, `config.bannerMediaAssetId`, `config.bannerHref` on a parameter typed `SectionDisplayConfig`.
- `SectionDisplayConfig` is owned by `app/composition/composition-api.ts:65` and **does not declare** any banner key — while it *does* declare `heightPreset`, which the parser never populates: a second dead field proving the type is out of sync with the payload.
- `parseSectionDisplayConfig()` (`composition-api.ts:215-231`) is an explicit whitelist that only maps `title`, `subtitle`, `href`, `itemCount`, `sourceKind` — so even a correctly typed `bannerImageUrl` would be **dropped at runtime** for `/fa` Home sections.
- The same banner keys are already used by `app/admin/landing-pages/admin-landing-section-forms.tsx` (writes them) and `app/storefront/storefront-product-showcase-embla-rails.tsx:381-382` (consumes them), so the payload contract exists; only the Home read path loses it.
- Home surface: this is Shopeiva-locked (`docs/visual-baselines/HOME-FIDELITY-CONTRACT.md`). Widening the type/parser is the *fix*, but any change that starts rendering previously-dropped banner config **changes Home output** → requires visual review (`npm run test:critical-storefront` + the critical visual checklist) and Architect approval.
- Additional member: `shared-composition-renderer.tsx:819` passes a preview-fake banner item lacking `alt` (TS2741×1) — same file, same preview/dead-preview path.

### RC3 — Discriminated `AdminResult` unions are read without narrowing, and several competing `AdminResult` shapes coexist (6 errors + wider drift)

- `app/admin/settings/page.tsx:244,277,297,330,355` read `result.denied` **before** checking `result.ok`, on `{ ok: true; data } | { ok: false; denied?: boolean }` (see `app/admin/quantity-settings-api.ts:32,51`). The correct narrowing is `if (!result.ok && result.denied)`.
- `app/admin/landing-pages/admin-landing-pages-screen.tsx:130` reads `pageResult.message` on the `ok: true` branch (`{ ok: true; data } | { ok: false; message; denied? }`, `admin-landing-pages-api.ts:57`) — the same anti-pattern with an inverted-looking expression.
- Structural drift behind it: at least **four** distinct local `AdminResult<T>` shapes exist — `lib/admin/admin-result.ts:10` (`state`/`data`/`status`/`message?`), `app/admin/menus/admin-menus-api.ts:35`, `app/admin/campaigns/admin-campaigns-api.ts:83`, `app/admin/landing-pages/admin-landing-pages-api.ts:57`, plus per-module variants whose `denied`/`message` members differ (`settings` modules). Consumers therefore cannot rely on one narrowing rule.
- `app/admin/landing-pages/admin-landing-pages-screen.tsx:431` (TS2322, same file) is a **genuine bug, not only a type error**: `refresh` was widened to `(opts?: { notice?: string; tone?: "error" | "success" })` but is still wired as `onClick={refresh}`; the click passes a `MouseEvent` as `opts`, so the notice is skipped and `setMessageTone` would receive a non-string. Minimal fix is `onClick={() => refresh()}`.

## 5. Named clusters requested by the task, resolved

| Named area | Verdict |
| --- | --- |
| `GridColumnDef.maxWidth` | Confirmed as the single highest-yield root: **96 errors** (RC1). |
| admin-languages | 11 errors: 8 × `maxWidth` (RC1) + 3 × `flex`/`pinned` excess properties (C3) on `GridColumnDef`. |
| admin-receipts | 8 errors: 7 × `maxWidth` (RC1) + 1 × actions column literal missing `width`/`minWidth`/`maxWidth` (TS2739). |
| `SectionDisplayConfig` vs `shared-composition-renderer` | Confirmed as RC2: **9 errors**, and a real drop of banner configuration on the Home path. |
| `i18n/middleware-locale` | Confirmed, **1 error** (C7): `middleware-locale.ts:7` imports `type Locale` from `./routing.ts`, but `routing.ts` imports `Locale` from `./locale.ts` without re-exporting it. Canonical owner is `lib/i18n/locale.ts` (imported directly that way in `app/admin/settings/page.tsx:6`). |

## 6. Genuine bugs vs stale tests/contracts vs product decisions

**Genuine bugs (beyond type noise)**

- `admin-landing-pages-screen.tsx:431` — `onClick={refresh}` passes a `MouseEvent` into a handler expecting an options object (RC3).
- `admin-landing-pages-screen.tsx:130` — `pageResult.ok ? sectionResult.message : pageResult.message` reads `message` off the wrong branch; the reported message is not the one that failed.
- C2 — Home silently drops `bannerImageUrl`/`bannerMediaAssetId`/`bannerHref` (and `heightPreset`) at parse time, so configured banners never render.
- `legacy-grid-bridge.ts:76` + `types.ts:102` — the type claims a required cap that the runtime never applies.

**Stale tests / stale contracts (must be updated, not "fixed" by weakening production types)**

- `app/admin/category-admin-form.test.ts:364` and `app/admin/category-admin-screen.tsx:40,42` reference `parseMetaKeywords` / `serializeMetaKeywords`, which `app/admin/category-admin-api.ts` no longer exports (TS2305 + TS2339, C6) — a rename/removal migration left both the consumer and its test behind.
- `design-system/app-data-grid/legacy-grid-bridge.test.ts:68-69` — fixture predates/contradicts the required `maxWidth`.
- `app/admin/admin-api.test.ts:114` — fixture lacks `AdminOrderDetail.consolidatedPackages` after that field became required (TS2345).
- `app/admin/landing-pages/admin-builder-ux-r8.guard.test.ts:69` — asserts `false` into a `true`-typed slot (TS2322).
- `app/admin/menus/admin-menus-api.ts:84,90,91` — calls `adminHeaders(boolean)` where `Record<string, string>` is required, `parseAdminProblemErrorCode(body)` with one argument where two are required, and `mapAdminErrorMessage(code, "دستوری فارسی")` where an `AdminErrorLocale` key is required: the menus module is stale against the canonical admin error-locale contract (C11).

**Product / visual decisions — STOP questions, no invented semantics**

- **C1 direction:** make `GridColumnDef.maxWidth` optional (type-only, zero runtime delta, matches the bridge's own precedent) **or** supply real per-column caps to 95 literals (≈95 visual decisions across ~14 locked admin grids, and any real cap changes grid behaviour). This audit recommends the optional path but does not decide it.
- **C3 `flex`/`pinned`:** `GridColumnDef` has neither; `AppDataGrid` columns are AG Grid `ColDef`s which support both. `language-list`, `catalog-units-screen` and `shipping-services-screen` therefore pass props that are **silently dropped** today by the bridge. Either add supported fields to the legacy type + bridge (visual delta) or complete those screens' migration to `ColDef`/`AppGridFilterColumnDef` (LOCK-SF-266/LOCK-SF-322 direction). Requires Architect decision.
- **C9 `actionSlots: 5`** (`app/admin/content-list.tsx:262`): `buildPinnedActionsColumnDef` only accepts `1 | 2 | 3`. A 5-action kebab layout needs the helper extended or the actions reduced — a UX/visual decision.

## 7. Ordered recovery plan

Sequenced by dependency; parallel only where the surfaces are independent.

1. **T1 — C1 (`GridColumnDef.maxWidth` → optional).** Owner `design-system/data-grid/types.ts`; plus bridge conditional pass-through (`legacy-grid-bridge.ts`) and a `clampWidth` fallback in `design-system/data-grid/DataGrid.tsx:532` so no new error appears. Expected reduction **−96** (155 → 59). No Home/PDP shared surface touched (admin grids only). Rollback: revert the single commit; single contract file.
2. **T2 — C5/C3-style narrowing + the two genuine admin-listing bugs.** `app/admin/settings/page.tsx` (`!result.ok && result.denied`, ×5) and `app/admin/landing-pages/admin-landing-pages-screen.tsx` (`pageResult.message` branch + `onClick={() => refresh()}`). Expected **−7** (59 → 52). Independent of T1; may run in parallel.
3. **T3 — C6 stale category meta-keyword API and its stale test.** Re-point or re-implement `parseMetaKeywords`/`serializeMetaKeywords` at the canonical owner and update the test. Expected **−4** (52 → 48).
4. **T4 — C7 i18n import.** One line: import `Locale` from `./locale.ts` (or add an explicit re-export in `routing.ts`). Expected **−1** (48 → 47). Must run `npm run test:i18n` (`middleware-locale.test.ts` exists).
5. **T5 — C2 Home banner configuration (STOP-gated).** Type + parser widening in `app/composition/composition-api.ts` with the renderer as the affected consumer; **requires Architect decision plus `npm run test:critical-storefront` and the critical visual review** because it can change locked Home output. Expected **−9**.
6. **T6 — C3 half-migration (`flex`/`pinned`).** STOP-gated on the direction (extend legacy type/bridge vs complete migration). Expected **−5**.
7. **T7 — C8/C9/C10/C11/C12 local clusters.** Small, independent, file-local fixes; split into per-surface repair tasks rather than one broad task (≈15 errors, all local). Includes the genuinely unsupported `actionSlots: 5` STOP question.
8. **T8 — final green typecheck and CI enforcement.** Only after T1–T7; then wire `npm run typecheck` into the enforced gate so the count cannot silently regress. Out of scope for this audit.

Constraint honoured throughout this plan: no `any`, no `@ts-ignore`, no `tsconfig` exclusion, no weakened types, no disabled gates, no baseline snapshot edits, no broad rewrites.

**Recommended FIRST bounded repair Task: `TB-FE-GRIDCOLUMNDEF-MAXWIDTH-OPTIONAL-001`** — scope: `design-system/data-grid/types.ts`, `design-system/app-data-grid/legacy-grid-bridge.ts`, `design-system/data-grid/DataGrid.tsx`, `design-system/app-data-grid/legacy-grid-bridge.test.ts`; expected **155 → 59** errors, type-only with zero runtime delta; validation `npm run typecheck`, `npm run test:grid`, `npm run test:architecture`; rollback = revert one commit. **Not executed by this task.**

## 8. Validation actually performed

- `cd src/frontend && npm run typecheck` — exit `2`, 155 errors (raw preserved).
- `node_modules/.bin/tsc --noEmit` — exit `1`, 155 errors (raw preserved).
- Diagnostic parsing/inventory generated mechanically from the raw compiler output; cluster sums verified equal to 155 and to the per-code totals.
- Source audit reads only (no writes) of the declarations and uses behind the top clusters, plus the named focused tests/guards: `legacy-grid-bridge.test.ts`, `category-admin-form.test.ts`, `admin-api.test.ts`, `admin-builder-ux-r8.guard.test.ts`, `admin-grid-migration.test.ts`, `frontend-source-size.guard.test.ts`.

**Tests deliberately NOT run and not claimed:** no `npm run test:*` suite was executed (frontend test suites), no `npm run test:critical-storefront`, no `npm run build`, no backend build/test, no AMSC wave. **Overall `typecheck` is NOT green** and is not declared green: 155 errors remain at `0420bb14a`. No test result is reported as PASS that was not run.

**No production mutation:** the working tree contains only this audit's added evidence/task files. `git status` shows no modified tracked source file. Pre-existing untracked files from earlier tasks (`docs/architecture/evidence/TB-CART-RETRY-LIVE-ACCEPTANCE-001/`, `TB-CART-*`, `TB-TMAR-USERPREFERENCE-AMSC-001-W3-R1/`, `TB-CHECKOUT-OPEN-UNPAID-DISTINCT-001/RESULT.bridge.txt`) were left untouched — not removed, stashed or rewritten. The running development services were not disrupted.
