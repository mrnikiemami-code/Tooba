# W18 — Analyze (foundation)

## Mission

Establish the canonical `Tooba.ProductWorkspace` module skeleton so W19 can migrate aggregate reads without inventing architecture mid-move.

## W17 decision preserved

- Remaining Host ProductWorkspace routes = **19**
- Full ProductWorkspace post-write aggregate responses = **15**
- Blind Catalog ownership of aggregate composition = **REJECTED**
- PageComposition suitability = **NO**
- Selected target = **split ownership**
  - Catalog = Catalog write authority + Catalog-only routes
  - ProductWorkspace (new) = Admin aggregate composition + aggregate HTTP
  - Host = temporary residue only
- Direct `CatalogDbContext` reach-through must be eliminated (later waves)
- ProductWorkspace composition must use module Contracts only
- `Party.Application` dependency must become `Party.Contracts`
- StoreAppearance remains deferred

## Classification

| Concern | Classification |
|---|---|
| Destination module foundation | **FOUNDATION_MISSING → created in W18** |
| Route moves | **NONE in W18** |
| Response contracts | **UNCHANGED (still Host-owned)** |
| Host ProductWorkspace production files | **RETAINED** |
| New business behavior | **NONE** |
| ARCH-COMPLETE-002 | **NOT claimed** |

## Skills applied

- `tooba-architecture-analyze` (ownership / boundary inventory for skeleton)
- `tooba-architecture-migrate` (foundation-creation gate only — no business file moves)
- `AGENTS.md` + TMAR structure / Host evacuation protocol + W17 evidence
