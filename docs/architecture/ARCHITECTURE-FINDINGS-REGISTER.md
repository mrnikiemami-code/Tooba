# Tooba — Architecture Findings & Risk Register

Status: ACTIVE | Authority: durable inventory of **deferred** defects, architectural debt, harmful constraints, and audit discoveries.
Not a bug tracker replacement, task authorization, acceptance, or permission to change locked surfaces.
Owner: Architect review. All changes are versioned on main; keep findings after repair with status/resolution evidence.

## Operating contract

- Record an audit finding when it is important but **not** within the current task's authorized repair scope. Record serious regressions/risks even when the triggering change is ACCEPTED.
- Keep each finding uniquely numbered; update it rather than duplicating it. Record exact surface/paths, symptom, potential harm, evidence commit, confidence (**CODE_VERIFIED**, **WORKER_REPORTED**, or **NEEDS_REPRODUCTION**), severity, status, owner, next safe bounded action, dependencies/tests, and closure evidence.
- Statuses: `OPEN_DEFERRED`, `READY_FOR_APPROVED_TASK`, `IN_PROGRESS`, `RESOLVED_VERIFIED`, `ACCEPTED_RISK`, `DISPROVEN`. New tasks require separate Architect authorization; registry items never auto-trigger execution or AMSC.
- Distinguish verified facts from Worker-only test/runtime counts. Never silently reset baselines or weaken existing architecture, localization, identity, or visual guards to close an item.
- Before touching an audited screen, check relevant items here; include affected IDs in task/report. Fix the requested issue first, then address approved debt only within scope.
- Plans below are **proposed**, not scheduled or authorized. Closure requires a real code diff, focused evidence, and Architect acceptance. If an issue is known but not reproduced, mark uncertainty, not PASS.

## Deferred findings

### TF-001 — Admin order detail: oversized mixed-responsibility screen
- **Status / priority:** OPEN_DEFERRED / P2 (maintainability).
- **Surface:** `src/frontend/app/admin/admin-order-detail-screen.tsx`, route `/admin/orders/{checkoutId}`.
- **Evidence:** `5aba09f810c5b0d8222f95943685b14df8533b40` (CODE_VERIFIED: 1,093 lines; supply/history/reservation and other presentation/logic co-located). Worker describes broader header/finance/notes organization; those detailed responsibility labels have not all been independently certified.
- **Harm:** hard-to-reason-about changes, wider regression surface, pressure from size guard.
- **Proposed bounded plan:** inventory component/data dependencies and behavior first; extract one cohesive self-contained section at a time using existing admin patterns; keep route/orchestration and public props stable. Do not split by line count alone. Prove actions, countdown, refresh, responsive layout and typecheck for touched paths after each approved slice.
- **Acceptance:** reduced coupling with same rendered behavior and no new cross-feature imports; relevant focused/visual checks. No task currently authorized.

### TF-002 — Admin order detail: fixed two-language and DOM-derived locale
- **Status / priority:** OPEN_DEFERRED / P1 (multilingual correctness).
- **Surface:** same screen; `reservationLocale()`, operational history labels, `OrderSupplyCard`.
- **Evidence:** `5aba09f8` (CODE_VERIFIED: `reservationLocale()` consumers; `reservationLocale() === "en" ? actorDisplayEn : actorDisplayFa`; `adminSupplyMessageFa`; `toLocaleString("fa-IR")`). The October 12 card-reorder commit **did not create** this debt.
- **Harm:** locale context may diverge from DOM/path and other configured locales may receive the wrong text, number formatting or direction.
- **Proposed bounded plan:** trace central locale context, server/API multilingual contract and fallback first; remove component-local DOM parsing and hard-coded language branches using **existing** i18n mechanisms. Do not invent translations or assume the supported locale list. Separate frontend-presentation changes from any needed server contract work; escalate before contract changes.
- **Acceptance:** focused tests for at least default/alternate configured locale plus fallback and direction/formatting; preserve functionality, countdown and layout. No blanket claim all languages are verified.

### TF-003 — FE-SIZE shrink-only line budget pressures unreadable JSX
- **Status / priority:** OPEN_DEFERRED / P2 (developer tooling quality).
- **Surface:** frontend source-size architecture guard and `admin-order-detail-screen.tsx`.
- **Evidence:** `5aba09f8` (CODE_VERIFIED: component moved as one physical line and file fell from 1,096 to 1,093 lines). **WORKER_REPORTED**: multiline placement failed `OVERSIZED_GROWTH` due to shrink-only baseline. Exact guard implementation and baseline criteria have not been independently reproduced for this entry.
- **Harm:** code formatting becomes a workaround for an architectural metric; poor readability and meaningless churn.
- **Proposed bounded plan:** reproduce guard on clean HEAD; inspect baseline logic; design a narrowly scoped rule that prevents new responsibilities while allowing formatting-only edits. Preserve meaningful size/cohesion protection; do not globally relax caps or regenerate baselines.
- **Acceptance:** positive multiline-formatting proof and negative genuine-growth proof against the changed guard; focused existing architecture checks.

