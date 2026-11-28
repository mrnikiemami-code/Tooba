# TB-296 OFFER DEFINITIVE CLOSURE — RESULT

Status: **BLOCKED — FINAL ACCEPTANCE NOT ISSUED.** No substantive product change; only test-harness evidence added.

## Sync / build
- Fast-forwarded to `41f70f1dcd4396795e5559ef00e960364aa6b52b` (includes the stated `79e0dae08`); OWNER-UAT-NEW-FORM-MULTICURRENCY.md read. Existing Host 5088 DLL `a4233f700a17ee68602e26cec47915b29fe356bf4a8f58e3daba72e90bcef5bf` unchanged; Frontend rebuilt and served on Next 3000, BUILD_ID `jsVpndJbdJPiok3_-k5_8`; typecheck 0. No migration/new Host/DB.

## Passed
- **Fresh form**: `/fa/admin/offers` → New is empty (seller/product/variant placeholders, no SKU, 0 price/inventory rows); wrapper Back and repeated New remain empty.
- **Affected tests**: Offer mount + create-multicurrency + draft-recovery + effective-scope **37/37** PASS; i18n 22/22; model 22/22; architecture **27 baseline / 0 Offer-owned**; OpenSpec strict valid; acceptance-gate (11) and architect-review-gate (14) self-tests PASS. Prior Offer 144/144, Inventory 53/53, and integer-IRR checkout/Tax evidence preserved.
- **Readonly closure** on current candidate: warehouse name/code filtering PASS; admin English and seller Persian duplicate dismiss/reopen + localized navigation to exact existing offer PASS; `writeAttempts=0`.
- Mount source-size guard restored (test-only fixture extraction, previously committed).

## Exact blocker
- `GET /v1/admin/offers/active-listing` for a confirmed **free** tuple returns **HTTP 200 with an empty body**, not the required canonical **204**. Same source path in `OfferSellerEndpoints.FindActiveAsync`: both endpoints return `api.From(Result.Success())`, which `ApiResponseFactory` maps to 200-empty. `findAdminActiveOffer`/`findSellerActiveOffer` only treat 204/JSON-null as absent, so the empty 200 throws `offer.duplicate_lookup.unavailable`; the shared MessageBox shows “بررسی آفر موجود ممکن نیست. دوباره تلاش کنید.” and the create stays fail-closed. Actual captured status/body: 200, `""` (probe evidence).
- Consequence: free-tuple multicurrency create, duplicate-create flow, locale-recovery create and lookup-retry cannot proceed; seller positive multicurrency creation is also blocked (no free tuple can be confirmed absent; seller A still has 0 free published tuples and no permission escalation was used).
- This is an architect-owned backend presentation/canonical-absence correction (`FindActiveAsync` → explicit 204/no-content via the existing presentation factory). No frontend workaround was applied to hide the server defect.

## Evidence
- `definitive-evidence/definitive-uat.json` (fresh/repeat PASS; blocked cases with exact errors), `probe-definitive.cjs`/`probe-lookup.cjs` (200-empty capture), `affected-tests.log`, `i18n.log`, `model.log`, `architecture.log`, `openspec.log`, self-test logs.
- Prior valid evidence preserved: `final-fixture-evidence/integer-checkout.json`, `owner-uat-closure-evidence/...`, owner UAT HTTP/browser, and backend suites.
- P1C/created-offer residue: none (the blocked create produced no offer).
