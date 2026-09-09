# Quantity policy

`EffectiveQuantityPolicy` + `IQuantityNormalizer` in BuildingBlocks.

Product: Unit + DecimalPlaces (0–6) + optional Step.
Step=null → any value legal under DecimalPlaces (1.25 with places=2).
Step=0.25 → align then precision-round using the single GlobalRoundingMode.

Cart/checkout resolve policy by batched variant lookup (`GetEffectiveQuantityPoliciesForVariantIdsAsync`).
