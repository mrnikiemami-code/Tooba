# 18 — Current variant boundary (TB-P06-T021)

Task policy: do **not** redesign advanced Variant/Attribute architecture. Report limitations as `ADVANCED_VARIANT_DEFERRED` unless they block the selected real product sale.

## Verdict

```text
ADVANCED_VARIANT_DEFERRED
```

Current Variant support is **sufficient** for a single-axis (or single default) sellable proof product. Advanced attribute schemas, inheritance, bulk matrix generation, and complex product-type axes are **deferred** and **non-blocking** for TB-P06-T021 PASS.

## What exists today (supported model)

| Layer | Support | Paths |
|---|---|---|
| Domain | `CatalogProduct` has many `CatalogVariant`; variants carry axis attribute values; fingerprint uniqueness | `src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogDomain.cs` |
| Contracts | `CreateVariantAsync(productId, catalogCodeSeam, axes[])`; attribute definitions with `IsVariantAxis` | `CatalogContracts.cs` |
| Offer bind | Offer keys **`CatalogVariantId`**, not ProductId alone | `OfferDomain.cs`, `OfferContracts.cs` |
| Storefront PDP | Returns `variants[]`, `selectedVariantId`, per-variant `primaryOffer`; FE variant chips | `StorefrontModels.cs`, `StorefrontComposer.cs`, `storefront-pdp.tsx`, `storefront-api.ts` |
| Demo seed | Default “pack” axis; first few products get a second “special pack” variant with its own Offer | `StorefrontDemoCatalogBootstrap.cs` (~lines creating `CreateVariantAsync` + optional special variant) |
| Admin workspace | Variants section lists variant status + offer counts (read) | `product-workspace-screen.tsx` |
| Seller panel | Surfaces `catalogVariantId` on offer rows/detail; no variant matrix editor | `seller-api.ts`, vendor products pages |

Architecture foundation: `docs/architecture/42-catalog-product-variant-foundation.md` (accepted TB-P03-T001).

## What is deferred (not required to close sale)

- Category-driven attribute schemas / product-type authoring UX
- Multi-axis combinatorial variant matrix UI (size × color × … bulk generate)
- Attribute inheritance across category trees
- Buy-box ranking across many sellers/variants beyond current primary-offer selection
- Treating variants as free-form duplicated products

Label for all of the above: **`ADVANCED_VARIANT_DEFERRED`**.

## Blocking check

| Question | Answer |
|---|---|
| Can a sellable item exist with one Product + one Variant + one Offer? | **Yes** (domain + seed + storefront) |
| Does lack of advanced matrix block cart/checkout/payment? | **No** |
| Is missing Variant **create HTTP** a sale blocker? | **Yes, but as Catalog authoring gap** (folded into sale blockers B1 in `03-sale-blockers.md`), not as “need advanced variant redesign” |

## Proof-product recommendation for T021

Use the **narrowest supported configuration**:

```text
1 Published Catalog Product
→ 1 Catalog Variant (single axis value or seed-equivalent default pack)
→ 1 Seller Offer on that Variant
→ 1 Active tax-exclusive Price (Pricing)
→ Inventory position with available units > 0
```

Do not expand scope into multi-axis fashion matrices for the E2E proof.