### TF-004 — Admin grid/type contract drift
- **Status / priority:** OPEN_DEFERRED / P1 (type safety and feature reliability).
- **Surface:** `src/frontend/app/admin/admin-order-detail-screen.tsx`, `GridColumnDef.maxWidth`, affected order-grid tests.
- **Evidence:** **WORKER_REPORTED**, `5aba09f8` task: nine `maxWidth` TypeScript errors in changed screen, 155 repo-wide pre-existing errors; two order-related tests reportedly fail over `/maxWidth:/` and non-link reference in `admin-order-operations.test.ts`. The full set and baseline sameness were not independently rerun here.
- **Harm:** type errors obscure new regressions and divergent grid API assumptions weaken design-system boundaries.
- **Proposed bounded plan:** inspect canonical grid column type and actual call sites, determine whether consumer or design-system contract is wrong; repair the owner and focused tests without applying `any` or bypassing typecheck. Separate unrelated repo-wide TS failures into independent issues only with evidence.
- **Acceptance:** touched area clean under `tsc` and focused grid/order tests; capture baseline difference accurately.

### TF-005 — Legacy Admin Host path referenced by obsolete test
- **Status / priority:** OPEN_DEFERRED / P2 (test reliability).
- **Surface:** `src/frontend/app/admin/admin-t007-history.test.ts` (reported); historical `src/backend/Host/Tooba.Host/Admin/AdminPanelComposer.cs`.
- **Evidence:** **WORKER_REPORTED** `ENOENT` after Host evacuation, reported during `5aba09f8` review. Exact current test path and new canonical owner still require verification.
- **Harm:** false-negative test suite and unreliable architecture feedback.
- **Proposed bounded plan:** locate new owning module/file via git history, update test assertions to canonical boundary (not merely an old path replacement); retain negative proof for prohibited Host ownership. Do not recreate legacy Host code to satisfy stale tests.
- **Acceptance:** test passes with both required positive and negative ownership assertions.

### TF-006 — Multiple tooltip owners in admin order grid
- **Status / priority:** OPEN_DEFERRED / P2 (predictability and UX).
- **Surface:** `app-grid-row-actions.tsx` title, `AdminOrderOperationsMenu` CSS tooltip, and `AppDataGrid` `tooltipValueGetter`/`tooltipShowMode="whenTruncated"`.
- **Evidence:** WORKER_REPORTED during October 12 order-grid tooltip repair; consumer ownership has not been independently audited.
- **Harm:** duplicate/overlapping tooltips, mouse-position-dependent behavior.
- **Proposed bounded plan:** inventory all three paths and consumers; select the canonical owner per semantic trigger and retire redundant rendering without changing accessible names. Test hover, focus, RTL/LTR and truncation.
- **Acceptance:** one clear tooltip source per trigger; focused grid/design-system regression and browser proof. Not authorized for immediate repair.

### TF-007 — Shared portal tooltip accessibility and dialog/top-layer boundary
- **Status / priority:** OPEN_DEFERRED / P2 (accessibility and future component integration).
- **Surface:** `src/frontend/design-system/primitives/overlays.tsx`.
- **Evidence:** WORKER_REPORTED, uncommitted +88/−5 tooltip portal change: tooltip not associated with trigger by `aria-describedby`; default `document.body` portal may appear beneath native `<dialog>` top layer.
- **Harm:** inaccessible descriptions for assistive technology; future dialog usage may hide tooltip.
- **Proposed bounded plan:** review established overlay primitives and real consumers; add accessible relation and only necessary configurable portal host if an authorized use case requires it, avoiding a parallel overlay system. Check focus, hover, scroll, resize, unmount and stacking boundaries.
- **Acceptance:** accessible trigger/tooltip relationship and tested dialog case where applicable; regression on current admin grid and showcase.

### TF-008 — Residual admin-grid TypeScript and header ownership defects
- **Status / priority:** OPEN_DEFERRED / P2 (type/build and UI ownership).
- **Surface:** `admin-screens.tsx` (reported missing `ReactNode` import at ~line 237); actions/checkbox grid columns reportedly lack app-owned header.
- **Evidence:** WORKER_REPORTED from October 12 tooltip audit; no independent TypeScript run or contract inspection for this entry.
- **Harm:** persistent TS2304 noise and incomplete header contract usage.
- **Proposed bounded plan:** reproduce on current HEAD, inspect canonical column/header interfaces, repair owning consumers with focused tests; do not weaken type checks or modify unrelated columns.
- **Acceptance:** affected TS errors and headers fixed, no new architecture or grid regression.

