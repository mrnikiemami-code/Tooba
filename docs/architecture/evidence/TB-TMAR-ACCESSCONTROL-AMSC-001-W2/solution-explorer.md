# Solution Explorer — AccessControl (W2)

## Classification

| Axis | State |
| --- | --- |
| Solution-Explorer-State | `CANONICAL` |
| Solution file | `src/backend/Tooba.slnx` |
| Solution folder | `/Modules/AccessControl/` |

## Verified `.slnx` grouping

```xml
<Folder Name="/Modules/AccessControl/">
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Domain/Tooba.AccessControl.Domain.csproj" />
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Contracts/Tooba.AccessControl.Contracts.csproj" />
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj" />
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj" />
  <Project Path="Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj" />
</Folder>
```

- Exactly **5** `<Project Path="Modules/AccessControl/…` entries — one per production project on disk.
- `Tooba.AccessControl.Endpoints` **is** present (no missing Endpoints entry).
- Every declared path resolves to an existing `.csproj` on disk.
- No stale entry points at a deleted/renamed path.
- No flat `"/Modules/"` catch-all folder exists for this module.

## Sibling comparison (no decorative divergence)

| Module | Solution folder | Project entries |
| --- | --- | --- |
| `Party` | `/Modules/Party/` | 5 |
| `AccessControl` | `/Modules/AccessControl/` | 5 |
| `Catalog` | `/Modules/Catalog/` | 5 |
| `Offer` | `/Modules/Offer/` | 6 (includes `Tooba.Offer.Tests`) |
| `Order` | `/Modules/Order/` | 6 (includes `Tooba.Order.Tests`) |

`AccessControl` has no tests project, so 5 entries is the correct count — this is **not** a
missing-project defect (W0 finding `F5`, out of scope). Entry ordering inside the folder is
alphabetical (`Domain`, `Contracts`, `Application`, `Infrastructure`, `Endpoints`), matching
`Catalog`; it is cosmetic only and was deliberately left unchanged to avoid churn.

## Grouping rules respected

- No assembly was renamed for visuals.
- No `.csproj` was moved for visuals.
- No decorative Solution Folder disconnected from disk.
- Namespace-only organization was **not** treated as sufficient — disk and Solution Explorer
  were verified separately.
