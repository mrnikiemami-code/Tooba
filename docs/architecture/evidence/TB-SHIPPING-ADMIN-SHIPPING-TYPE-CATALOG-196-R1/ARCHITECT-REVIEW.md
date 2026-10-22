# Architect review — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1
Date: 2026-10-22
Source checkpoint: d5406396e8032b32d22244f29d20336f4c4017fb
Verdict: PARTIAL / NOT ACCEPTED. TB-196 remains unaccepted.

## Independently observed
Compared changed source; read Domain ShippingType, transport reader, registry/tracing adapter, prepared-facts flow and store changes. Worker reports 43 rule + 26 architecture tests, not API/listener/PostgreSQL proof; architect did not execute tests. Source supports improvements in lossless input, prepared full patch, actual-content comparison and exact constraint matching, but no missing proof is inferred.
Reported 85 min exceeds 45; the recorded Tooba.Host build was outside R1's explicit affected-Fulfillment-only scope, despite Out-Of-Scope-Commands NONE. Do not repeat it or overwrite locked shared outputs. No Host source change or shared process shutdown is claimed.

## Newly identified blocking regressions
R196-R1-01: ShippingType.Create calls ThrowIfNegativeOrZero(sortOrder > Max ? -1 : sortOrder). This rejects VALID sortOrder 0 despite declared minimum 0, Application acceptance and database CHECK. Positive rule tests did not cover this endpoint boundary. Use the same inclusive 0..1000000 validation as Update; demonstrate the current failing regression before repair.

R196-R1-02: In removing Domain -> Fulfillment.Contracts coupling, the change removes valid Domain invariant protection. ShippingTypeTranslation.Create/Replace now only check Name != null and permit empty LanguageId/translation id, whitespace/overlength name or overlength description through public factories/mutators; ShippingType.Create directly AddRange's supplied translations without validating their uniqueness/null state. Domain is not safe merely because Application usually preflights it.
Architect correction to prior R1 instruction: do NOT add forbidden Domain -> Fulfillment.Contracts or force external error constants into Domain. Preserve dependency guard and canonical Application transport errors. Use existing local argument/invariant exception convention (or an existing lawful Domain rule mechanism) while enforcing invariants atomically; do not map argument exception MESSAGE text to business errors. Removing rules is not an acceptable coupling fix.

## Still missing (not closed by source or unit count)
Actual four-route request/handler/validator set equality; production-delegate Admin auth/body/input proof; real adapter-backed dynamic-language matrix; real ActivityListener parent/correlation/disposal/exception/cancellation; lookalike constraint negative; historical schema versus regenerated snapshot and real PostgreSQL constraints/unique/revision races. Full module acceptance and all activation/UI/Shipping assignment remain closed.
The result's TB196-Acceptance label naming only relational proof understates these missing gates; correct it in append-only evidence.

## Sole next task
TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2 (parent R1): ONLY repair the two Domain boundary regressions above, with targeted factory/mutator and one own store/handler sort-zero regression. No API harness/tracing work, layout rework, DB/schema, Host/full suite or additional feature. 20-minute hard cap. This small slice must not absorb the whole R1 backlog.
After review, issue separate bounded evidence work for API/auth/registry, tracing and relational/snapshot. These are planning gates, not executable future tasks. No automatic continuation; accepted Shipping foundations/D2 decision preserved and TB-185 paused.
