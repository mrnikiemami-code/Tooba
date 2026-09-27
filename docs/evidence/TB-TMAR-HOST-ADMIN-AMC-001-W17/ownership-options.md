# W17 — Ownership options evaluation

## A. Catalog owns all remaining ProductWorkspace endpoints

**Reject.**

Evidence: `GetAsync` / list enrich / grid metrics compose Offer, Pricing, Inventory, Tax, and Party. Catalog owning those HTTP responses would require Catalog → foreign business composition inside Catalog Application/Infrastructure — forbidden by ARCH-CONTRACT-001 and Host evacuation destination rules.

Catalog may still own **Catalog-only** slices (already done for SEO/media/history/publish-readiness) and **Catalog write commands**. It must not own aggregate `ProductWorkspaceView` composition.

## B. Host remains permanent owner

**Reject as end-state.**

Evacuation protocol: Host must not permanently own module-specific endpoints, grid policies, persistence authority, or cross-module orchestration that belongs at a module boundary. Current Host ownership is **temporary residue** until a lawful composition module exists.

## C. Extend existing PageComposition module

**Reject — PageCompositionSuitability = NO.**

Code evidence:

| Check | Evidence |
|---|---|
| Domain intent | `PageKeys.Home`, `SectionCatalog` (hero, stories, category_grid, product rails, brands, articles…) — storefront **home page section layout**, not Admin product workspace |
| Application surface | `IPageCompositionDirectory` — GetHome / Admin home section CRUD only (`PageCompositionContracts.cs`) |
| Dependencies | `PageComposition.Application.csproj` references **Domain only** — no Catalog/Offer/Pricing/Inventory/Tax/Party Contracts |
| Persistence | Own `PageCompositionDbContext` for page definitions/sections — unrelated schema |
| Host residue | Separate Host `PageComposition/` folder + `PageCompositionPanelComposer` — different surface |

Name similarity (“Composition”) is semantic coincidence. Extending PageComposition would conflate storefront page layout with Admin commercial product workspace aggregation.

## D. New dedicated Backoffice / ProductWorkspace composition module

**Viable and preferred as composition + HTTP owner for aggregate routes.**

Requirements if chosen:

- Depend only on module **Contracts** / gateways (Catalog, Offer, Pricing, Inventory, Tax, Party.Contracts)
- Never reference foreign Infrastructure/DbContexts
- Never Application → foreign Application
- Own aggregate read models (`ProductWorkspaceView`, list/grid DTOs), grid policy/engine, and HTTP registration for remaining workspace routes
- Orchestrate Catalog write commands then re-compose aggregate (strategy B)

Do **not** create the module in W17.

## E. Split ownership (Catalog writes + composition reads)

**Selected.**

| Concern | Owner |
|---|---|
| Catalog mutation authority (create/core/title/quantity/category/brand/lifecycle/variants/delete) | **Catalog** (Commands + Domain + Catalog persistence) |
| Aggregate GET / list / grid composition | **New ProductWorkspace composition module** |
| Post-write `ProductWorkspaceView` HTTP body | **Composition module** after Catalog command |
| brand-options (Catalog-only read) | **Catalog** (preferred) or composition via Catalog Contracts |
| DELETE with Offer reference check | **Catalog** command using **Offer.Contracts**; HTTP may live on composition shell or Catalog NoContent endpoint |
| Already evacuated SEO/media/history/readiness | **Catalog** (preserve) |
| Final HTTP for remaining 19 Host routes | **Composition module Endpoints** (except optional brand-options/delete if Catalog takes NoContent/Catalog-only reads) |
| Host | Temporary until evacuation; then thin shell only |

StoreAppearance: deferred; no shared ownership with ProductWorkspace required by evidence.