### TF-009 — Storefront locale inventory is hard-coded to two languages
- **Status / priority:** OPEN_DEFERRED / P1 (multilingual architecture).
- **Surface:** `src/frontend/lib/i18n/locale.ts`; storefront locale routing, UI formatting and fallback.
- **Evidence:** CODE_VERIFIED against `e8abc83787b7f16bed9e3c89d741346b4374f84e`: `LOCALES = ["fa", "en"] as const`, `isLocale`, `parseLocale`, `langForLocale`, `dirForLocale`, and `acceptLanguageForLocale` encode FA/EN only. This predates the shipping language fix. Related to TF-002 (admin) but different owner/surface; do not duplicate an admin issue.
- **Harm:** centrally configured additional languages cannot flow end-to-end through storefront; URL, headers, direction, fallback and formatting may disagree.
- **Proposed plan (NOT authorized):** first map Backend language registry and frontend route/locale ownership, enabled locales and fallback; design one canonical locale resolution contract; migrate existing frontend consumers in small bounded batches while preserving `/fa` and `/en`, SEO, RTL/LTR and Shopeiva locks. No invented language list or broad rewrite.
- **Acceptance:** configured-language and fallback integration tests, correct Accept-Language/URL/formatting/direction, no regression to existing locales. Requires explicit task authorization.

### TF-010 — Server error display inferred from script instead of localized error contract
- **Status / priority:** OPEN_DEFERRED / P1 (user-facing error correctness).
- **Surface:** `src/frontend/lib/i18n/displayable-server-detail.ts` and storefront error adapters.
- **Evidence:** CODE_VERIFIED against `e8abc8378`: `isDisplayableServerDetail` tests Persian vs Latin Unicode script and rejects broad technical-keyword patterns. Worker reports other cart/PDP/product-card error mappers default to Persian (not independently traced here).
- **Harm:** Latin-script non-English locales and mixed-script/technical-safe messages may be misclassified; user may see wrong language or generic fallback despite valid server localization.
- **Proposed plan (NOT authorized):** inspect canonical error code/catalog, locale resolution and fallback; select display messages by stable error code and centralized localization with server authority; retain safe sanitization independent of script; add tests for all configured locales, neutral/mixed scripts and unknown codes. Do not bypass security sanitization.
- **Acceptance:** localized errors chosen by contract, not Unicode script guess; focused negative/fallback tests and preserved HTTP error handling.

### TF-011 — Shipping payload fixes language to fa
- **Status / priority:** OPEN_DEFERRED / P1 (shipping multilingual correctness).
- **Surface:** `src/frontend/app/storefront/storefront-shipping-api.ts`.
- **Evidence:** CODE_VERIFIED at `e8abc8378`: request body uses `language: "fa"` despite new Accept-Language propagation.
- **Harm:** active locale and submitted shipping language can differ, even with correct header.
- **Proposed plan (NOT authorized):** verify backend DTO language semantics (language vs locale, required support), use canonical current locale in payload through existing adapter and test request under multiple configured locales; avoid hardcoding any two-language assumption.
- **Acceptance:** body/header carry consistent language per contract and tests cover fallback.

### TF-012 — Customer-facing shared storefront footer exposes internal implementation jargon
- **Status / priority:** OPEN_DEFERRED / P2 (product copy / visual lock).
- **Surface:** shared Shopeiva footer, exact owner to locate during an authorized task.
- **Evidence:** WORKER_REPORTED during `e8abc8378`: footer displays `ویترین Tooba روی قالب خریداری‌شدهٔ Shopeiva · دادهٔ زنده Catalog/Offer`. Not independently reproduced. Shared storefront and visual lock require caution.
- **Harm:** confusing internal jargon visible to customers and possible visual regression if corrected without review.
- **Proposed plan (NOT authorized):** locate shared footer owner, verify live copy and approved visual baseline, replace only copy with product-facing localized resources under existing design tokens, run mandatory critical storefront/visual tests.
- **Acceptance:** customer copy without internal technology names and unchanged accepted Home/PDP visuals.

## Related observations (not separate authorized tasks)

- **Admin order operations:** earlier assessment noted that pending-payment plus expired reservation may expose only cancel on the admin operations menu. Customer-facing checkout-scoped retry was subsequently fixed and live-accepted (`1ae919f2`, `c1dab07c`), but this **does not** establish that an admin operation should exist or is authorized. Treat as `NEEDS_REPRODUCTION` before filing an implementation task.
- **Frontend system-wide multilingual debt:** the cart and order surfaces contain older `fa/en` branches. This register tracks the verified order-detail example; broader scan needs a separate authorized read-only audit.
- **Global test baseline:** test-suite failure counts are Worker reports until independently reproduced; no global regeneration or self-authorized cleanup.

## Review log

- 2026-10-12 — Explicit user-requested registration of storefront i18n and customer-copy findings TF-009..012 after shipping bugfix `e8abc8378`; no production repair authorized.

- 2026-10-12 — Recorded tooltip ownership, accessibility/top-layer and admin-grid type/header risks (TF-006..008) from Cursor's still-uncommitted tooltip report; status WORKER_REPORTED pending code review.

- 2026-10-12 — Created from audit findings surfaced during `5aba09f8` (reservation-cycle card moved to page bottom; movement itself accepted). Initial findings deferred; no repairs authorized or run.
