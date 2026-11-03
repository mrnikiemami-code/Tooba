# RECEIPTS — TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R1

## Identity
- Bridge-Task-Row: `438a816d-d101-470b-8d39-36396e746fe2`
- Expected-Base: `063e100c8f3f60b8d08b08f55b25714d3e551ef6`
- Code-SHA: PENDING_COMMIT
- Evidence-SHA: PENDING_COMMIT

## Runtime
- FE: `http://127.0.0.1:3000` production Next (PID recorded at handoff)
- Host: `http://127.0.0.1:5088` publish-primary `Tooba.Host.exe`
- DB: SingleStore / store-alpha / `tooba_alpha`
- DataProtection: `artifacts/publish-primary/App_Data/dataprotection-keys` preserved
- CapturedPreview local env (process): Enabled=true, SelectionEnabled=true, CheckoutEnabled=true, AvailableDateCount=7
- Checked-in gates: unchanged OFF

## Tests
- Order `AcceptedGroup*` 16/16
- FulfillmentValidatorBehaviorTests (seller shape) focused green
- FE mount `admin-create-shipment-modal.mount.test.ts` 3/3
- FE mount carrier suite 10/10
- `npx tsc --noEmit` 0
- `npm run build` 0

## Live authorized read (no mutation)
- Actor: `01a036c2-970e-7000-8eb7-94bf5cc2d8db` via `X-Tooba-Dev-Actor-User-Id` (Development)
- GET `/v1/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9` → 200
- shippingGroups=2; transportMode=Manual; methods پیشتاز; dates 2026-11-23 & 2026-11-30; windows 09:00–10:00; owner Store; fees 125000 IRR
- Group A createParcelEligible=true; Group B blocked remaining packed qty
- No create_shipment / pack / dispatch / payment calls on this order

## Touched surfaces (high level)
- Order.Contracts: `AcceptedGroupParcelCreateAuthority` + ServiceCode/OptionCode on facts
- Order.Application binder/orchestrator/detail eligibility + transportMode
- Fulfillment.Application seller create bind + ShippingGroupId validator
- FE modal inFlight + panel CarrierApi gate + mount tests + error map

## Review URL
`http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9`
