# Validation — Host/CatalogAdapters AMC-001

## Focused commands

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~HostCatalogAdaptersAmcGuardTests|FullyQualifiedName~HostAdminAmcW33MerchandisingGuardTests|FullyQualifiedName~HostStorefrontAmcR3GuardTests|FullyQualifiedName~TmarDurableGuardTests"
```

## Expected

- `HostCatalogAdaptersAmcGuardTests` PASS (folder ABSENT, Catalog.Infrastructure adapters, Contracts seam, SoT hostCatalogAdaptersAmc)
- `HostAdminAmcW33MerchandisingGuardTests` PASS (gate path retargeted)
- `HostStorefrontAmcR3GuardTests` PASS (shell adapter path retargeted)
- `TmarDurableGuardTests` PASS after PLACEHOLDER → real SHA stamp

## Schema / frontend

- NONE / UNCHANGED
