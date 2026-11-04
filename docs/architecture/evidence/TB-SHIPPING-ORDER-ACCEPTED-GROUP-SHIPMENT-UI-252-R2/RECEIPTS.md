# RECEIPTS — TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R2

## Identity
- Bridge-Task-Row: `ddb08716-6039-45cc-9fd2-5fbd15a66e34`
- Expected-Base: `2c73cd938d1b948390c9b0666636171032cc449c`
- Code-SHA: PENDING_COMMIT
- Evidence-SHA: PENDING_COMMIT

## Runtime
- FE: `http://127.0.0.1:3000` production Next (`next start -p 3000 -H 127.0.0.1`)
- Host: `http://127.0.0.1:5088` publish-primary `Tooba.Host.exe`
- DB: SingleStore / store-alpha / `tooba_alpha`
- DataProtection: `artifacts/publish-primary/App_Data/dataprotection-keys` preserved (KEYS_AFTER=True)
- CapturedPreview local env (process): Enabled=true, SelectionEnabled=true, CheckoutEnabled=true, AvailableDateCount=7 (`D:\temp\tb252r2-gates.json`)
- Checked-in gates: unchanged OFF

## Tests executed
- Fulfillment `AcceptedGroupPostTrustedMetadataCreateTests` 3/3 (directory create + null refuse + builder missing name)
- Order `AcceptedGroupCreateShipmentTransportRoutingTests` 4/4 (CarrierApi refuse; trusted Manual bind; source forward; validator accept)
- FE mount `admin-create-shipment-modal.mount.test.ts` 3/3
- `npx tsc --noEmit` 0
- `npm run build` 0
- Host `dotnet publish` Release → publish-primary exit 0

## Live authorized read (no mutation)
- Actor: `01a036c2-970e-7000-8eb7-94bf5cc2d8db` via `X-Tooba-Dev-Actor-User-Id` (Development)
- GET `/v1/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9` → 200
- shippingGroups=2; transportMode=Manual; methods پیشتاز; dates 2026-11-23 & 2026-11-30; windows 09:00–10:00; owner Store/store-alpha (hidden in FE cards); fees 125000 IRR
- Group A createParcelEligible=true; Group B blocked remaining packed qty
- No create_shipment / pack / dispatch / payment calls on this order

## Touched surfaces (high level)
- Order.Contracts trusted metadata builder
- Order.Application binder/orchestrator + Application metadata edge
- Fulfillment.Application seller create trusted metadata forward
- Fulfillment.Tests real directory Post create proof
- FE compact modal/panel Jalali + ownership hide + one inline error + error-map

## Review URL
`http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9`
