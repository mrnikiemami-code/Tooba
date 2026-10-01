# Validation — Host/Settings AMC-001

## Focused commands

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~HostSettingsAmcGuardTests|FullyQualifiedName~SettingsFoundationTests|FullyQualifiedName~TmarDurableGuardTests"
```

## Expected

- `HostSettingsAmcGuardTests` PASS (folder ABSENT, Composition binder, SoT hostSettingsAmc)
- `SettingsFoundationTests` PASS (route locks + Development seed idempotent when Docker available)
- `TmarDurableGuardTests` PASS after PLACEHOLDER → real SHA stamp

## Schema / frontend

- NONE / UNCHANGED
