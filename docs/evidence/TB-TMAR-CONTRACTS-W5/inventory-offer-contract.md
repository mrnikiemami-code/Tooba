# Inventory → Offer Contracts — TB-TMAR-CONTRACTS-W5

## Before
Inventory.Application → Offer.Application (dead App edge; Infra already used Offer.Contracts lookup)

## After
Inventory.Application → Offer.Contracts

## Baseline shrink
Removed: `Tooba.Inventory.Application -> Tooba.Offer.Application`
