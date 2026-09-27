# Behavior parity — W8

| Concern | State |
|---|---|
| Routes / methods / paths | PRESERVED (seven Definition routes) |
| Admin auth | ICatalogAdminAuthorizer on all seven |
| List order | DisplayOrder then Code |
| Get missing | 404 catalog.attribute.missing |
| Create 201 + `{ definitionId }` | PRESERVED (AttributeDefinitionCreatedResult) |
| Create + metadata | Atomic single SaveChanges (improves prior two-step without schema change) |
| Update returns definition view | PRESERVED |
| Preview impact shape | PRESERVED |
| Set capability returns view | PRESERVED |
| Add option 201 + `{ optionId }` | PRESERVED |
| Duplicate code/name | 409 typed codes |
| Variant-axis rules | Domain + typed Results |
| Tenant mutation guard | EnsureCanMutateAsync preserved |
| Retained Host Attribute routes | UNAFFECTED mappings |

## Create atomicity

Prior: create SaveChanges then optional metadata SaveChanges (partial possible).  
W8: one Application create with optional metadata before single SaveChanges. Success JSON unchanged.
