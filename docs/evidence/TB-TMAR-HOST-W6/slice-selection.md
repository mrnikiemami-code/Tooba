# Slice selection — TB-TMAR-HOST-W6

- Host file: Admin/ShippingServiceEndpoints.cs — Create/Update/Deactivate/EnsureSeed (+ EnsureSeed callers)
- Owner module: Fulfillment
- DbContext: FulfillmentDbContext (ShippingServices / Translations / Options)
- Write effects: insert/update/deactivate catalog rows; idempotent seed of 5 default services
- Business decisions: code uniqueness, language validity, seed language pick (fa/en), option replace-on-update
- Transaction: single SaveChangesAsync per operation; no BeginTransaction; module-local
- Callers: Admin shipping-services routes; List/ensure-seed; AdminOrderOperations shipping-methods tree; StorefrontShippingComposer LoadEligibleMethods
- Target CQRS: Create/Update/Deactivate/EnsureShippingCatalogSeed Commands → Handlers → IShippingServiceDirectory → ShippingServiceDirectory
- Baseline shrink: remove Admin/ShippingServiceEndpoints.cs from tmar-host-write-files.json
- Safer than Seller/ProductWorkspace (ownership clearer); safer than Order-adjacent (no CROSS_CONTEXT_CONSISTENCY)
