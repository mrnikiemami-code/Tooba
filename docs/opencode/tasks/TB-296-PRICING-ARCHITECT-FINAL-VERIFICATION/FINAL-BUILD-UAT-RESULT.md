# TB-296 OFFER FINAL BUILD UAT — RESULT

Status: **BLOCKED — FINAL ACCEPTANCE NOT ISSUED.** No substantive product change.

## Clean rebuild / identity
- HEAD `00cc92b35a0ad49083f8c1269841f67e63ce5c12`; both Backend and Frontend clean-built and served on existing Host 5088 / Next 3000 / `tooba_alpha`.
- Host DLL SHA-256 `e0cbff3f316a7fd907271a8559de430787a7a738d1647068167ca2651e0f9b02`; Next BUILD_ID `kHFWSFop7lmAufiLTlG3U`; typecheck 0. No migration/new Host/DB.

## Canonical absence fixed and verified
- Rebuilt Host now returns **HTTP 204, empty body** for a confirmed free tuple: `GET /v1/admin/offers/active-listing?sellerPartyId=…&catalogVariantId=…&channel=Marketplace`. (The prior 200-empty was the stale-DLL runtime, not current source.)

## Connected owner UAT (PASS)
- Fresh New empty; repeated New and browser Back stay empty.
- **Multi-currency create PASS**: free tuple → 204 absent → Save Draft → staged IRR 100000 / USD 13.25 / EUR 20.5 in one form → one Offer → reload shows three Active prices with three independent revisions and exact amounts (integer IRR; store primary authority preserved); exact cleanup 0.
- Duplicate existing dismiss/reopen and localized navigation to the exact existing offer PASS (no create write).
- Affected tests **40/40** (mount, create-multicurrency, draft-recovery, quantity-input, effective-scope), i18n 22/22, model 22/22; Offer 144/144, Inventory 53/53 (new DLL); architecture **27 baseline / 0 Offer-owned**; OpenSpec strict valid; both gate self-tests PASS. Prior checkout/Tax/payment/inventory evidence preserved.

## Remaining gaps (not PASS)
1. **Connected locale-recovery case**: after switching the create page to English, the harness could not read `offer-price-input` on the pricing tab (timeout). The draft-recovery logic test passes and prior locale evidence is preserved, but the exact connected assertion was not captured.
2. **Connected lookup-error case**: aborting `active-listing` did not surface the expected `offer-message-error` within 15s in the harness; the adapter source still fail-closes on a thrown lookup, but the exact UI witness was not captured.
3. **Readonly closure**: seller duplicate dismiss/reopen/navigation PASS; admin duplicate case failed at `product ambiguous` under the new internal searchable selector (harness selector, 0 writes). Warehouse case is not present in this run’s saved case list.
4. **Seller positive multicurrency create NOT_RUN**: authorized seller A still has 0 free published tuples; seller B has free tuples but lacks `product.create`; no permission escalation and no new fixture-development cycle per recovery SOT.
5. Numeric/nullable/Catalog precision/Persian digits, RTL row actions, currency deletion, internal warehouse search and shipping responsibility/type are covered by the passing quantity-input/mount/i18n tests and source; not all were independently repeated in a live browser in this run.

## Evidence
- `final-build-evidence/` (affected/offer/inventory/i18n/model/architecture/openspec/self-test logs, build identity).
- `definitive-evidence/definitive-uat.json` (fresh/repeat/multicurrency/duplicate PASS; locale/lookup harness failures with exact errors).
- `owner-uat-closure-evidence/fcd6c5d8-…/readonly-closure.json` (seller duplicate PASS, admin selector failure, 0 writes).
- No created-Offer/cart residue (0).
