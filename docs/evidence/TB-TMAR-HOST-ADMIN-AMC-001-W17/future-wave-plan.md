# W17 — Future wave plan (post Architect ACCEPT of W17)

Bounded waves (~10–15 min where practical). Do **not** start W18 until Architect ACCEPT.

| Wave | Scope | Production behavior |
|---|---|---|
| **W18** | Establish `Tooba.ProductWorkspace` module skeleton (projects, DI, empty Endpoints map hook, Contracts ports stubs for Catalog read composition + Party.Contracts `IPartyLookup` adoption plan). Host still owns routes. No route moves. | No HTTP/behavior change |
| **W19** | Move **read** aggregate: `GET /`, `POST /query` (+ grid policy/engine), `GET /{id}`. Catalog read ports replace direct CatalogDbContext for those paths. | Preserve response shapes |
| **W18b / W19b** (may fold into W18–W19) | Move `GET /brand-options` to **Catalog.Endpoints** (Catalog-only). | Preserve |
| **W20** | Catalog Commands for core writes (create, catalog-title, core, quantity-policy, category, additional category, brand). ProductWorkspace Endpoints own HTTP; post-command composition returns `ProductWorkspaceView`. | Preserve 200/201 + body |
| **W21** | Lifecycle: publish / unpublish / archive / restore — Catalog Commands + post-command composition. | Preserve |
| **W22** | Variants: create / patch — Catalog Commands + post-command composition. | Preserve |
| **W23** | Delete (Offer.Contracts reference check) to Catalog; 204/409 preserved. Soft-archive-on-reference semantics preserved. | Preserve |
| **W24 (final)** | Delete Host `ProductWorkspaceEndpoints/Composer/Models` (+ relocate DevelopmentBootstrap disposition). Remove Host DI/registration. Host/Admin file count decreases. Grid files under Host/Grid for products removed or emptied. | Host residue ZERO for ProductWorkspace |

### Explicit non-goals per wave

- No StoreAppearance work (deferred).
- No next Host folder until Admin ProductWorkspace disposition complete (or Architect re-scopes).
- No HTTP contract breaks without dedicated Architect task.
- No Catalog composition of Offer/Pricing/Inventory/Tax/Party.

### Next recommended wave after W17 ACCEPT

**W18** — establish lawful ProductWorkspace composition module + Contracts ports (no production route evacuation yet).
