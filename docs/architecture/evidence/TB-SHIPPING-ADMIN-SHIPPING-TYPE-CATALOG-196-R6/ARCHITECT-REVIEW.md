# Architect review — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6
Date: 2026-10-22
Reviewed checkpoint: f30eaee17b046f45413a17d85645f883b442db86
Verdict: BLOCKED confirmed; acceptance of TB-196 remains closed.

## Independently confirmed source
Read RESULT, actual latest migration, production context/design-time factory, snapshot type blocks and test fixture. Migration creates quoted PascalCase columns but CHECK SQL uses snake_case. This is intrinsically inconsistent (e.g. "Code" versus code) and matches the reported PostgreSQL42703. Design-time factory uses bare UseNpgsql while production uses ToobaNpgsql.ConfigureModuleContext/UseSnakeCaseNamingConvention. Snapshot's type properties also lack explicit snake_case column names. This is a real scaffolding/configuration defect, not a reason to change customer behavior.
Worker reports real disposable provider failure and manifests; architect did not execute commands. Remaining committed relational assertions are UNEXECUTED, not acceptance proof.

## Legacy/snapshot limits and process correction
Historical-versus-current-model comparison reports missing outbox pending index, two migration-only shipping_methods CHECKs, five defaults and ten names. These remain explicit unresolved legacy differences, outside type-migration repair authority.
The fixture compares initialized snapshot to IDesignTimeModel and obtains209 operations. Source explicitly passes designTime:true; the report's "non-design-time path" dismissal is not established. No equality was proved, yet the fixture materializes GenerateCreateScript and proceeds to latest migration after known discrepancies. This violated R6's Gate1 stop condition; do not claim full scope compliance. The resulting repeated failures do not strengthen proof.
R6 fixture must fail immediately on unproved snapshot equivalence or substantive legacy discrepancy in its full-schema mode. Teardown must mark DownExecuted only if latest Up truly succeeded/history row existed. Offline negatives inside a failing collection fixture are not independently proved passing by an all-failed filter.
The exactly-one-active fixture correction is independently visible in source and Worker reports its1/1 execution; close that R5 corner. R5 bounded DI/registry/listener evidence otherwise retained.

## Architect authorization for ONE repair slice
Only TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7, parentR6.
1. Correct this module's design-time factory to reuse the canonical production Npgsql/schema/naming configuration WITHOUT resolving real/shared credentials or opening a connection.
2. Narrow exception to historical-byte-preservation: amend ONLY the latest UNACCEPTED and unshipped 20261022101415_ShippingTypeCatalog migration (cannot be repaired by a later migration because execution never reaches it). Align the two new tables' columns/PK/FK/index references with actual canonical snake_case model. Update ONLY their corresponding entity/relationship blocks in its designer/current snapshot. Older migration/designer and every unrelated snapshot block remain unchanged. If evidence shows this migration deployed/applied elsewhere, STOP for reviewed upgrade plan rather than rewriting that environment's history.
3. Independent isolated type-only Up/Down acceptance compares ACTUAL historical legacy schema before versus after: add/drop just two own tables, legacy exact unchanged. This is explicitly a narrow proof independent of the unresolved whole-snapshot gate, NOT permission to suppress that gate or label full parity PASS.
4. Strengthen R6 full-schema fixture fail-fast/disposal bookkeeping; do not skip checks or mark unknown equality true.
No full relational/storage/race battery now; focus on deployability of the two new tables and fresh-context store smoke. Whole TB196 stays BLOCKED_PENDING_LEGACY_SNAPSHOT_AND_RELATIONAL_ACCEPTANCE even if R7 passes.
No shared DB, legacy DDL/index/default/constraint repair, full snapshot regeneration, old migrations, public API/Domain/owner/UI/Shipping/financial/activation change. Record exact scope/timing and stop. TB-185 paused.
