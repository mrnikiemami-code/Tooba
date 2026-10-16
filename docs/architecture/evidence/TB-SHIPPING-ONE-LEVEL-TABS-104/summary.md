# TB-SHIPPING-ONE-LEVEL-TABS-104 — Evidence index

Task `TB-SHIPPING-ONE-LEVEL-TABS-104` (parent 103) · Channel `tooba-main` · Worker `tooba-worker-01`
Mode `BOUNDED_CATALOG_SEED_LATENCY_AND_NARROW_SELLABLE_OFFER_PATH` · Base-SHA `ce1b9c2637bec4e36e622c8975e2ee3be915e736`
Stop-State `STOP_FOR_ARCHITECT` · **Status: BLOCKED (`BLOCKED_NARROW_SELLABLE_SEED_PATH`)**

| File | What it proves |
| --- | --- |
| `task.md`, `task-receipt.json`, `claim-task.js` | Received artifact, claim record via `GET /api/tasks/next?channelId=tooba-main`, receipt kept UNSTAGED |
| `source-route-inventory.txt` | The complete source-confirmed picture: every Catalog seed route, that `seed-only` keeps the 420–720 product constraint and `Published==0`, that the demo seed never touches offer/pricing/inventory gateways, the three bounded sellable Development seams and their exact lines, the startup chain that makes them unreachable on an empty database, and the two-boolean configuration surface with no bounded option |
| `environment-and-parity.txt` | Base-SHA/ancestry, the explicit zero-scratch decision and its justification, read-only shared parity and container/listener state |
| `shared-parity.sql` / `shared-parity.txt` | Raw read-only `tooba_alpha` parity (7 methods / 14 translations / 50 carts / 78 lines / 14 drafts / 26 seller orders / 567 products / 3223 offers) |
| `RESULT.bridge.txt` | The complete Bridge result contract posted for this task |

## Verdict in one paragraph

Task 104 was a decision task, and the decision is **BLOCKED, not PARTIAL**: at HEAD there is no existing official
Development-only route or supported parameter that can produce one sellable offer on an empty scratch database. The
only seed HTTP API builds 420–720 Draft products and asserts `Published == 0`, so it is draft-only **by
construction** — which re-classifies 103's `offers=0` from a timeout artefact into designed behaviour. The three
bounded sellable Development seams (`CatalogAttributeSchemaSellableEnricher`, `IWorkspaceDemoSeed`, `StorefrontDemo
CatalogBootstrap`) are all reachable only through the `RunLegacyBootstraps=true` startup chain, which aborts on an
empty database at `CatalogAttributeSchemaDevelopmentSeed.ApplyAsync` line 99, and `CatalogDemoSeedOptions` offers no
flag to select them individually. No seed was invoked, no scratch environment was created, and the shared stack was
verified unchanged. The deliverable is the minimal Catalog-owned proposal (P1: surface the already-computed
readiness `Missing[]` codes; P2: a bounded Development-only fixture entry point reusing only existing public
application commands; P3: an optional sequence guard) recorded in
`docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-104.md`.
