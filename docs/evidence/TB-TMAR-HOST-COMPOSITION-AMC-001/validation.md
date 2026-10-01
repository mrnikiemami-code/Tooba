# Validation

```text
dotnet build Content.Infrastructure + Localization.Infrastructure + Host
dotnet test --filter "FullyQualifiedName~HostCompositionAmcGuardTests|FullyQualifiedName~ContentDevelopmentSeedHostSourceTests|FullyQualifiedName~HostDevelopmentMigrationSeamGuardTests|FullyQualifiedName~HostWalletAmcGuardTests|FullyQualifiedName~HostSupportAmcGuardTests|FullyQualifiedName~TmarDurableGuardTests"
```

Expected PASS after PLACEHOLDER → implementation SHA stamp.
