# Order Tax contract extraction — TB-TMAR-CONTRACTS-W4

## Created
`Tooba.Tax.Contracts`

## Moved
- `TaxOutcome` enum (Contracts assembly; namespace `Tooba.Tax.Domain`; Domain TypeForwardedTo)
- `ITaxCalculator`
- `TaxCalculationRequest`
- `TaxCalculationResult`

## Retained in Tax.Application
Admin/config: `ITaxDirectory`, `ITaxUseCaseGuard`, category/rule references

## Dependency direction
- Order.Application → Tax.Contracts (not Tax.Application)
- CheckoutDirectory uses Tax.Contracts for calculator types
- Tax.Application / Tax.Infrastructure continue implementing directory + calculator DI

## BuildingBlocks
Tax.Contracts references `Tooba.BuildingBlocks` only for `QuantityRoundingMode` (domain-neutral primitive).
