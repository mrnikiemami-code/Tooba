# Guard repair — W19-R1

## Added

- `HostAdminAmcW19R1GuardTests`
  - gateway: zero `catch (InvalidOperationException)`, zero `ex.Message`, uses `TryIsAssignableProductCategory`, no hierarchy duplication
  - Domain: `TryGetCategoryLevel` / `TryIsAssignableProductCategory` / shared `TryResolveCategoryLevel`; throwing APIs retained
  - W19 ownership: Host routes=18, PW routes=1, Admin=52, Contracts-only
  - SoT `hostAdminAmcW19R1` + W20 absent
- `CatalogCategoryTreeSafeProbeW19R1Tests` — valid L1/L2/L3 parity, missing/cycle non-throwing, throwing API compatibility
- Existing `HostAdminAmcW19GuardTests` + `ProductWorkspaceAggregateGetW19Tests` remain green

## Non-goals

No repair of Host composer IOE catch, list/grid, other Catalog callers.
