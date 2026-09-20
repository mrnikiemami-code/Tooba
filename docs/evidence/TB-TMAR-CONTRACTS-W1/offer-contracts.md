# Offer.Contracts — TB-TMAR-CONTRACTS-W1

## Created

`src/backend/Modules/Offer/Tooba.Offer.Contracts/`

Contents (minimal):

- `SalesChannel` enum (stable commercial channel codes)

## Why required

Cart/Order/Pricing.Domain depended on Offer.Domain solely for `SalesChannel`. After dead-ref attempt, compile proved the enum is live. Contracts extraction is the correct boundary instead of foreign Domain.

## Rules respected

- No EF / Infrastructure / Host
- No domain entities (SellerOffer etc. stay in Domain)
- No implementation classes
- Did not clone Offer.Application API

## Compatibility

Enum remains in namespace `Tooba.Offer.Domain` inside the Contracts assembly; Offer.Domain forwards the type so existing Application/Infra/Host references continue to resolve.
