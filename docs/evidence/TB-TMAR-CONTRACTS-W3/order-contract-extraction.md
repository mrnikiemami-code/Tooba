# Order contract extraction — TB-TMAR-CONTRACTS-W3

## Change
Order.Application ProjectReference:
- removed `Tooba.Offer.Application`
- added `Tooba.Offer.Contracts`

## Code
`OrderContracts.cs` continues `using Tooba.Offer.Domain` for `SalesChannel` namespace compatibility (type lives in Offer.Contracts assembly since CONTRACTS-W1).

## Compatibility
No type forwarding required — SalesChannel already lives in Offer.Contracts (CONTRACTS-W1). Offer.Domain retains TypeForwardedTo for legacy Domain consumers.

## Not moved
Offer Application directories / write APIs remain in Offer.Application (out of scope).
