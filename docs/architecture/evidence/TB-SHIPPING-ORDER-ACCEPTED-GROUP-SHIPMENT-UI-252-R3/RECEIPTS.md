# RECEIPTS — TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R3

## Identity
- Bridge-Task-Row: `89db09f3-c8c1-440f-905e-e8e1f928e5df`
- Expected-Base: `36845985ba2d9a931b65bcec8e936ee23154f674`
- Code-SHA: PENDING_COMMIT
- Evidence-SHA: PENDING_COMMIT

## Runtime
- FE: `http://127.0.0.1:3000` production Next (`next start -p 3000 -H 127.0.0.1`) PID 32864
- Host: `http://127.0.0.1:5088` publish-primary PID 13288
- DB: SingleStore / store-alpha / `tooba_alpha`
- DataProtection keys preserved
- CapturedPreview local ON (Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7); checked-in OFF
- Host health 200; FE admin order route 200

## Tests
- Order `AcceptedGroupTrustedOriginMetadataTests` + binder directory wiring + transport routing: 12/12
- Fulfillment `AcceptedGroupPostTrustedMetadataCreateTests` 3/3
- `npx tsc --noEmit` 0
- `npm run build` 0 (`BUILD_EXIT=0`)

## Live authorized read (no mutation)
- GET `/v1/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9` → 200 after redeploy
- shippingGroups=2 method=پیشتاز Manual; eligible True/False; recipient preserved; no order mutation

## Review URL
`http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9`
