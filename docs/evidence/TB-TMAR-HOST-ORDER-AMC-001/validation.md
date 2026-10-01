# Validation — Host/Order AMC-001

## Focused commands

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~HostOrderAmcGuardTests|FullyQualifiedName~CheckoutIdentityContractTests|FullyQualifiedName~TmarDurableGuardTests"
```

## Expected

- `HostOrderAmcGuardTests` PASS
- `CheckoutIdentityContractTests` PASS (actor still exposes session.IsAuthenticated)
- `TmarDurableGuardTests` PASS after PLACEHOLDER → real SHA stamp
