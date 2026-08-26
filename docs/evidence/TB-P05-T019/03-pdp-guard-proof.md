# 03 — PDP Guard Proof

- Contract: `docs/visual-baselines/PDP-FIDELITY-CONTRACT.md`
- Structural assertions: `src/frontend/app/storefront/pdp-structure.guard.test.ts`
- Sticky strip: `data-testid="pdp-sticky-tabs"` + `sticky top-0`
- Distinct tabs: intro/full/specs/reviews/qa/bulk via `pdp-tab-*` and dedicated bodies
- Other sellers / related: `pdp-other-sellers`, `pdp-related`
- Forbidden flatten markers checked (`TabContent`, `generic-tab-panel`)
- Product.Price / Product.Stock authority markers forbidden
- Command: `npm run test:pdp-guard`
- Non-visual hooks only (no redesign)
