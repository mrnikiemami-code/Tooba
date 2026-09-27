# Result / errors — W6 Facets

## Canonical path

Handler → `Result`/`Result<T>` → Endpoint `ApiResponseFactory.From` → catalog contributor + resx.

## CatalogErrorCodes (Facet)

| Code | HTTP | Meaning |
|---|---|---|
| `catalog.facet.category.missing` | 404 | Category not in tenant Catalog |
| `catalog.facet.definition.missing` | 404 | Attribute definition missing |
| `catalog.facet.schema.missing` | 400 | Definition not in effective schema |
| `catalog.facet.not_filterable` | 400 | Schema row not filterable |
| `catalog.facet.display_type.invalid` | 400 | DisplayType vs ValueKind violation |
| `catalog.facet.override.missing` | 404 | Local override row missing (prior Host `catalog.facet.missing`) |
| `catalog.facet.reorder.invalid` | 400 | Reorder set incomplete/duplicate/mismatched |

## Prior Host surface

- Hard-coded `catalog.facet.invalid` / `catalog.facet.missing` with `ex.Message` titles
- `PlatformHttpException` catch → dropped (authorizer + factory)

## Message classification

- FacetDirectory: no Persian IOE / message-as-code
- Domain display-type: typed enum, not exception message
- Unknown exceptions propagate (global Host presentation)

## Descriptor ownership

Single owner: `CatalogErrorCatalogContributor` + `CatalogErrors.resx` / `.fa.resx`.
