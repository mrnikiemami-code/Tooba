# RECEIPTS — TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252

## Identity
- Bridge-Task-Row: `571d666e-631b-42c4-abf6-412439f89a5e`
- Expected-Base: `7786d4bc0898b03fcdef6a7bbcba2a1c6950eb82`
- Code-SHA: 068fb09fc059e3ae1856a0e0829102499290e61c
- Evidence-SHA: 1d0631e3c84e4ddfa7118b66436f252d3fc17022

## Runtime
- FE: `http://127.0.0.1:3000` production Next (restarted after build)
- Host: `http://127.0.0.1:5088` publish-primary `Tooba.Host.exe` (PID recorded at handoff)
- DB: SingleStore / store-alpha / `tooba_alpha`
- DataProtection: `artifacts/publish-primary/App_Data/dataprotection-keys` preserved
- CapturedPreview local env: Enabled=true, SelectionEnabled=true, CheckoutEnabled=true, AvailableDateCount=7
- Checked-in gates: unchanged OFF

## Tests
- `AcceptedGroupShipmentCreateBinderTests` 7/7
- `FulfillmentShippingGroupExecutionTests` 10/10
- `node --test app/admin/admin-order-accepted-group-shipment-252.test.ts` 4/4
- `npm run typecheck` 0
- `npm run build` 0

## Live authorized read (no mutation)
- Actor: `01a036c2-970e-7000-8eb7-94bf5cc2d8db` via `X-Tooba-Dev-Actor-User-Id` (Development)
- GET `/v1/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9` → 200
- shippingGroups=2; methods پیشتاز; dates 2026-11-23 & 2026-11-30; windows 09:00–10:00; owner Store; fees 125000 IRR
- Group A createParcelEligible=true; Group B blocked remaining packed qty
- No create_shipment / pack / dispatch / payment calls on this order

## Touched surfaces (high level)
- Order Application: `AcceptedGroupShipmentCreateBinder`, orchestrator CreateShipment, AdminOrderDetail models/composer
- Order Endpoints: error codes + resx
- Fulfillment Domain: method mismatch guard
- FE admin: panel group cards, create modal read-only, ops menu guard, admin-api/ops shippingGroupId, error map

## Review URL
`http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9`
