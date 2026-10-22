# R13 append-only corrections to the R6 evidence

The R6 fixture/assertions referenced in `RESULT.md` were **never executed as passing proof**; R13 established that
they were structurally stale. This note is append-only and does not rewrite the original R6 text.

1. `TypeCatalogPostgresSchemaFixture.LatestMigration` was `20261022101415_ShippingTypeCatalog` and the whole gate
   compared the CURRENT model against a chain ending before that migration — the accepted
   `ix_outbox_messages_pending` (R10) index can never exist at that checkpoint, so the gate could not have passed
   as written. R13 replaced the single checkpoint with the real phases (pre-type → type → R10 → strict current).
2. The raw read helpers and raw-SQL matrix used quoted PascalCase columns/rules (`"Revision"`, `"Code"`, …) and
   `FK_shipping_type_translations_shipping_types_ShippingTypeId`, while the accepted migration creates
   `snake_case` columns and `fk_shipping_type_translations_shipping_types_shipping_type_id`.
3. `TwoArrivalGate` never captured a per-context original `Revision`; `StatementFailureInterceptor` counted reads
   and treated any reader as a completed write; `LegacyProjection` suppressed objects by a `shipping_type`
   substring and a history prefix.

No R6 acceptance claim is retroactively corrected by prose here; the executed R13 evidence is the authority for
these surfaces.
