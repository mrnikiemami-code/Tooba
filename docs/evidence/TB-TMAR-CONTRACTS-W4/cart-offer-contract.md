# Cart → Offer Contracts — TB-TMAR-CONTRACTS-W4

## Before
Cart.Application → Offer.Application (`SalesChannel` via Offer.Domain namespace)

## After
Cart.Application → Offer.Contracts (`SalesChannel` assembly ownership unchanged since CONTRACTS-W1)

## Notes
Cart.Application consumed only `SalesChannel` from the Offer edge; no Offer Application directories/APIs.

## Baseline shrink
Removed: `Tooba.Cart.Application -> Tooba.Offer.Application`
