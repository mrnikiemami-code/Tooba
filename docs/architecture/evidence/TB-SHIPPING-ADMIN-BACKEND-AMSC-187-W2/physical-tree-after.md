# Physical tree — AFTER

Only two production paths changed. Everything else is byte-identical to the base tree.

```diff
  Tooba.Shipping.Endpoints/
+   ShippingEndpointModule.cs                     <-- NEW: canonical composition entry (namespace Tooba.Shipping.Endpoints)
    Admin/ShippingAdminAuthorizer.cs
    Admin/ShippingMethodFinancialStatusEndpoints.cs
    Admin/ShippingOperatingScheduleEndpoints.cs
    Admin/ShippingOperationalQuoteEndpoints.cs
    Admin/ShippingPackagingPolicyEndpoints.cs
-   Admin/ShippingRateCoverageEndpoints.cs        <-- composition class removed; only the 7 rate/coverage routes remain
+   Admin/ShippingRateCoverageEndpoints.cs
    Errors/ShippingErrorCatalogContributor.cs
    Resources/ShippingErrorResources.cs
    Resources/ShippingErrors.resx
    Resources/ShippingErrors.fa.resx
```

## Exact move mapping (mechanical, behavior-preserving)

| Type | Before | After |
| --- | --- | --- |
| `ShippingEndpointModule` | `Admin/ShippingRateCoverageEndpoints.cs`, namespace `Tooba.Shipping.Endpoints.Admin` | `ShippingEndpointModule.cs`, namespace `Tooba.Shipping.Endpoints` |
| `ShippingRateCoverageEndpoints` | `Admin/ShippingRateCoverageEndpoints.cs` (unchanged location, unchanged namespace) | same path, unchanged |
| `IShippingAdminAuthorizer` / `ShippingAdminPermissions` | `Admin/ShippingRateCoverageEndpoints.cs` | unchanged (no cohesion defect proved: both are the admin route surface's own seams) |

Repository-wide compile fix caused by the namespace change of the moved type:

| File | Change |
| --- | --- |
| `src/backend/Host/Tooba.Host/Program.cs` | added `using Tooba.Shipping.Endpoints;` (one import; no Host file/folder added, no Host business code) |
| `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingErrorCatalogAndResourcesTests.cs` | added the `Tooba.Shipping.Endpoints` namespace import; the "single registration site" assertion now names the authoritative composition path (`.../Tooba.Shipping.Endpoints/ShippingEndpointModule.cs`) |
| `Tooba.Shipping.Tests/Admin/ShippingAdminEndpointOwnershipTests.cs` | `MapGroup("/v1/admin")` pin moved to a new `CompositionSource()` (same assertion, authoritative file) |
| `Tooba.Shipping.Tests/Admin/ShippingMethodFinancialAuthorityTests.cs` | `ShippingMethodFinancialStatusEndpoints.Map(admin)` pin now read from `ShippingEndpointModule.cs` |
| `Tooba.Shipping.Tests/Admin/ShippingOperatingScheduleEndpointOwnershipTests.cs` | admin-group + `Map(admin)` pins now read from `ShippingEndpointModule.cs` |
| `Tooba.Shipping.Tests/Packaging/ShippingPackagingEndpointOwnershipTests.cs` | admin-group + `Map(admin)` pins now read from `ShippingEndpointModule.cs` |
| `Tooba.Shipping.Tests/Operations/ShippingOperationalQuoteEndpointOwnershipTests.cs` | `Map(admin)` pin now read from `ShippingEndpointModule.cs` |
| `Tooba.Shipping.Tests/Composition/ShippingModuleStructureGuardTests.cs` | NEW durable structure guard (7 facts) |
| `Infrastructure/DependencyInjection/ShippingModule.cs` | comment-only: stale "not composed yet / no migration exists" remark replaced with the true composition + CLI-migrations statement |

No request, validator, resource, route, DTO, migration or schema file was moved or edited.
