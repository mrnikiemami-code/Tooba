# Applicable locks — TB-P09-T015

Read from `docs/architecture/TOOBA-LOCKS.md`.

- LOCK-QTY-001 Product quantity is decimal (`numeric(18,6)`); record counts stay `int`
- LOCK-QTY-002 Product owns Unit / DecimalPlaces / optional Step
- LOCK-QTY-003 Offer owns Min/Max only
- LOCK-QTY-004 One quantity subsystem
- LOCK-QTY-005 Historical quantity snapshots immutable
- LOCK-I18N-001 Language Registry is dynamic (UoM translations by LanguageId)
- LOCK-ROUND-001 One GlobalRoundingMode
- LOCK-ROUND-002 Quantity vs money (Step not used for money)
- LOCK-ROUND-003 Historical documents not recalculated
- LOCK-INVOICE-001 Order-backed invoice
- LOCK-INVOICE-002 Header aggregates persisted; list/report reads Header
- LOCK-INVOICE-003 Finalize then sum
- LOCK-INVOICE-004 TaxAmount / DutyAmount / TotalTaxAndDutyAmount
- LOCK-INVOICE-005 Invoice rounding snapshot
- LOCK-OPS-001 Exact fulfillment quantity; no Orders UI regression
