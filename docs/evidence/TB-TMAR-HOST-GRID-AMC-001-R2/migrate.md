# TB-TMAR-HOST-GRID-AMC-001-R2 — Migrate

## Scope

Move Story admin grid policy + DB-native engine out of Host into Story.Infrastructure; Host Story composer consumes Story DI port only.

## Disposition executed

| Item | Before | After |
| --- | --- | --- |
| `AdminListGridPolicies.Stories` | Host | `Story.Infrastructure/Grid/StoryAdminGridPolicies` |
| `AdminStoryGridQueryEngine` | Host/Grid | `Story.Infrastructure/Grid` |
| Grid port | none | `IAdminStoryGridPort` + `AdminStoryGridAdapter` |
| `StoryPanelComposer` | `new AdminStoryGridQueryEngine(db)` + Host policy | `IAdminStoryGridPort` only |
| Program DI Host Story engine | registered | **Removed** |
| Host Story engine file | present | **Deleted** |

## Ownership notes

- FOUNDATION_PARTIAL: Story Application gained BuildingBlocks + Ports; Infrastructure gained Grid/Adapters.
- Normalize maps `GridQueryValidationException` → `PlatformHttpException` to preserve Host Story endpoint catch mapping.
- No Host/Development sink; BuildingBlocks.Grid remains shared platform.

## Behavior parity

- Field whitelist / default sort `displayOrder` asc / paging / search / advanced filter unchanged.
- DB-native filter/sort/page semantics preserved in moved engine.

## Guards

- New: `HostGridAmcR2GuardTests`
- Baseline: removed `Grid/AdminStoryGridQueryEngine.cs` from `tmar-host-write-files.json`

## Explicit non-goals

- Full Story ARCH-COMPLETE-002 / CQRS dissolution of StoryPanelComposer
- Schema change
- Commit / push / Bridge POST
