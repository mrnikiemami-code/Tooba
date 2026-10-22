# Architect review — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9
Date: 2026-10-22
Reviewed main: 49f5b8721290a436d82df7160811b6a62b626dd6
Verdict: ACCEPT bounded analysis WITH corrections below; proposals are not approved wholesale.
TB-196 overall: BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE.

## Independent review
Compared 0e53f460..49f5b872: only task/evidence/recovery documentation changed. Read matrix/proposal/result, raw R6 diff AND both full saved manifests, shared OutboxMessageMapping, Fulfillment InitialFulfillment, AddShippingOneLevelCatalog, ShippingMethod domain normalization and isolated migration-test source.
Independent set calculation on the actual files: historical192, model191, missing16, unexpected17. All33 raw-diff identities are consistent with 3 one-sided objects, 5 default pairs and 10 name pairs. R9's repeated model216 count is WRONG for 03-model-expected-legacy.txt; do not propagate it. These are saved fixture facts, not live merchant schema facts.

Shared mapping already defines the nonunique pending outbox index on occurred_at with processed_at IS NULL AND dead_lettered_at IS NULL; InitialFulfillment creates no such index. Raw manifest confirms absence. This suffices for the narrow missing-index repair. No shared persistence/business authority change is needed.
The two shipping_methods CHECKs originate in historical raw DDL. Preserve them pending explicit model alignment; this review does not authorize dropping protection. MethodCode normalization is stricter for that code axis; avoid treating it as exhaustive proof of every legacy-option or raw writer.
Ten full-shape name pairs are consistent with the saved diff. Names are not universally irrelevant: future migrations addressing existing objects may rely on exact names, even while ordinary reads/writes do not. No blanket exclusions authorized.

## Corrections to proposed mechanics / claims
Do NOT adopt CREATE INDEX IF NOT EXISTS followed by unconditional Down DROP INDEX. It neither validates an existing same-name object's shape nor establishes ownership for rollback. R10 uses a normal transactional CreateIndex/DropIndex under EF migration history. An already-existing same-name index must fail closed without history advance/data change, even when its shape happens to match. Shared/live preflight and any adoption/concurrent-build plan are separate deployment work.
No live table size, measured dispatcher regression, exhaustive sibling compliance or performance impact was established. Do not claim a production lock window is safe or quote a benchmark.
Default mapping is not merely metadata: HasDefaultValue changes EF insert generation unless generation semantics are explicitly controlled. No blanket default acceptance, drop or mapping change is approved here. D2-D5 remain open; no single blanket rule "model always wins"/"database always wins".

## Sole next authorization
TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10, parent R9: prepare ONE additive Fulfillment pending-index migration (plus canonical target metadata), independently execute exact schema/data preservation, Up/Down and history-controlled reapply proofs on a disposable parsed/verified PostgreSQL17 target. Standard transactional operation only; no deployment/shared DB. New history record is allowed only in that disposable DB.
Current runtime/model/snapshot, every preexisting migration/designer and all business/API/Contracts/Host/frontend/flags stay byte-identical. New latest TargetModel must match unchanged canonical model; do not rewrite the historical196 Designer to describe a different migration id.
Remaining CHECK/default/name drift stays visibly blocked. Full storage/race battery and full-schema gate are not run/relaxed in this slice. R9 source analysis/tests were not independently executed by architect; only source/evidence reviewed and set arithmetic calculated remotely.
