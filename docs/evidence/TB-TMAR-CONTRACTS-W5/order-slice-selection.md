# Order-hub slice selection — TB-TMAR-CONTRACTS-W5

## Selected edge
`Tooba.Order.Application -> Tooba.Pricing.Application`

## Types
`IPriceLookupGateway`, `PriceQuote`, `PriceResolutionQuery` (checkout line quote resolution)

## Classification
Sync read/authority — price selection at checkout

## Why Pricing first
Task prefers Pricing before Promotion; smaller than Cart/Inventory write orchestration; already DTO-shaped lookup gateway.

## Target
`Tooba.Pricing.Contracts`

## Expected shrink
Remove Order.App→Pricing.App from App→App baseline

## Transaction caveat
CheckoutDirectory still resolves prices inside shared-ACID scope; boundary-only change.
