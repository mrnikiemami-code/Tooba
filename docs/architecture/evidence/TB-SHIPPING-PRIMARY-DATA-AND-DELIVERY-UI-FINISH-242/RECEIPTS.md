# TB-242 receipts (no secrets)

## Runtime
- Host: D:\temp\tb242-primary\Tooba.Host.exe PID45304 URL http://127.0.0.1:5088
- FE: D:\temp\tb242-fe next start -p 3000 PID41116
- Ready: edition=SingleStore
- Chrome handoff: D:\temp\tb242-chrome CDP9351
- Frozen user profile: D:\temp\tb241-chrome CDP9350 (untouched)
- Marketplace start backup: D:\temp\tb242-marketplace-start-backup.ps1
- SingleStore start: D:\temp\tb242-singlestore-start.ps1
- Private DB backup: D:\temp\tooba_alpha_tb242_pre.bak

## Migrations applied (store-alpha / tooba_alpha)
- shipping: 20261028201416_ReceivingSlotDuration
- order: 20261028200100_AcceptSlotDurationFormat

## HTTP proof artifacts (private temp, not secrets-free enough for git if credentials present)
- D:\temp\tb242-flow4.log / tb242-sel.json / tb242-draft.json / tb242-proj.json
- Test buyer email pattern tb242-*@example.invalid (credentials only on private temp disk)

## Tests
- typecheck 0
- storefront-shipping-continue-state + captured-selection 27/27
- test:critical-storefront 19/20 (pre-existing storefront-landing-blocks.tsx:286 bg-white)
