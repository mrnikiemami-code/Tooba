# TB-P04-T003 — Dependency decision

Inspected `src/frontend/package.json` before adding packages.

Added runtime packages: none.

Added test runner packages: none. Pure state tests use Node `node:test` with `--experimental-strip-types`.

TanStack Table: not added. T001 classified Shopeiva tables as REBUILD. A headless table would help later virtualization, but this foundation needs Tooba-owned typed filters, logical sticky columns, Design System primitives, and a storage-agnostic saved-view seam. Native `<table>` plus existing tokens meets the capability set without a second layout engine.

Heavyweight enterprise grids: rejected (bundle, RTL, licensing, design-token mismatch).

Revisit: a later envelope may add TanStack Table solely for virtualization if workspace datasets require it. The query adapter and column types stay Tooba-owned either way.
