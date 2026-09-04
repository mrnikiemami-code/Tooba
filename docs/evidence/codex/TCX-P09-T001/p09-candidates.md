# TCX-P09-T001 candidate P09 tracks

## 1. Notification preference and delivery-policy foundation — RECOMMENDED

- Owning modules: `UserPreference` first; later `Notification` consumes a contract/policy result.
- Commercial value: lets customers and sellers control transactional channel choices without weakening mandatory/security notices; closes the explicit `NOTIFICATION_PREFERENCES = DEFERRED` gap.
- Expected areas: `Modules/UserPreference` Domain/Application/Infrastructure and module-local tests; later feature-local notification/settings endpoints and UI.
- Database: new typed preference rows/columns in `user_preference` schema with a module-local migration; no Content tables.
- Frontend: not required for the first slice; later existing settings surfaces can consume it.
- Shared navigation/Host wiring: none for the first slice because `UserPreference` is already composed.
- P08 conflict risk: low.
- Size: Small first slice; Medium full track.
- First slice: typed per-actor notification preference model and persistence invariants, including mandatory-notice fail-safe defaults, channel/category validation, UTC audit timestamps, and focused tests. No endpoint or UI changes.

## 2. Returns inspection and disposition hardening

- Owning modules: `Returns`; later approved contracts to Payment/Inventory/Fulfillment.
- Commercial value: strengthens post-sale operations, partial RMA handling, auditable inspection, refund/restock instructions, and retry safety.
- Expected areas: `Modules/Returns` domain/application/infrastructure and module-local migration; later `Host/Returns` and `app/returns`.
- Database: return inspection/disposition evidence and transition data in the `returns` schema.
- Frontend: not needed for a domain-first slice; needed later for seller/admin workflows.
- Shared navigation/Host wiring: none for a domain-first slice; existing routes/navigation already exist.
- P08 conflict risk: low.
- Size: Medium.
- First slice: formalize inspection/disposition transition invariants and idempotency with focused module tests, without cross-module writes.

## 3. Wallet mixed-tender checkout

- Owning modules: Wallet plus Cart/Checkout, Payment, and Order contracts.
- Commercial value: closes the explicit mixed-tender gap and allows wallet balance plus external payment.
- Expected areas: Wallet, Cart, Payment, Order, module contracts, storefront checkout/payment API and UI.
- Database: wallet holds/reservations and payment/order snapshot changes across several module schemas.
- Frontend: required.
- Shared navigation/Host wiring: navigation likely unnecessary, but shared contracts, endpoint composition, and checkout surfaces are required.
- P08 conflict risk: low direct Content overlap but high integration/conflict surface.
- Size: Large.
- First slice: architecture/contract proof for reservation, idempotency, compensation, and snapshot semantics before implementation.

## 4. Full variant matrix and faceted-search continuation

- Owning modules: Catalog, with Storefront/Search consumers.
- Commercial value: improves complex-product sellability and discovery.
- Expected areas: Catalog domain/application/infrastructure, Admin product workspace, shared Admin client, storefront listing/filter UI.
- Database: Catalog variant combinations, attribute/facet configuration, and Catalog migration/model snapshot.
- Frontend: required for useful delivery.
- Shared navigation/Host wiring: no new navigation expected, but shared Admin workspace/client and canonical grid/tree systems are involved.
- P08 conflict risk: low direct Content overlap, medium overall because Catalog/Admin shared surfaces are active high-churn areas.
- Size: Large.
- First slice: isolated Catalog-domain combination validation tests only; defer UI and shared client edits.

Selection: candidate 1 best satisfies commercial value, explicit repository gap, existing module composition, isolated validation, and a zero-shared-hotspot first slice.
