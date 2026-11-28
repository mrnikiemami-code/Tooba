# Proposal — offer-multicurrency-pricing-296-r11

## Why
The Offer Phase 1 workspace records exactly one price in the store default currency (Pricing currently rejects any currency other than the store default). UAT requires real multi-currency authored prices per offer, chosen from the authoritative system currency reference, before the discount engine starts. The 1d777e2e UAT fixes (market column, duplicated variant labels, excess copy, header borders, edit-mode inventory provisioning) are already on the branch and must be preserved and verified.

## What Changes
- MC-01 System currency reference: Pricing exposes the authoritative active system currencies (code, names, decimal places) and rejects unsupported currencies.
- MC-02 Multi-currency authored prices: a seller/admin records and updates one base price per offer for each active system currency; the store default currency remains the primary list/checkout price.
- MC-03 IRR precision: IRR authored prices are integer amounts server-side and in the UI.
- MC-04 Registry locale/RTL: all new labels use the shared message catalogs (fa/en/ru) and the shared registry direction.
- MC-05 UAT operational corrections: no market column, no duplicated variant labels, no excess copy, working inventory-position provisioning, contained header borders (verify the existing fixes).
- MC-06 Regression: existing channel, create/edit, inventory, history and single-currency behavior remain.

## Capabilities
### New Capabilities
- `offer-pricing-multicurrency`: Pricing-owned authoritative currency reference and multi-currency authored offer prices, surfaced through the shared Offer workspace.

### Modified Capabilities
- None declared here: existing Offer workspace/price contracts remain authoritative; this change extends Pricing's seller pricing boundary without a parallel path.

## Impact
Pricing (Domain/Contracts/Infrastructure), Offer (Application/Contracts/Endpoints/read model), shared Offer workspace and admin/seller adapters, Host control-plane currency configuration, Development seed/migration only if required. No discount engine, no new Host/DB, no unrelated modules.

## Authority
Owner UAT closure goal TB-296-PRICING-FINAL-UAT-CLOSURE (branch `opencode/iso-agents-002`, base `1d777e2e`). Architecture references: `docs/architecture/08-pricing-market-currency.md` (Locale != Market != Currency; authored price vs FX; fail closed on unknown currency), `AGENTS.md`, `docs/opencode/ARCHITECT-DELIVERY-METHOD.md`, `docs/opencode/ARCHITECT-QUALITY-GATE.md`. Worker PASS != Architect ACCEPT; no self-authorization.
