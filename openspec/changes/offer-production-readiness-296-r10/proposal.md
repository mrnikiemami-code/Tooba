# Proposal — offer-production-readiness-296-r10

## Why
R9-R1 improved the existing Offer workspace but architect acceptance remains withheld: channel selection, pre-create inventory, history, atomic concurrency, canonical locale and Offer-owned folder boundaries remain incomplete. R10 is the owner-authorized bounded production-readiness task. Preserve the R8 accepted layout and working R9/R9-R1 behavior.

## What Changes
- R10-01: Channel.
- R10-02: Create workspace.
- R10-03: Inventory provisioning.
- R10-04: Shared persistence.
- R10-05: Atomic concurrency.
- R10-06: History.
- R10-07: Unit and conditions.
- R10-08: Price and shipping.
- R10-09: Central locale.
- R10-10: Frontend architecture.
- R10-11: Backend and Host.
- R10-12: UX and feedback.
- R10-13: Performance and regression.
- R10-14: Evidence and closure.
- Implement authorized owning-module contracts and connect both role-specific surfaces; no invented policy.
- Close all in-scope architecture violations under AQ-01..AQ-08; distinguish unrelated baseline debt.

## Capabilities
### New Capabilities
- `offer-production-readiness`: connected single-offer creation, editing, recovery, domain-owned data and scoped architecture acceptance.

### Modified Capabilities
- None declared here: this change records the R10 acceptance overlay; existing Offer workspace specs and contracts remain authoritative where compatible.

## Impact
Offer, Catalog, Inventory, Pricing, Fulfillment and Localization owning contracts as applicable; shared frontend locale and Offer feature boundaries; admin/seller workspace routes and grids; isolated verification only. No redesign, whole-project cleanup, discount funding, settlement, bulk grid, Stage3 policy, live migration or TB-297 execution.

## Authority
Immutable task package: `docs/opencode/tasks/TB-296-R10-OFFER-PRODUCTION-READINESS/TASK.md`, `REQUIREMENTS.json`, `ACCEPTANCE-MATRIX.json`. All 14 requirements and 42 cases remain mandatory; this OpenSpec change does not replace or weaken them.
