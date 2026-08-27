# 04 — Page composition domain (TB-P06-T015)

Module: `src/backend/Modules/PageComposition/`  
Schema: `page_composition`

## Entities

| Entity | Key fields |
|---|---|
| `PageDefinition` | PageDefinitionId, PageKey (`home`), TenantId, Locale (null = all), VersionToken, CreatedAt, UpdatedAt |
| `PageSection` | PageSectionId, PageDefinitionId, SectionType, DisplayOrder, IsVisible, Variant, ConfigurationJson (≤4000) |

## Operations

- reorder (ordered section ids)
- show / hide (`IsVisible`)
- update config (validated JSON)
- add approved section (catalog type only)
- remove section
- restore default (canonical home order)

## Boundaries

| Owns | Does NOT own |
|---|---|
| Section arrangement / visibility / safe config | Article body / SEO (Content) |
| PageKey = `home` MVP | Product identity (Catalog) |
| Tenant-scoped definitions | Free-form HTML/CSS widgets |

Migration: `InitialPageComposition`. Seed: `PageCompositionDevelopmentSeed` (idempotent per tenant, e.g. store-alpha).
