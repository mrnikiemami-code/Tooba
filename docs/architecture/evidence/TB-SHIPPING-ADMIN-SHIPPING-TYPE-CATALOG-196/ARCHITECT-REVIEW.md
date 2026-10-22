# Architect review — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196
Date: 2026-10-22
Code reviewed: 7f1e1e0806cb17f53e69996d798e27451bc386be
Current observed HEAD: c9f711dd415423f5a929b078a16381028d81c419 (later transport/evidence helpers only).
Verdict: PARTIAL / NOT ACCEPTED — bounded corrective work required.
No Shipping assignment or product activation authorized.

## Source review findings
Compared scoped production diff, read new endpoint/authorizer, commands/queries/models/validators, domain, store, language adapter and migration changes. No architect build/test/runtime execution. Worker reports only builds and 21 rule tests; no API, language matrix, real listener or PostgreSQL/race proof. Reported 63 minutes exceeded the actual 45-minute hard limit; do not describe this as deadline compliant.

R196-01 TRANSPORT/VALIDATION: ToInputs filters null elements and maps null collections to []; malformed entries therefore disappear before validators. Create/Update bind a complex body automatically rather than using the established canonical body-reader path. Empty/malformed/null input and auth-before-body behavior have not been demonstrated. GET detail has caller-controlled id without validator; List has unused caller-controlled languageId with neither validation nor meaningful behavior. All four routes need exact request/input provenance, not a claim that two command validators cover them.

R196-02 NO-OP/PREFLIGHT: ShippingTypeStore.UpdateAsync mutates translation objects BEFORE establishing whole-input safety; changed is true whenever Translations.Count>0, even for byte-identical content. Repeating the same nonempty translation patch advances Revision/UpdatedAt. Domain Replace sets Name before validating Description, so a later invalid value can partially mutate the SAME tracked instance. Preflight the complete proposed state and calculate actual semantic delta before any tracked assignment; no-op must preserve both revision and timestamp while stale expectedRevision still conflicts. Direct store/domain failure needs regression proof, not HTTP validator coverage alone.

R196-03 REGISTRY: ActiveDefault uses ordered FirstOrDefault, silently choosing one when multiple active defaults exist. Unknown active state fails but missing order becomes int.MaxValue. Reject unusable/duplicate/empty identity and missing/multiple active default facts explicitly; do not invent order/default. Validate relevant metadata required for this capability, without duplicating a registry or adding fa/en authority.

R196-04 TRACING: ShippingTypeLanguageFacts issues ILanguageLookup.ListAsync with no IModuleCallTracer child span. Add canonical tracing and real-listener parent/correlation/disposal/error-type/cancellation proof. Neighboring untraced legacy calls are no exemption and remain outside scope.

R196-05 STRUCTURE/CODE CONTRACT: Commands/ShippingTypeCommands.cs and Queries/ShippingTypeQueries.cs declare namespace Application.ShippingTypes rather than their physical subfolders; align changed paths/namespaces per Structure, updating imports only where necessary. TryNormalizeCode trims surrounding whitespace despite required rejection of malformed identity; reject rather than silently rename. New raw domain shipping_type.* literals should use the already-owned typed error constants. Match owned unique constraint identities exactly rather than substring Contains. These are bounded new-capability corrections, not a module refactor.

R196-06 RELATIONAL/SNAPSHOT GATE: Additive migration exists but was not executed. Snapshot regeneration adds hundreds of lines for unrelated pre-existing entities. A clean has-pending-model-changes result proves model/snapshot agreement, NOT that historical migration-chain schema equals that snapshot. No acceptance of this repair until a separate bounded relational task compares the real historical migrated schema with the regenerated snapshot (excluding the two new tables), executes additive Up and relevant raw constraint/unique/concurrency proofs. Do not re-scaffold/drop/recreate legacy tables or apply to shared DB. Preserve current migration/designer/snapshot during R1; no blind rollback of the regenerated snapshot either.

## Next bounded slice
Only TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1, parent TB-196: repair R196-01..05 and prove the actual API/auth/dynamic-language/no-op/preflight/trace behavior using controlled fixtures and an in-process harness. No DB/container/migration edit or full-module suite. R196-06 remains explicitly PENDING for a later separately issued task; R1 PASS does not accept TB-196, prove PostgreSQL or certify Fulfillment. Do not attempt all missing acceptance within one oversized repeat.

## Preserved requirements
Fulfillment owns installation/store-resolved shared type definitions; authorized Admin view/manage are independent; sellers cannot administer taxonomy. No foreign implementation/persistence edge, no policy inheritance, no seed labels, no Shipping assignment, no frontend/Storefront/Checkout/financial/activation/deployment change. Existing Shipping acceptance through194 and D2 decision195 remain intact; TB-185 paused. Correct evidence append-only; no false API/DB/certification claim.
