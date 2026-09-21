# result-delta — TB-TMAR-REFBATCH-TP-RESULT-001

## Pricing changes actually required

`ISellerOfferPricingGateway.SetPriceAsync` already returned `Task<Result>` after FND-RESULT-001-R1, but expected seller-write failures for invalid market/currency and active base overlap still fell through nested `CreatePriceAsync`/`ActivateAsync` `SemanticException` paths.

Delta applied in `PriceDirectory.SetPriceAsync`:

- `MarketCode.TryParse` / `CurrencyCode.TryParse` → `Result.Failure` (stable Pricing error codes)
- preflight active-base overlap → `Result.Failure(pricing.overlap)` before create/activate
- amount invalid / offer missing|seller mismatch remain `Result.Failure`
- success → `Result.Success()`
- no catch-all Exception→Result
- Offer seller endpoint continues to use central `ApiResponseFactory.From(write)`

Supporting: `CurrencyCode.TryParse`, `MarketCode.TryParse`.

## Tax RESULT_DELTA_NOT_APPLICABLE

Revalidated: no Tax-owned HTTP route; calculation failures remain `TaxOutcome` via `TaxCalculationResult`; no SemanticException HTTP surface; no ceremonial Result adoption. Zero Tax production changes.

## Foundation compatibility

No BuildingBlocks production changes. Pricing failures remain catalogued in Pricing ErrorDescriptor + resx (en/fa). Offer endpoint already central ApiResponseFactory consumer.

## Observability

Result business failures continue to flow as `IResultStatus` / `result.status=business_failure` via existing TracingBehavior. No new ActivitySource/spans.

## Architecture guards

Added `Seller_price_write_uses_result_not_expected_semantic_exception_control_flow` in PricingArchitectureGuardTests.

## Focused validation (ran)

- `Tooba.Pricing.Tests` — Passed 14/14 (includes new Result seam guard)
- `dotnet build src/backend/Tooba.slnx` — 0 errors (pre-existing unrelated warnings only)

## Skipped validation (deliberately)

- `Tooba.Tax.Tests` — Tax production unchanged; RESULT_DELTA_NOT_APPLICABLE
- Offer/BuildingBlocks/Host integration suites — no Offer/shared Result/API production changes beyond already-applied SetPriceAsync Result signature; budget forbids broad suites