# Path ↔ namespace exactness

State: **`EXACT`** — checked for every production `.cs` file in all five production projects
(62 files; the 13 EF migration/designer/snapshot files under `Persistence/Migrations` use the
existing canonical EF exemption and were not modified).

Method: expected namespace = project name + relative directory segments joined by `.`; compared
against the file's declared `namespace` directive. Result: **zero mismatches**, before and after
W2 (after accounting only for the intended root file).

| File | Expected | Actual | Verdict |
| --- | --- | --- | --- |
| `Tooba.Shipping.Endpoints/ShippingEndpointModule.cs` (new) | `Tooba.Shipping.Endpoints` | `Tooba.Shipping.Endpoints` | EXACT |
| `Tooba.Shipping.Endpoints/Admin/ShippingRateCoverageEndpoints.cs` | `Tooba.Shipping.Endpoints.Admin` | unchanged | EXACT |
| all other production files | path-derived | unchanged | EXACT |

Notes:

* The moved type changed namespace `Tooba.Shipping.Endpoints.Admin` → `Tooba.Shipping.Endpoints`
  because it is now a **root** file; no namespace alias, `using` alias workaround or
  `global using` was introduced to hide folder debt.
* `Tooba.Shipping.Infrastructure/DependencyInjection/ShippingModule.cs` keeps
  `Tooba.Shipping.Infrastructure.DependencyInjection` — the certified Payment/`DependencyInjection/`
  precedent, not a mismatch.
* Migration files are exempt by existing lock; their namespaces
  (`...Infrastructure.Persistence.Migrations`) already equal their paths anyway.

Durable guard: `ShippingModuleStructureGuardTests.Every_production_namespace_equals_its_path_derived_namespace`.
