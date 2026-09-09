# Migration safety — TB-P09-T014

`20260909140000_AddInvoiceHeaderAggregates`: ADD COLUMN IF NOT EXISTS.

Backfill from stored line counts/qty and existing header money. Duty=0. Rounding=Nearest. Money places from currency.

No historical recalculation with today's Product/rounding settings.
