# Invoice snapshot — TB-P09-T014

Header stores RoundingModeUsed + MoneyDecimalPlacesUsed at Open/checkout.

Backfill uses stored historical values and Nearest; does not recalculate with today's Floor.

Changing GlobalRoundingMode does not rewrite existing header amounts or snapshot fields.
