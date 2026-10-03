# TB-TMAR-WISHLIST-AMC-001-W4-R1 — Boundary evidence correction

## Accurate Contracts boundary

Production cross-module dependency remains **Contracts-only**:

- `Catalog.Contracts` — Application presentation composer + Infrastructure directory product lookup
- `Order.Contracts.Fulfillment` — production `WishlistCustomerActorResolver` **and** `WishlistDevelopmentSeed` (session/guest actor seam)

Foreign Application / Infrastructure / Domain coupling: **ZERO**.

## Corrected surfaces

- `docs/architecture/evidence/TB-TMAR-WISHLIST-AMC-001-W4/w4-certification.md`
- `docs/architecture/tmar-current-state.json` → `wishlistAmc001.crossModuleComposition`
- `docs/architecture/tmar-module-structure-manifests.json` → Wishlist `certificationNote`
