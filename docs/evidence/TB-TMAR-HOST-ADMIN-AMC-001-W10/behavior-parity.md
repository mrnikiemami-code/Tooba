# Behavior parity — W10

| Concern | State |
|---|---|
| GET attributes locale default fa-IR | PRESERVED (handler) |
| Editor JSON shape (fields/readiness/categoryPath) | PRESERVED (`ProductAttributeEditorState`) |
| Effective schema ordering | PRESERVED |
| Localized definition/option names | PRESERVED |
| Display formatting (bool/number/enum) | PRESERVED |
| Readiness embedded in editor | PRESERVED |
| Readiness when no primary category → complete empty | PRESERVED |
| Bulk all-or-nothing transaction | PRESERVED |
| Bulk history EventAttributesChanged | PRESERVED |
| Bulk response = refreshed editor (requested/default locale) | PRESERVED |
| Single upsert semantics | PRESERVED |
| Enum / active / schema / variant-axis rules | PRESERVED (typed Result) |
| Route methods/paths | PRESERVED |
| Admin auth | ICatalogAdminAuthorizer |
| Actor/context for history | Module CatalogActorRequestBinding |
| Tenant isolation | Catalog DbContext unchanged |
| Expected failure HTTP | Typed CatalogErrorCodes via ApiResponseFactory (replaces generic catalog.attribute.invalid + ex.Message) |
| Seller SetProductAttributeRequest | RETAINED in Host Admin → **RELOCATED to Seller in W10-R1** |
