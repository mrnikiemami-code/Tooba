# RECEIPTS — TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283

| Item | Value |
|---|---|
| Base SHA | `33c69baffdd8d0280de0c8a6edd1c625e0c4396e` |
| Plan commit | `719167d983a70506739d720cc6249cdbb158a3f0` |
| Code SHA | `611f97a85ec7c2e464e4f649820992202ca3754c` |
| TASK SHA256 | `22b8d393d6c582021f80b4a15147c47b7fdcb2123992a9a47da68757906586df` |
| Plan SHA256 | `175afa0d995cdc4850d0654fd74e134506c780a200060169d02758850c99c06d` |
| Next BUILD_ID | `BBwBaSOakfco4OUr4yzrP` |
| Next URL | `http://127.0.0.1:3000` |
| Host | PID `34164`; DLL `E301B13E558899F59F19505EB7059D3BFC65518BD1DAD035D223F0DDAB321019` |
| OpenSpec | `openspec/changes/seller-offer-safe-edit-283/`; strict validation passed |
| Review gate | FAIL (open REQ-01/REQ-03; W1/W5 PARTIAL) — recorded, not hidden |

## Commands

```powershell
NODE_PATH=C:\Users\User\OpenCode-UI-Lab-20261122-064208\node_modules
node C:\Users\User\OpenCode-UI-Lab-20261122-064208\node_modules\@playwright\test\cli.js test --config docs\architecture\evidence\TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283\browser\playwright.config.cjs --reporter=list
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SellerOfferSafeEdit283RelationalTests|FullyQualifiedName~OfferSkuPatch283RelationalTests"
dotnet test src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~Draft_sku_patch"
npx tsc --noEmit
npm run build
node docs/ai/delivery/check-readiness.mjs docs/ai/tasks/TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283 --stage review --task-sha 22b8d393d6c582021f80b4a15147c47b7fdcb2123992a9a47da68757906586df
```

## Evidence hashes

| File | SHA256 |
|---|---|
| `browser/assertions.json` | `791f97a33cfab31867e43a082651aa9da173c4cc94a67c1c4de7b880623b611a` |
| `browser/run-final.log` | `6b1a0cdcc082d3bbeb7e895001916582612d68f9fbef11163fbcaadb6db15935` |
| `postgresql.log` | `c124e5d1f331b90e2a4c2388690c46b1ed7ea1f3f4118054443fcbb356b01670` |

## Safety

No migration/schema/seed/grant/reset, no live offer/price/stock/reservation write, no provider/payment/notification, no screenshot, no subagent.
