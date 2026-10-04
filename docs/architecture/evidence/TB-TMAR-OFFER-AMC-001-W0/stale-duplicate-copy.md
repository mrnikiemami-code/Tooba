# Offer stale / duplicate physical copies — W0

Physical-Copy-State: `CLEAN`

## Method

Enumerated all `*.cs`, `*.resx`, `*.csproj` under `src/backend/Modules/Offer` excluding `bin/`/`obj/`; compared type declarations against the accepted manifest/SoT paths; searched the whole `src/backend` tree for a second live home of the same Offer responsibility.

## Result

| Check | Result |
| --- | --- |
| Leftover path after a historical move | NONE |
| Same responsibility in two live Offer paths | NONE |
| Solution entry pointing at a deleted path | NONE |
| Second `OfferDbContext` / second `offer` schema owner | NONE |
| Duplicate `OfferErrorCodes` with conflicting values | NONE — Domain subset (4 codes) and Contracts superset (17 codes) are **distinct namespaces** (`Tooba.Offer.Domain.Errors` vs `Tooba.Offer.Contracts.Errors`); the 4 shared code *values* are identical and each has exactly one `ErrorDescriptor` |
| Duplicate `OfferStatus` / `SalesChannel` | NONE — Domain `ValueObjects` are the internal enums; Contracts `Dtos` are the boundary enums (explicit mapping in `OfferContractMapping`) |
| Duplicate `OfferEndpointLocalizer` | REMOVED historically; `OfferEndpointLocalizer_is_deleted_and_resources_exist` guard asserts absence |
| `TypeForwarders.cs` / `TypeForwardedTo` | NONE |
| `Host/Seller` folder | ABSENT (removed historically) |

## Historical residue explicitly confirmed absent

- `Host/Storefront/StorefrontPrimaryOfferResolver.cs`
- `Host/OfferGlobalUsings.cs`
- `Host/.tmp-t014-test-out/`
- `Tooba.Offer.Application/{Commands,Queries}/Offer{Requests,Handlers,Queries,QueryHandlers}.cs`

## Note on `Tooba.Offer.Tests/bin`, `obj`

Build output only. Not source. Ignored by every guard.

## Risk

`CLEAN`. No physical-copy defect blocks structure certification.
