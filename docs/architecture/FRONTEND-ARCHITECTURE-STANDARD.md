# Tooba Frontend Architecture Standard

Status: ACTIVE — prospective feature-development contract
Scope: `src/frontend/**`
Authority: applies to every new feature and every changed frontend feature; does not retroactively certify existing pages.
Priority: existing accepted visual locks, `ARCH-FE-FREEZE-001`, task scope, and approved behavior/API contracts remain binding. Conflict => STOP and request Architect decision; never silently bypass a lock.

## Required before writing frontend code

1. Read root `AGENTS.md`, this standard, and relevant feature/visual contracts. Map the existing route/page, component tree, data access, state, types, tests, and shared design-system facilities.
2. Find the current owner of the capability. Extend the established pattern within that owner; do not create a parallel client, feature framework, state store, grid, form, or design system.
3. State the smallest intended change, its owning files and boundaries, current behavior/API to preserve, expected tests and visual impact. If ownership cannot be decided without a broad redesign, STOP.
4. Respect `ARCH-FE-FREEZE-001`: do not change frontend where a task or lock forbids it. A backend fix does not authorize frontend edits.

## Implementation rules — mandatory for each new feature

- **Single owner:** route/page composes the user flow; feature code owns its use-case interactions; reusable presentation belongs to the established design system; API contracts/network access live in established typed clients/adapters. Follow existing repo conventions rather than impose a new folder taxonomy.
- **Dependency direction:** feature/page may depend on shared UI, utility and API boundaries; shared UI must not depend on a specific page, feature, route, authenticated session or domain client. No circular feature imports, cross-feature internal imports, or deep imports bypassing public capability surfaces. Explicitly document any unavoidable existing exception; do not expand it.
- **Rendering versus behavior:** avoid embedding network calls, business policy, mutation sequencing, or complex data transformations in pure presentational components. Preserve appropriate Next.js server/client boundaries; never expose secrets or server-only dependencies to client bundles.
- **Type safety:** use explicit domain/API types; validate/normalize untrusted boundaries where needed; do not introduce `any`, unsafe assertions or duplicate conflicting DTOs to make a change compile.
- **Data and state:** use the existing API adapter, error handling, loading/empty/failure flows, cache and URL/query conventions. Preserve server authority for pricing, inventory, permissions and other business decisions. Avoid duplicated sources of truth and redundant request loops.
- **Reusable UI:** reuse existing `design-system/` components, `AppDataGrid`, established form/workspace patterns and design tokens before creating a new variant. Do not duplicate a shared component to solve one page issue.
- **Maintainability:** keep components and hooks cohesive; extract when a genuine independent responsibility emerges, not to satisfy arbitrary line limits. No speculative abstraction, invented architecture framework or unrelated refactor.
- **Compatibility:** preserve public routes, query params, payload shapes, accessibility, responsiveness, locale-aware formatting, directionality (RTL/LTR), and all configured languages where relevant, existing feature flows and approved layouts. Do not invent test data, promotional prices, ratings or availability.

## Change workflow (a feature task is NOT an automatic redesign)

1. **Precheck:** identify owner, dependencies, API, state and visual restrictions; state risks.
2. **Implement:** smallest coherent change following the existing architecture; preserve behavior outside requested scope.
3. **Verify:** run `npm run typecheck` when feasible, focused tests for changed capability, and relevant build/checks where appropriate; distinguish not-run from passed. Add focused regression tests for meaningful changed behavior.
4. **Critical storefront:** for Home, PDP and shared storefront visuals, follow `docs/visual-baselines/CRITICAL-STOREFRONT-VISUAL-REVIEW.md` and run `cd src/frontend && npm run test:critical-storefront`; verify desktop and 390x844 mobile fidelity when environment supports captures. Functional PASS is not Visual ACCEPT.
5. **Report:** changed boundaries/files, reasoning for reusable code, tests actually run, behavior/API/visual deltas, and outstanding architecture debt. Do not claim certification or visual approval without evidence.

## Stop / escalation conditions

- Any requested feature violates a locked visual/functional contract, frontend freeze, security boundary or task authorization.
- A fix requires wide refactoring or a new architecture pattern; propose a separate bounded task instead of silently broadening scope.
- Type/API contract behavior is uncertain or cannot be validated safely.
- Existing architecture debt is unrelated to the requested feature: record it but do not fix it opportunistically.

## Acceptance checklist for a changed frontend feature

- [ ] One clear feature owner, no parallel infrastructure
- [ ] No new forbidden dependencies or circular imports
- [ ] Page / feature / data-access / reusable-UI responsibilities respected
- [ ] Existing API contracts and user behavior preserved unless change explicitly approved
- [ ] Loading, empty, error and relevant accessibility/i18n states addressed
- [ ] Focused tests and type checks reported accurately
- [ ] Shopeiva visual and freeze checks honored when applicable
- [ ] No unrelated refactor, speculative abstraction, or unapproved production change

This is a prospective development standard, NOT a claim that every pre-existing frontend file already complies. Certification of an entire page/feature requires separate evidence and Architect acceptance.
