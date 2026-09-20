# Ownership Registry Design

Proposed path: `docs/architecture/TOOBA-DOMAIN-OWNERSHIP.yaml`

Purpose: compact module/capability rules — **not** a forever class list.

## Schema (design only — not implemented this task)

```yaml
version: 1
modules:
  - name: Catalog
    boundedContext: catalog
    capabilities: [product, variant, category, brand, attribute-schema]
    namespaces: [Tooba.Catalog.Domain]
    contractsProject: Tooba.Catalog.Contracts  # future
    schema: catalog
    dbContext: CatalogDbContext
    migrationsOwner: Tooba.Catalog.Infrastructure
    allowedDependsOn: [BuildingBlocks]
    publicBoundary: Contracts
  - name: Pricing
    ...
exceptions:
  - typeFamily: StoreAppearance*
    current: Catalog
    target: StorefrontSettings # TBD
    status: deferred
```

Semantic ownership still requires architecture review. CI checks namespace/module consistency against this registry after Foundation.
