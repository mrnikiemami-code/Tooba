# Content Disposition — TB-TMAR-HOST-CONTENT-AMC-001

| File | Disposition | Target | Notes |
| --- | --- | --- | --- |
| ContentAdminAccess.cs | MOVE | `Content.Endpoints/Admin/` | `IAdminPanelAccess` + capability check; codes `content.view/create/edit/publish`; Persian 403/503 preserved |
| ContentEndpoints.cs | MOVE + SPLIT | Admin articles → `Endpoints/Admin/`; public → `Endpoints/Storefront/ContentStorefrontEndpoints.cs` | Routes preserved |
| ContentCategoryEndpoints.cs | MOVE | `Content.Endpoints/Admin/` | |
| ContentAuthorEndpoints.cs | MOVE | `Content.Endpoints/Admin/` | |
| ContentTagEndpoints.cs | MOVE | `Content.Endpoints/Admin/` | |
| ContentArticleMediaEndpoints.cs | MOVE | `Content.Endpoints/Admin/` | |
| ContentArticleCommentEndpoints.cs | MOVE | `Content.Endpoints/Admin/` | |
| ContentPanelComposer.cs | MOVE | `Content.Endpoints/` | Uses Infrastructure grid + `ContentAdminGridPolicies` |
| ContentAuthorPanelComposer.cs | MOVE | `Content.Endpoints/` | |
| ContentArticleMediaPanelComposer.cs | MOVE | `Content.Endpoints/` | |
| ContentMediaAssetValidator.cs | MOVE | `Content.Infrastructure/Adapters/` | Registered in `ContentModule.AddServices`; Media.Application debt remains |
| ContentDevelopmentSeedHost.cs | MOVE | `Host/Composition/` | Namespace `Tooba.Host.Composition`; **not** Development allowlist |

## Adjacent (required for Endpoints without Host ref)

| File | Disposition | Target |
| --- | --- | --- |
| Host/Grid/AdminContentGridQueryEngine.cs | MOVE | `Content.Infrastructure/Grid/` (public) |
| Host/Grid/AdminContentAuthorGridQueryEngine.cs | MOVE | `Content.Infrastructure/Grid/` (public) |
| AdminListGridPolicies.Content / ContentAuthors | EXTRACT | `Content.Infrastructure/Grid/ContentAdminGridPolicies.cs` |

## Host wiring

- `Program.cs`: `MapContentModuleEndpoints()` + `AddContentEndpointPresentation()`; seed uses Composition namespace
- `Tooba.Host.csproj` + `Tooba.slnx`: Content.Endpoints project
- Host/Content folder: **deleted** (0 `.cs`)

## Untouched

- Host/Development allowlist (3 files)
- Other Host/Grid engines (Story/Review/Sellers)
- Schema/migrations/frontend
