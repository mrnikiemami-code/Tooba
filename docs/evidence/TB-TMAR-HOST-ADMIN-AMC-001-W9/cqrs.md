# CQRS — W9 Category Attribute-Schema

| Route | Request | Handler | Port |
|---|---|---|---|
| GET .../attribute-schema/effective | GetEffectiveCategorySchemaQuery | GetEffectiveCategorySchemaHandler | GetEffectiveAsync |
| POST .../bindings | BindCategoryAttributeCommand | BindCategoryAttributeHandler | BindAsync |
| PATCH .../bindings/{definitionId} | UpdateCategoryAttributeBindingCommand | UpdateCategoryAttributeBindingHandler | UpdateBindingAsync |
| DELETE .../bindings/{definitionId} | UnbindCategoryAttributeCommand | UnbindCategoryAttributeHandler | UnbindAsync |
| PUT .../bindings/order | ReorderCategoryAttributeBindingsCommand | ReorderCategoryAttributeBindingsHandler | ReorderAsync |

Pipeline: HTTP → `ICatalogAdminAuthorizer` → `ISender` → Handler → `ICategoryAttributeSchemaDirectory` → `Result` → `ApiResponseFactory`.

Endpoints inject zero directory/DbContext. Unique MediatR request types; no `*Contracts.cs` bundle; path↔namespace exact.
