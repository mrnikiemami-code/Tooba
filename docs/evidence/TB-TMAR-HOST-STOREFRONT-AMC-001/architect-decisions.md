# TB-TMAR-HOST-STOREFRONT-AMC-001 — Architect decisions

Locked by Architect (user-authorized AMC execution) for Host/Storefront recovery.

## Decisions

1. **Composer / residual browse BFF owner = Catalog** — no new `Modules/Storefront` BFF module.
2. **FOUNDATION_PARTIAL landing in Catalog is authorized** for storefront slices. Do not claim full Catalog ARCH-COMPLETE-002 certification in these waves.
3. **Multi-wave Storefront AMC** — Host ZERO is the program goal; each wave stops for review when required by pipeline, but Architect authorizes sequential R1→R2→R3 execution in this chat.
4. **Host/Development sink forbidden** — demo seed later waves go to module Development only.
5. **Thin Host security adapters retained** — Payment/Checkout session adapters stay Host-owned (relocate under `Host/Security/...` when touched); `StorefrontAccountIdentity` stays Host Authentication platform.

## Wave plan (locked)

| Wave | Scope |
| --- | --- |
| **R1** | Template catalog preview (Fashion + Industry + facade + 2 routes) → Catalog CQRS/Endpoints. Remove Host registrations. Durable `HostStorefrontAmcR1GuardTests`. |
| **R2** | Checkout-identity policy read → Catalog; Host enforcement adapters only. Appearance GET → Catalog. Media GET → Media.Endpoints. Security adapter folder hygiene. |
| **R3** | StorefrontComposer + Models + remaining Endpoints → Catalog Application Storefront capability with Contracts-only foreign enrichment; update Wishlist/Landing adapters. |
| **R4** | DemoCatalogBootstrap/Matrix → module Development; Host/Storefront ABSENT; final Host ZERO certify + SoT. |

## R1 start authorization

`READY_TO_MIGRATE` for R1 template slice. Behavior preservation mandatory (paths, DTO shapes, error codes, template purity).
