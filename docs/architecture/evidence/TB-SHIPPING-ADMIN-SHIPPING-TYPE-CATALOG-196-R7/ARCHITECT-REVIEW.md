# Architect review — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7
Date: 2026-10-22
Reviewed checkpoint: d78cfa136689f68bdbaeba97a51a401d105ae1fa
Verdict: ACCEPT bounded factory/type-migration naming repair and isolated Up/Down/store smoke.
TB-196 overall: BLOCKED_PENDING_LEGACY_SNAPSHOT_AND_RELATIONAL_ACCEPTANCE.

## Independently reviewed
Read migration/snapshot/factory/fixture diffs and new metadata/isolated migration/fail-fast tests. Latest Up columns and PK/FK/index references now match snake_case; owned CHECK/index semantics unchanged. Only latest migration/designer/current snapshot appear under migrations; snapshot changes are the two type blocks/relationship. Factory uses canonical ToobaNpgsql.ConfigureModuleContext. No older migration or public business/Domain/Contracts/Endpoint change.
Isolated test uses actual historical migrated primary DB, applies latest, checks history, old-manifest equality and real-store fresh readback, then executes Down and checks absence/history/old-manifest equality. This meaningfully establishes deployability and bounded preservation. It is not full storage/race proof or full snapshot parity.
Manifest exclusions still use token matching; for current checked-in migration the source proves only two table changes, so acceptance is bounded to this migration. Do not advertise that helper as arbitrary table-identity/data-flow enforcement.
Full-schema fixture now stops before model projection/latest Up when snapshot equality is unproved, and before Up on substantive drift. Down bookkeeping no longer claims no-op as real Down. Unit diagnostic assertions plus reported deliberate blocked probe are consistent with source.
Worker reports6 offline tests +1 real PostgreSQL test passing and one intentional gate-probe failure; architect reviewed remotely and did not execute or independently time the reported10min.

## Next source of mismatch
Current snapshot/latest Designer still contain pre-type entity metadata generated under the previously wrong design-time convention. Type-only R7 repair reduced reported operations209->191 but did not repair the old metadata. This is distinct from real migrated-schema differences (outbox pending index, migration-only checks/defaults/names). Do not flatten these into one acceptance claim.
R6's three substantive legacy differences and default/name discrepancies remain unresolved. No new DDL or shared migration is authorized by this review.

## Sole next task and exact exception
TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8, parentR7: canonical generated METADATA-ONLY rebuild of current FulfillmentDbContextModelSnapshot and BuildTargetModel metadata of the latest unshipped 20261022101415 Designer from the unchanged current canonical design-time model. This explicitly supersedes R7's type-block-only restriction ONLY for these two metadata artifacts.
Keep current runtime mapping/factory and latest executable migration Up/Down byte-identical; all older migration/designer byte-identical. No new migration/DDL, schema/index/default/check change, business change, baseline/guard suppression or certification.
Prove old mismatch and new zero relational model differences for initialized snapshot/latest TargetModel against actual IDesignTimeModel, with generator/version provenance and detailed semantic diff. Do not "repair" equivalence by changing runtime model to match a stale snapshot or widening ignore lists.
After this purely metadata repair, actual historical-schema parity and storage/race/constraint acceptance still need separate authorized work. Do not run PostgreSQL/storage suites merely to rediscover known blockers. No Shipping assignment/UI/quote/activation/deployment; TB185 paused.
