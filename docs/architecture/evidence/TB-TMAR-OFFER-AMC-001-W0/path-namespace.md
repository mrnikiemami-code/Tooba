# Offer path ↔ namespace — W0

Path-Namespace-State: `EXACT`

## Method

For every production `.cs` file in `Tooba.Offer.{Domain,Application,Contracts,Infrastructure,Endpoints}`, the single-line `namespace X;` declaration was compared to the path-derived namespace (`<ProjectName>` + folder segments joined by `.`). EF `Migrations/*` and `*ModelSnapshot.cs` are excluded per the repository lock (generated block-scoped namespaces).

This is the same rule enforced by `OfferPhysicalStructureGuardTests.ARCH_MODULE_PHYSICAL_001_namespaces_equal_path_derived_namespaces_exactly`, which passes on the current disk state.

## Result

| Project | Mismatches |
| --- | --- |
| `Tooba.Offer.Domain` | 0 |
| `Tooba.Offer.Application` | 0 |
| `Tooba.Offer.Contracts` | 0 |
| `Tooba.Offer.Infrastructure` | 0 |
| `Tooba.Offer.Endpoints` | 0 |

No alias workaround exists: no `global using` in any Offer production file, and no `global using Tooba.Offer*` in `Host/Tooba.Host`.

## Note for W1/W2

Every file moved in the migrate/structure waves must have its namespace rewritten to the new path-derived value in the same commit. Files whose namespace changes (path-derived, no alias):

- `Tooba.Offer.Application.Commands.<UseCase>` → `Tooba.Offer.Application.Offers.Commands.<UseCase>`
- `Tooba.Offer.Application.Queries.<UseCase>` → `Tooba.Offer.Application.Offers.Queries.<UseCase>`
- `Tooba.Offer.Application.{Mappings,Ports,Policies,ReadModels}` → `Tooba.Offer.Application.Offers.{...}`
- `Tooba.Offer.Application.Validators` → `Tooba.Offer.Application.Validation` (shared rules/codes) or `Tooba.Offer.Application.Offers.{Commands,Queries}.<UseCase>` (request validators)
- `Tooba.Offer.Contracts.Ports` → `Tooba.Offer.Contracts.ReturnPolicy` (return-policy boundary types)
- `Tooba.Offer.Application.ReturnPolicy` (new; `ReturnPolicyResolver`)
