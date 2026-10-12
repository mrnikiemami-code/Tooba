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

## Related observations (not separate authorized tasks)

- **Admin order operations:** earlier assessment noted that pending-payment plus expired reservation may expose only cancel on the admin operations menu. Customer-facing checkout-scoped retry was subsequently fixed and live-accepted (`1ae919f2`, `c1dab07c`), but this **does not** establish that an admin operation should exist or is authorized. Treat as `NEEDS_REPRODUCTION` before filing an implementation task.
- **Frontend system-wide multilingual debt:** the cart and order surfaces contain older `fa/en` branches. This register tracks the verified order-detail example; broader scan needs a separate authorized read-only audit.
- **Global test baseline:** test-suite failure counts are Worker reports until independently reproduced; no global regeneration or self-authorized cleanup.

## Review log

- 2026-10-12 — Created from audit findings surfaced during `5aba09f8` (reservation-cycle card moved to page bottom; movement itself accepted). Initial findings deferred; no repairs authorized or run.
