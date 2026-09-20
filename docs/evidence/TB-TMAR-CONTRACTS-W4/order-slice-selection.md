# Order-hub slice selection — TB-TMAR-CONTRACTS-W4

## Selected edge
`Tooba.Order.Application -> Tooba.Tax.Application`

## Consumed types (checkout path)
`ITaxCalculator`, `TaxCalculationRequest`, `TaxCalculationResult`, `TaxOutcome` (used by CheckoutDirectory; Order.Application held the ProjectReference)

## Usage class
Stable read/authority calculator contract — sync tax evaluation at checkout

## Why safest/highest-value now
- Task prefers Tax/Pricing read authority before Cart/Inventory write edges
- Calculator surface is small and already DTO-shaped
- Does not change reservation/cart write orchestration
- High extraction value for future Tax service boundary

## Target
`Tooba.Tax.Contracts`

## Expected baseline reduction
Remove `Tooba.Order.Application -> Tooba.Tax.Application` from App→App

## Consistency caveat
CheckoutDirectory still calls tax inside shared-ACID TransactionScope; contract move preserves sync invocation. Microservice extraction of Tax still requires checkout consistency design.
