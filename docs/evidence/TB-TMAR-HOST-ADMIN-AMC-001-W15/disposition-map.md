# Disposition map — W15 Product Workspace History read (member-level)

Source Host files (RETAINED_PARTIAL after W15):
- `Admin/ProductWorkspaceEndpoints.cs`
- `Admin/ProductWorkspaceComposer.cs`
- `Admin/ProductWorkspaceModels.cs`

Host/Admin production `*.cs` count: **52 → 52**.
History Host route count: **1 → 0**.

## Endpoint route registrations (Host)

| Member | Classification | Destination |
|---|---|---|
| `MapGet("/{productId:guid}/history", GetHistoryAsync)` | MOVE_W15 | Catalog.Endpoints ProductHistory |
| `GetHistoryAsync` method | DELETE from Host | Catalog endpoints |
| All other ProductWorkspace route maps | RETAIN | Host (later waves) |
| `ReadPermissions` / `X-Tooba-Workspace-Scope` | RETAIN for remaining Host routes; history GET leaves Host | GET: no edit-scope check; Admin auth via ICatalogAdminAuthorizer |
| `AdminPanelAccess.RequireAuthorizedAsync` on history | REPLACE | `ICatalogAdminAuthorizer` |
| `CatalogActorHttpBinding` group filter | RETAIN on Host group; history route leaves Host | no actor bind on history GET |
| `ToError` / PlatformHttpException catch on history | DELETE from history surface | `ApiResponseFactory` |

Exact route preserved: `GET /v1/admin/products/{productId:guid}/history` with optional `section`, `skip`, `take`.

Endpoint defaults: `skip null → 0`, `take null → 50`.

## Composer members

| Member | Classification | Destination |
|---|---|---|
| `GetHistoryPageAsync` | MOVE_W15 then DELETE Host public | `GetProductHistoryQuery` + reader |
| `ToHistoryItemView` | DELETE (history-route-only after move) | Application handler mapping |
| `BuildHistoryShellListsAsync` | RETAIN | Host Composer (aggregate GetAsync Activity/Audit) |
| Aggregate `GetAsync` Activity/Audit shell usage | RETAIN | Host Composer |
| `AppendProductHistoryAsync` call sites in other mutations | RETAIN | Host Composer (not W15) |

## Models

| Type | Classification | Destination |
|---|---|---|
| `ProductHistoryPageView` | MOVE shape to Catalog; DELETE Host | Application `ProductHistoryPageView` |
| `ProductHistoryItemView` | MOVE shape to Catalog; DELETE Host | Application `ProductHistoryItemView` |
| `ProductHistoryItem` | RETAIN Host | Still used by `ProductWorkspaceView.Activity` / `Audit` |
| `ProductWorkspaceView.Activity` / `Audit` | RETAIN | Host (untouched behaviorally) |

## Catalog persistence / contracts

| Member | Classification | Destination |
|---|---|---|
| `ICatalogDirectory.ListProductHistoryAsync` | EXTRACT → focused reader; thin unwrap wrapper retained | `IProductHistoryReader.ListAsync` |
| `CatalogDirectory.ListProductHistoryAsync` | DELEGATE one-way to focused reader; unwrap IOE for legacy | thin wrapper |
| `CatalogDirectory.ToHistoryDto` | MOVE into focused reader (or shared private) | ProductHistoryReader |
| `AppendProductHistoryAsync` / `QueueProductHistory` | RETAIN | Not W15 (writes stay) |
| `ProductHistoryPage` / `ProductHistoryEntryDto` | RETAIN Application CatalogContracts | Live callers + reader return |
| `ProductHistoryRules` | PRESERVE Domain | SectionLabelFa, ActorSystemFa, section constants |

### Live thin-wrapper callers of `ListProductHistoryAsync` (document)

- Host `ProductWorkspaceComposer.BuildHistoryShellListsAsync` (aggregate shell)
- Host.Tests `ProductHistoryTests`
- Host.Tests `PrimaryCategoryMigrationTests`

## Domain / rules (PRESERVE)

| Concern | Classification |
|---|---|
| skip = Math.Max(0, skip) | PRESERVE in reader |
| take = Math.Clamp(take <= 0 ? 50 : take, 1, 100) | PRESERVE in reader |
| section blank/null → no filter; nonblank → Trim + exact equality | PRESERVE |
| Order OccurredAt DESC, HistoryId DESC | PRESERVE |
| TotalCount after section filter, before Skip/Take | PRESERVE |
| ActorDisplayName blank → ActorSystemFa | PRESERVE |
| ProductHistoryRules.SectionLabelFa | PRESERVE |

## Error / result

| Code | HTTP | W15 treatment |
|---|---|---|
| `workspace.product.missing` | 404 | Result + CatalogErrorCodes on new HTTP/read seam |
| `workspace.permission.denied` | 403 | Not applied on GET history (CanView always true from ReadPermissions; no edit-scope) |

No PlatformHttpException / expected InvalidOperationException / message parsing on moved history HTTP/read seam.
Legacy `ICatalogDirectory.ListProductHistoryAsync` may still throw IOE for old callers via unwrap OUTSIDE new HTTP seam.

## Validation matrix

| Request | Classification |
|---|---|
| `GetProductHistoryQuery` | NO_VALIDATOR_REQUIRED |

## Final disposition

`READY_TO_MIGRATE`
