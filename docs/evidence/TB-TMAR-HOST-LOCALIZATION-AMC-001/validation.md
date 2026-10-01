# Validation — Host/Localization AMC-001

## Focused commands

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~HostLocalizationAmcGuardTests|FullyQualifiedName~LanguageDirectoryPersistenceTests|FullyQualifiedName~TmarDurableGuardTests"
```

## Expected

- `HostLocalizationAmcGuardTests` PASS (folder ABSENT, Endpoints + Content guard, SoT hostLocalizationAmc)
- `LanguageDirectoryPersistenceTests` PASS
- `TmarDurableGuardTests` PASS after PLACEHOLDER → real SHA stamp

## Schema / frontend

- NONE / UNCHANGED
