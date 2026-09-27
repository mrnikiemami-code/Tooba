# Analyze — TB-TMAR-HOST-CONTENT-AMC-001

## Before tree (`src/backend/Host/Tooba.Host/Content/`)

```
ContentAdminAccess.cs
ContentArticleCommentEndpoints.cs
ContentArticleMediaEndpoints.cs
ContentArticleMediaPanelComposer.cs
ContentAuthorEndpoints.cs
ContentAuthorPanelComposer.cs
ContentCategoryEndpoints.cs
ContentDevelopmentSeedHost.cs
ContentEndpoints.cs
ContentMediaAssetValidator.cs
ContentPanelComposer.cs
ContentTagEndpoints.cs
```

Production file count before: **12**

## Foundation

Content module: Application + Domain + Infrastructure present.  
**Missing:** Endpoints project, Contracts project, CQRS handlers.  
Manifest: Content absent from `tmar-module-structure-manifests.json` → not structure-certified.  
Classification: **FOUNDATION_PARTIAL** (Endpoints foundation required for evacuation).

## Ownership map (summary)

| File | Classification | Disposition |
|------|----------------|-------------|
| Content*Endpoints (6) | HTTP_ENDPOINT / MODULE | MOVE → Content.Endpoints |
| Content*PanelComposer (3) | PRESENTATION_COMPOSITION | MOVE with endpoints (interim) or dissolve to Application CQRS |
| ContentAdminAccess | AUTHORIZATION_ADAPTER | MOVE Endpoints; prefer `IAdminPanelAccess` / authorizer seam (no Host static) |
| ContentMediaAssetValidator | INTEGRATION_ADAPTER | MOVE → Content.Infrastructure/Adapters (Media.Application debt remains) |
| ContentDevelopmentSeedHost | HOST_COMPOSITION_ROOT | RELOCATE out of Content/ — **not** into Development allowlist (locked 3 files); use `Host/Composition/` |

## Out-of-folder adjacent residue (resolved in same AMC)

- `Host/Grid/AdminContentGridQueryEngine.cs` → **MOVED** `Content.Infrastructure/Grid/`
- `Host/Grid/AdminContentAuthorGridQueryEngine.cs` → **MOVED** `Content.Infrastructure/Grid/`
- Content / ContentAuthors entries in `AdminListGridPolicies` → **MOVED** to `ContentAdminGridPolicies` (module-owned Normalize)

Course correction: Endpoints must not reference Host; composers require Infrastructure grid engines.

## Development lock

Do **not** add files to `Host/Development/` — AMC-001 retained allowlist of 3 marketplace bootstraps.
ContentDevelopmentSeedHost → `Host/Composition/` only.

## Final analyze disposition

**READY_TO_MIGRATE** → executed: Host/Content ZERO via Content.Endpoints + Infrastructure adapters/grid + Composition seed.
Full Offer-style CQRS/canonical ApiResponseFactory = residual module debt.
