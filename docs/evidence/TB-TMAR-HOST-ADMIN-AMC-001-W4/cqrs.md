# CQRS — W4 Tags

All 9 HTTP operations are MediatR-backed:

| Route | Request |
|---|---|
| GET tags/ | `ListTagsQuery` |
| POST tags/ | `CreateTagCommand` |
| GET tags/{id} | `GetTagQuery` |
| GET products/{id}/tags/ | `ListProductTagsQuery` |
| POST products/{id}/tags/{tagId} | `AssignProductTagCommand` |
| DELETE products/{id}/tags/{tagId} | `RemoveProductTagCommand` |
| GET categories/{id}/tags/ | `ListCategoryTagsQuery` |
| POST categories/{id}/tags/{tagId} | `AssignCategoryTagCommand` |
| DELETE categories/{id}/tags/{tagId} | `RemoveCategoryTagCommand` |

Flow: HTTP → `ICatalogAdminAuthorizer` → `ISender` → Handler → `ITagDirectory` → `Result` → `ApiResponseFactory`.

No endpoint injects `ICatalogDirectory`, `ITagDirectory`, or `CatalogDbContext`.
Authoritative request types exist exactly once (no duplicate Commands/Queries).
