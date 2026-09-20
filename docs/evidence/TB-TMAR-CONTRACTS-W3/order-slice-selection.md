# Order-hub slice selection — TB-TMAR-CONTRACTS-W3

## Selected edge
`Tooba.Order.Application -> Tooba.Offer.Application`

## Consumed types
`SalesChannel` enum only (via `OrderContracts` / Domain snapshots)

## Usage class
Stable value/authority contract (read-time channel identity), not write/orchestration

## Why safe now
- Already owned in `Tooba.Offer.Contracts` since CONTRACTS-W1
- No CheckoutDirectory TransactionScope change
- No Saga / process redesign
- Single ProjectReference swap; no multi-edge Order hub rewrite

## Target
`Tooba.Offer.Contracts`

## Expected baseline shrink
Remove `Tooba.Order.Application -> Tooba.Offer.Application` from `tmar-app-to-app-edges.json`

## Consistency caveat
Checkout still uses SalesChannel inside shared-ACID scope; contract move does not alter transaction semantics. Future microservice extraction of Offer still requires checkout consistency design.
