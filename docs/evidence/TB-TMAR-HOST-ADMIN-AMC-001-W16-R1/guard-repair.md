# Guard repair — W16-R1

## Added

`src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminAmcW16R1GuardTests.cs`

Proves:

- `ProductPublishReadinessReader` has no `catch (InvalidOperationException` / no catch / no throw IOE
- uses `CatalogCategoryTreeRules.IsAssignableProductCategory` (not Ensure)
- no category hierarchy duplication (`GetCategoryLevel` / `ProductAssignableLevel` / ancestor walk absent in reader)
- W16 publish-readiness route ownership + focused seam reuse preserved
- Host/Admin count remains 52; W17 task/evidence absent

## Focused tests

`ProductPublishReadinessCapabilityTests`:

- empty primary gate
- level 1/2 → false, level 3 → true via `IsAssignableProductCategory`
- non-assignable path does not throw

## Preserved

`HostAdminAmcW16GuardTests` remain; W16 migration guards still green.
