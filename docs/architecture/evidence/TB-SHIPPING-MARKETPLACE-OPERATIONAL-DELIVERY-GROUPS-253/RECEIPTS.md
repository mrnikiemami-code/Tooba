# RECEIPTS — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253

## Claim
- Bridge row `ae9668d1-8cbc-4df0-b0ec-9303d0a4368a` Claimed via `/api/tasks/next` for worker `tooba-worker-01`.
- Expected base `e33517eb45b319ee77f53efb3256b91ad4451b4e` matched HEAD at claim.

## Runtime
- Host: `http://127.0.0.1:5088` artifacts/publish-primary `Tooba.Host.exe` PID 6472
- FE: `http://127.0.0.1:3000` `next start` PID 29332
- Edition SingleStore / tenant store-alpha / connectionReference tenant-alpha / DB tooba_alpha
- DeploymentId `store-alpha-canonical-tb253`
- CapturedPreview local ON (Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7); checked-in OFF
- DataProtection: `artifacts/publish-primary/App_Data/dataprotection-keys` preserved (KEYS_AFTER=True)

## Focused proof
- Order.Tests `MarketplaceOperationalDeliveryGroupAuthorityTests` **8/8**
- FE focused (items-shipping + 252 + operational mount) **25/25**
- `npm run typecheck` 0
- `npm run build` 0
- Host `dotnet publish` Release → publish-primary exit 0 (after Host stop)

## Canonical read-only handoff
- Actor: `01a036c2-970e-7000-8eb7-94bf5cc2d8db` via `X-Tooba-Dev-Actor-User-Id`
- GET `/v1/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` → 200
- shippingGroups=3 ownerKinds=Store,Store,Store
- operationalDeliveryGroups=3 all `isCrossSellerMarketplace=false` with Store separate reason
- FE `/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` → 200
- No create/pack/dispatch/cancel/payment mutation on user order

## Relational matrix
- Disposable PostgreSQL §B: **not executed** (PARTIAL)

## Review URL
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf
