# Root allowlist

State: **`ENFORCED`** — every project root matches its manifest allowlist exactly.

| Project | Root `.cs` files (actual) | `rootAllowlist` | Result |
| --- | --- | --- | --- |
| `Tooba.Shipping.Application` | none | `[]` | ENFORCED |
| `Tooba.Shipping.Domain` | none | `[]` | ENFORCED |
| `Tooba.Shipping.Contracts` | none | `[]` | ENFORCED |
| `Tooba.Shipping.Infrastructure` | none (`DependencyInjection/ShippingModule.cs` is the composition root) | `[]` | ENFORCED |
| `Tooba.Shipping.Endpoints` | `ShippingEndpointModule.cs` | `["ShippingEndpointModule.cs"]` | ENFORCED (after W2) |

Forbidden-root-file lists recorded in `docs/architecture/tmar-module-structure-manifests.json` for the
`Shipping` entry are all absent on disk:

* Application: `[]`; forbidden top-level folders `Commands`, `Queries`, `Validators`, `Models`,
  `Ports` (technical-axis-first roots) — none exists.
* Domain: forbids root `ShippingRatePolicy.cs`, `ShippingMethodCoverage.cs`,
  `ShippingCoverageGrant.cs`, `ShippingPackagingPolicy.cs` — all live under `Aggregates/`.
* Contracts: forbids root `ShippingErrorCodes.cs`, `ShippingAdminContracts.cs`,
  `ShippingRatePolicyContracts.cs` — all live in capability folders.
* Infrastructure: forbids root `ShippingModule.cs`, `ShippingDbContext.cs` and top-level
  `Migrations/`, `Directories/` — none exists (`Persistence/`, `Persistence/Migrations/` are canonical).
* Endpoints: forbids root `ShippingRateCoverageEndpoints.cs`,
  `ShippingMethodFinancialStatusEndpoints.cs`, `ShippingOperatingScheduleEndpoints.cs`,
  `ShippingPackagingPolicyEndpoints.cs`, `ShippingOperationalQuoteEndpoints.cs`,
  `ShippingAdminAuthorizer.cs`, `ShippingErrorCatalogContributor.cs`, `ShippingErrorResources.cs` —
  the capability/authorizer/catalog files stay in `Admin/`, `Errors/`, `Resources/`.

No allowlist was widened to hide debt; the single new root file is the canonical composition entry
itself. The manifest entry is `structureCertified: false` and does not claim certification.

Durable guard: `ShippingModuleStructureGuardTests.Root_allowlists_and_forbidden_paths_match_the_real_physical_roots`.
