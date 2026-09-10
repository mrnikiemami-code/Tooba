# R1 Multi-Seller Fixture

Built via normal Storefront cart APIs on Host `:5088` (no projection hand-edits).

| Field | Value |
|---|---|
| Store / Host | `alpha.localhost` / `127.0.0.1:5088` |
| Fixture cart | `01a08c19-1233-7000-bdba-ae0d7b02b96c` |
| Handoff cart | `01a08c19-2599-7000-b43d-907a228e9a2a` |
| Sellers | **2** distinct `SellerPartyId`s |

## Offers / sellers

| Role | SellerPartyId | OfferId | Title (human) | Prep days |
|---|---|---|---|---|
| Seller A (faster) | `01a03826-97c5-7000-ad15-c9d141b1f32e` | `01a03826-9936-7000-b499-ff26a6123a8c` | گوشی هوشمند سری 1 | **1** |
| Seller B (slower) | `01a030d1-40cb-7000-8abe-6d31739956c5` | `01a030d1-40f1-7000-95f6-b8efc58e2619` | گوشی هوشمند سری 1 | **3** |

Prep overrides configured in `Tooba:ShippingMethods:SellerPreparationDaysByPartyId` (`appsettings.Development.json`). Default remains 1; Arman seller overridden to 3.

## Cart build path

1. `POST /v1/storefront/cart` (guest secret)
2. Add KG offer qty `1.25`
3. Add Arman offer qty `1`
4. Result: `itemCount=2.25`, both sellers present on cart lines

Store-enabled method used for proofs: `post:express` (lead **2** days, base price **200000** IRR).

Script: `_r1_runtime.mjs` · raw: `r1-runtime-raw.json`
