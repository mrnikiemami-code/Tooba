# Promotion → Offer Contracts — TB-TMAR-CONTRACTS-W6

## Before
Promotion.Application → Offer.Application (SalesChannel + Offer types via Application)

## After
Promotion.Application → Offer.Contracts

## Also relocated for consumers
`IReturnPolicyResolver` (+ options/choices/resolver) → Offer.Contracts so Order.Infrastructure compiles without Offer.Application after Promotion edge removal.

## Baseline
Removed: `Tooba.Promotion.Application -> Tooba.Offer.Application`
