# Line rounding — TB-P09-T014

`FinancialRounder.Round` applies GlobalRoundingMode at money places.

998 × 20% = 199.6 → Floor places 0 → 199; LineNet 799.

Promo/tax use the same helper. Header sums finalized line values; no extra sum-round.
