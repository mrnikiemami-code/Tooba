# RECEIPTS — TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248-R1

## Identity

- Task: `TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248-R1`
- Parent: `TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248`
- Channel: `tooba-main` / worker `tooba-worker-01`
- Bridge-Task-Row: `42cd45f1-f5ae-497f-ba98-4b2cb4641d3a`
- Expected-Base / Base-SHA: `3639acca97cee377f58be3ab7e92784bb1fced97` (`origin/main` at claim)
- Official Tipax: `https://document.tipaxapi.org/#v4-docs` / `https://omtestapi.tipax.ir/swagger/v4/swagger.json`
- Shipment paths: `POST /api/OM/v4/OmOrder`, `POST /api/OM/v4/Tracking/ByTrackingCode`, `POST /api/OM/v4/Tracking/BySecondaryTraceCode/{code}`, `PUT /api/OM/v4/Parcels/Cancel/{trackingCode}`
- Label: unsupported (no V4 label route; GET `/v1/{admin|seller}/shipping-services/shipments/{id}/label` → `shipping.carrier.shipment.unsupported`)

## Checks actually run

- `dotnet test Tooba.Shipping.Tests --filter FullyQualifiedName~ShippingCarrierShipmentDirectoryTests|TipaxV4ShipmentProviderTests|ShippingErrorCatalogAndResourcesTests|ShippingPersistenceModelTests|ShippingRequestClassificationTests|ShippingAuthoringContextEndpointTests|ShippingAuthoringOwnerEndpointTests|ShippingCapturedCarrierQuoteLookupTests|TipaxV4RateProviderTests|ShippingModuleStructureGuardTests|ShippingModuleCompositionGuardTests` → Passed **123/123**
- `dotnet test Tooba.Order.Tests --filter FullyQualifiedName~ShippingGroupAcceptedEvidenceTests|OrderInfrastructureOrganizationGuardTests|CheckoutDirectoryCarrierQuoteFreshnessTests` → Passed **21/21**
- `npx tsx --test features/admin-shipping-authoring/carrier-unconfigured-248-r1.test.ts app/vendor-panel/seller-api.test.ts` → **11/11**
- `cd src/frontend && npm run typecheck` → 0
- `cd src/frontend && npm run build` → 0 (Next.js 15.5.23)
- `dotnet publish Tooba.Host -c Release -o D:\temp\tb248r1-host-out` → 0

## Controlled contract (not live)

- Unconfigured/missing username: `shipping.carrier.unconfigured`, zero outbound HTTP.
- Missing Order facts: `shipping.carrier.shipment.not_eligible`, zero network.
- Duplicate create `operation_key`: 23505 winner replay, at most one outbound create.
- Create timeout: Tipax secondary-trace reconcile then UNKNOWN; no blind retry.
- Stale/expired/changed quote identity: `SameCarrierQuote` false; acceptance throws captured conflict before financial writes.
- Label: `CarrierShipmentUnsupported`.
- Classification: 54 requests / 93 routes / 49 validators / 5 no-validator.
- FA unconfigured copy: «اتصال شرکت حمل تنظیم نشده است».

## Live boundary

- No Tipax username/password used. No real provider HTTP. No real shipment create.
- Canonical Host5088 PID **8972** `publish-primary` `/health` 200; FE3000 PID **4072** `/fa` 200 and authoring 200.
- Unauthenticated Host `GET /v1/admin/shipping-services/carrier-accounts` 401; `GET /v1/admin/shipping-services/shipments` 401.
- Draft `01a17d0d-e033-7000-af43-49a1daa28822` not mutated. User cart/checkout/payment not submitted.

## Data / preservation

- Private `pg_dump -Fc tooba_alpha` → `D:\temp\tb248r1-backup\tooba_alpha-pre-tb248r1.dump` 2,685,193 bytes SHA256 `A818A295CEF1BB688A470D0C5C61C90C348367F38538F8ABD41E806D74A67971`.
- Applied via `Tooba.MigrationRunner apply --tenant store-alpha` (Development, SingleStore, tenant-alpha/`tooba_alpha`):
  - Shipping `20261102170000_CarrierConfiguration` + `20261102180000_CarrierShipmentOperations`
  - Order `20261102180000_AddAcceptedCarrierQuoteEvidence`
- Tables present: `carrier_accounts`, `carrier_secret_materials`, `carrier_reference_values`, `carrier_destination_mappings`, `carrier_method_bindings`, `carrier_shipment_operations`.
- DataProtection keys: Host content-root `App_Data/dataprotection-keys`, application name `Tooba`. Preserve that folder across Host content-root copies.
- Checked-in activation defaults remain OFF. Local Host keeps StorefrontCapturedPreview ON as previously running (`store-alpha-canonical-tb244`).
- Marketplace owners preserved in contracts/endpoints; canonical runtime stays SingleStore/store-alpha. No alternate port/account/cart/DB.
