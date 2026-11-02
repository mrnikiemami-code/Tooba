# RESULT — TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248-R1

Status: **PASS** within issued software/deploy scope  
Decision: **SOFTWARE_COMPLETE_DEPLOYED_PROVIDER_UNVERIFIED**  
NOT Architect ACCEPT, NOT live Tipax ACCEPT, NOT PSP/paid certification

Parent: `TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248`  
Base-SHA: `3639acca97cee377f58be3ab7e92784bb1fced97`  
Stop-State: `STOP_FOR_ARCHITECT`  
automaticNextImplementationTask: `NONE`

## Summary

All remaining carrier software for quote evidence, stale revalidation, shipment lifecycle, DataProtection key persistence, classification, authoring unconfigured copy, and canonical FE3000→Host5088 deployment is landed. The user-deferred item is actual Tipax communication with real credentials. No test account was requested, searched, or invented. Manual/Free remain available. CarrierApi is installed and unconfigured.

## Landed

- Order accepted shipping groups now store provider/account/config version/service, quote-input identity, amounts/currency/components, issued time, local freshness, and provider reference only when the provider actually returned one. All-or-nothing CHECK. Historical Order reads do not call Shipping/carrier.
- Checkout revalidates live vs stored carrier quote identity/amounts/freshness (`FreshUntilUtc < now`) before financial acceptance. Packaging/tax stay separate; carrier-collect stays out of payable.
- Provider-neutral `ICarrierShipmentProvider` + owner-scoped `shipping.carrier_shipment_operations` with unique `operation_key` replay (23505). Eligibility is Order.Contracts facts only; zero network when unconfigured/ineligible. Timeout → UNKNOWN; Tipax create timeout reconciles `POST /api/OM/v4/Tracking/BySecondaryTraceCode/{code}` then UNKNOWN if absent.
- Tipax V4 shipment adapter: `POST /api/OM/v4/OmOrder`, `POST /api/OM/v4/Tracking/ByTrackingCode`, `PUT /api/OM/v4/Parcels/Cancel/{trackingCode}`. Label/PDF is **explicitly unsupported** (no documented V4 label route used; `CanLabel=false`; GET label returns `shipping.carrier.shipment.unsupported`).
- DataProtection: `PersistKeysToFileSystem(App_Data/dataprotection-keys)`, `SetApplicationName("Tooba")`. Backup/deploy must copy that folder with the Host content root.
- Authoring FA copy «اتصال شرکت حمل تنظیم نشده است»; admin/seller shipment panels on existing order screens. No storefront shipment actions.
- Classification restored TB-220 publication rows and classified 248 accounts + 248-R1 shipments: **54 requests / 93 routes / 49 validators / 5 no-validator**.
- Canonical migrations applied with official runner after private backup.

## Verification

- Shipping focused filter (shipment directory/Tipax shipment/catalog/classification/owner+context/captured quote/Tipax rate/structure/composition/persistence): **123/123**.
- Order focused: evidence + organization guard + `SameCarrierQuote` freshness: **21/21**.
- Frontend `carrier-unconfigured-248-r1.test.ts` + `seller-api.test.ts`: **11/11**.
- `npm run typecheck`: exit 0.
- `npm run build`: exit 0 (Next.js 15.5.23).
- Host Release publish: exit 0.

## Live / runtime

Canonical FE `http://127.0.0.1:3000` PID **4072** → Host `http://127.0.0.1:5088` PID **8972** `publish-primary` SingleStore / store-alpha / `tooba_alpha`. `/health` 200. Unauthenticated `GET /v1/admin/shipping-services/carrier-accounts` and `GET /v1/admin/shipping-services/shipments` **401**. Authoring route 200.

Usable authoring URL (unchanged draft, no Save/publish/PUT/Order/payment/shipment):

`http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`

## Migrations

Private backup: `D:\temp\tb248r1-backup\tooba_alpha-pre-tb248r1.dump` 2,685,193 bytes SHA256 `A818A295CEF1BB688A470D0C5C61C90C348367F38538F8ABD41E806D74A67971`.

Runner `plan` then `apply --tenant store-alpha` (Development, edition SingleStore, connectionRef tenant-alpha):

- Shipping pending 2 → current `20261102180000_CarrierShipmentOperations` (includes `20261102170000_CarrierConfiguration`).
- Order pending 1 → current `20261102180000_AddAcceptedCarrierQuoteEvidence`.

Readback: `shipping.carrier_shipment_operations` plus 248 account/secret/reference/mapping/binding tables exist.

## Provider operations unavailable from the provider (documented)

- Tipax V4 **label/receipt/PDF download** — no official V4 label route is used; the capability is explicit `CarrierShipmentUnsupported`, not simulated.

## Remaining (external only)

LIVE TIPAX CONNECTION/ACCEPTANCE remains deferred by explicit user decision (no test account now). No software/UI/persistence/migration/deployment gap is left for that deferral.

## Stop

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
