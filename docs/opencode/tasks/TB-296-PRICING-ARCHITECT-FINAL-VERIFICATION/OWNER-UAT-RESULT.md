# TB-296 OWNER UAT FINAL CLOSURE — RESULT

Status: **BLOCKED — FINAL ACCEPTANCE NOT ISSUED.** No substantive product change was made; only a mechanical CS1591 XML-doc fix to the architect's migration was required to build. Architect owns the remaining corrections.

## Synchronization / build
- Branch `opencode/iso-agents-002`; HEAD `a2c7fbe504178d22191889dd3c75fd8c3a3f0e38` (fast-forwarded; the pre-existing local `.opencode/opencode.json` change is untouched/not committed).
- Applied additive migration `20261128120000_OfferPriceRetirementFact` to existing `tooba_alpha` (column `pricing.offer_price_changes.retired boolean not null default false`). The migration itself failed to compile with CS1591; added only XML doc comments to Up/Down (mechanical v1.5.2 fix, no semantic change).
- Rebuilt Backend and Frontend; Host 5088 (DLL SHA-256 `7500f9d6…`) and Next 3000 (BUILD_ID `qap1J2RncEuNS1CCMIhKw`) running; no new Host/DB.

## Connected verification — executed PASS
- **Pricing retirement (HTTP/SQL, 5/5)**: retire EUR keeps EUR amount 20.5 in a `Retired` authoring row + durable fact (`retired=true`), preserves USD 13.25 and IRR; replay one fact; changed intent conflict; concurrent retire-vs-set exactly one winner; retiring the primary IRR makes the offer amount null and a real storefront cart add is refused (purchase prevention); exact fixture cleanup.
- **Inventory ownership/reason (4/4)**: first physical initialization without a manual reason succeeds (owner records the system initial-stock reason); subsequent write without reason → `inventory.adjustment.reason_required`; a previously initialized position returned to zero still requires a reason; cross-seller write → 403/404 with no mutation.
- **Catalog unit root cause**: product `01a05387-fbd0-7000-acd3-4382ce92c773` («گوشی هوشمند سری 1») references Catalog unit **kg** (Mass) with `quantity_decimal_places=2`; other phone products use `pcs`/0. This is Catalog seed data — Offer/Inventory only project the Catalog unit; not an Offer projection bug.
- **UI (Playwright, 5/5)**: retirement remove → localized confirm → EUR row Retired while USD stays Active; invalid price/validation surfaces in the shared MessageBox with no legacy `offer-save-error` banner; warehouse control filters eligible locations by name/code; real panel registry fa-IR RTL / en-US / ru-RU with localized pricing labels; 390×844 and desktop no horizontal overflow and the long variant label is not clipped.
- Prior valid evidence preserved (legacy IRR normalization 2310→0; I did not repeat proven backend market/concurrency cases).

## Blocking findings (architect-owned)
1. **Inventory architecture guard fails**: `No_raw_inventory_machine_code_literal_outside_the_contract_owner` — raw literal `"inventory.initial_stock"` in `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.SellerWrite.cs` (must live in the Inventory contract owner). Inventory suite 52/53.
2. **Offer mount regression suite 5 failures** (stale test harness vs the intentional UAT-E1/D2 behavior; no product change made):
   - IRR decimals → missing `offer-save-error` (now MessageBox)
   - inventory below reserved/reason → missing `offer-save-error` (now MessageBox)
   - existing active listing → missing `offer-message-duplicate`
   - Persian amount normalization → missing `offer-save-error`
   - partial create recoverability → missing `offer-message-error`
3. **Seller create still blocked**: seller B has free published variants but lacks `product.create` (HTTP 403 at GlobalWithinOwner); seller A has `product.create` but no free published variant. No grant escalation and no synthetic Catalog variant was created (no safe variant cleanup owner path); exact blocker per audit.
4. **Isolated integer-IRR checkout with secondary currency NOT_RUN**: a synthetic offer has no Tax classification; Tax exposes only `ITaxDevelopmentSeedGateway`, no owner HTTP/command path; direct cross-schema SQL and mutating demo offers are forbidden.
5. Duplicate detection dismiss/reopen/localized-navigation and unavailable-lookup were not independently re-proven in this run (mount duplicate test fails as above).

## Quality gates
- Offer 144/144, Pricing 20/20, Inventory 52/53; frontend i18n PASS, model PASS, mount 24 pass / 5 fail, typecheck 0.
- Architecture guards: 27 pre-existing size violations, 0 Offer-owned; exact baseline identities otherwise; OpenSpec strict valid; acceptance-gate self-test (11 negative controls) and architect-review-gate self-test (14) PASS.
- AQ-01..AQ-08: no new architecture violation; the Inventory raw literal guard above is an in-scope gate failure; seller/duplicate/checkout proofs remain as listed.

## Identity / evidence
- Host DLL `7500f9d64a477f3339f6e62c8d54d75955aaac463382c88d05e216f0a6f0298f`; Next `qap1J2RncEuNS1CCMIhKw`; DB `tooba_alpha`; P1C/UAT fixtures cleaned (0 offers, 0 carts).
- Evidence: `owner-uat-evidence/owner-uat-http.json`, `owner-uat-browser.json`, `offer-tests.log`, `pricing-tests.log`, `inventory-tests.log`, `i18n.log`, `model.log`, `mount.log`, `architecture.log`, `openspec-strict.log`, `acceptance-selftest.log`, `review-selftest.log`, `identity.json`.
