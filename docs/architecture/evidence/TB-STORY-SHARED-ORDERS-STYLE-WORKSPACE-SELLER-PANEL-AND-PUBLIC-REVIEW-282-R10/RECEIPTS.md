# TB-282-R10 RECEIPTS

## Claim
- Bridge row `4cf86d28-f9b3-46c2-9815-b07056edc11f`
- Expected-Base `06e3bd558f0b9a2f323f9dbab596188580edb798` (ancestor OK)
- Claim HEAD `2cafe7af626e6822ecbd99e73875df59459110cf`

## E diagnosis (read-only)
- Seller story `01a04246-b309-7000-a922-4f9edfc5c909` GET 200 Draft, item URL `/images/stories/1.jpg`, no MediaAssetId.
- Before fix: contentReady true, actionCodes included submit; submit authorization gate → MediaForbidden 403.
- After Host refresh: contentReady=false; actionCodes=`update,add_item,delete`.

## Tests
- `dotnet test Tooba.Host.Tests --filter Story282R10|StoryMarketplace281DomainTests` → 8/8
- `tsx --test story-management-282r10.mount.test.ts story-schedule-time.test.ts` → 4/4

## Serve
- Host `http://127.0.0.1:5088` TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1 Development; `/health` ok; `/health/ready` 200
- Next `http://127.0.0.1:3000` next dev (build prerender blocked); vendor/admin stories + home 200

## Zeroes
- migration/DDL/seed/grant/live Story-Media write: ZERO
