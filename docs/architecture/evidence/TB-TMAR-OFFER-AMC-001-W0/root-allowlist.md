# Offer root allowlist / forbidden roots — W0

Root-Allowlist-State: `ENFORCED`

## Manifest entries (`docs/architecture/tmar-module-structure-manifests.json`)

`module: "Offer"`, `structureCertified: true`, `lockVersion: "ARCH-COMPLETE-002"`, 3 project entries:

| Project | `rootAllowlist` | `forbiddenRootFiles` |
| --- | --- | --- |
| `Tooba.Offer.Application` | `[]` | `OfferContractMapping.cs`, `IOfferStore.cs`, `OfferReadModelComposer.cs`, `OfferRequests.cs`, `OfferHandlers.cs`, `OfferQueries.cs`, `OfferQueryHandlers.cs` |
| `Tooba.Offer.Endpoints` | `["OfferEndpointModule.cs"]` | `OfferSellerEndpoints.cs`, `IOfferSellerAuthorizer.cs`, `OfferErrorCatalogContributor.cs`, `OfferErrorResources.cs`, `OfferEndpointLocalizer.cs` |
| `Tooba.Offer.Infrastructure` | `[]` | `OfferModule.cs`, `OfferDbContext.cs`, `OfferStore.cs`, `OfferEvents.cs`, `OfferModuleMigration.cs`, `OfferSchemaMigrator.cs`, `OfferDevelopmentSeedGateway.cs`, `OfferOutboxRegistration.cs` |

`forbiddenTopLevelFolders` is empty for all three entries.

## Disk verification

| Project | Root `.cs` files | Verdict |
| --- | --- | --- |
| `Tooba.Offer.Domain` | none | PASS |
| `Tooba.Offer.Application` | none | PASS |
| `Tooba.Offer.Contracts` | none | PASS |
| `Tooba.Offer.Infrastructure` | none | PASS |
| `Tooba.Offer.Endpoints` | `OfferEndpointModule.cs` only | PASS (allowlisted) |
| `Tooba.Offer.Tests` | none | PASS |

No `forbiddenRootFiles` entry exists at any project root. No root dump.

## Gaps found

1. **`Tooba.Offer.Contracts` and `Tooba.Offer.Domain` have no manifest entry.** They are governed by the project-local `OfferPhysicalStructureGuardTests` allowlists but are absent from `tmar-module-structure-manifests.json`. W2 will add honest entries (`rootAllowlist: []`, forbidden root dumps) for both.
2. The manifest `forbiddenRootFiles` lists are stale-name-based (they name the *old* god files). They remain correct but are not the primary structural guard; W2 will complement them with folder-granularity + capability-first guards.

## No-widening commitment

W2/W3 will not widen `rootAllowlist` to hide debt, and will not remove any `forbiddenRootFiles` entry.
