---
name: tooba-frontend-feature
description: Lightweight, mandatory architecture-first workflow for adding or changing a Tooba frontend feature while preserving existing ownership, contracts and locked visuals. Invoke for any authorized src/frontend feature change or bug fix.
---

# Tooba Frontend Feature (lightweight)

Scope: `src/frontend/**`. This skill implements, but NEVER overrides, `docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md`, `AGENTS.md`, accepted Shopeiva visual locks, `ARCH-FE-FREEZE-001`, and the active task. No automatic new task or retroactive certification.

## 1 — Preflight (before coding)
- Confirm the active task permits frontend changes. If frozen or unauthorized: STOP.
- Read the architecture standard and relevant visual/feature contracts.
- Trace the real route/page, owner, components, API client, state, types, reused design-system elements, and focused tests.
- Identify the smallest implementation surface; preserve public URL/API and visual behavior. Do not invent a parallel abstraction or speculative redesign.

## 2 — Implement one cohesive change
- Keep route/page composition, feature behavior, typed API adapter/state, and shared UI responsibilities separated according to existing local conventions.
- Reuse established clients, `design-system/`, `AppDataGrid`, forms and tokens. Prevent circular/cross-feature internal imports and new client/server boundary violations.
- Address loading/empty/error, accessibility and FA/EN/RTL where relevant. No hardcoded fake prices, stock, discounts or ratings.
- If the architecture fix needs broader changes than the requested feature: fix only approved scope and record the debt; do not silently enlarge the task.

## 3 — Proof and handoff
- Run relevant focused tests and `cd src/frontend && npm run typecheck` when feasible. Never report an unrun test as PASS.
- For Home/PDP/shared storefront visual changes, ALWAYS follow `docs/visual-baselines/CRITICAL-STOREFRONT-VISUAL-REVIEW.md` and run `npm run test:critical-storefront`; visual acceptance is separate.
- Report exact touched files, owner/boundary, contract/visual delta, tests actually run, remaining debt and STOP decision.
- No automatic AMSC waves, mass folder moves, global baseline changes, or certification claims merely because code compiles.
