# RECEIPTS — TB-SHIPPING-ORIGINAL-STORE-COMPLETE-HANDOFF-243

## Runtime
- Host: D:\temp\tb243-primary\Tooba.Host.exe PID8856 URL http://127.0.0.1:5088 DeploymentId=store-alpha-tb243-primary Edition=SingleStore
- FE: D:\temp\tb243-fe next start -p 3000 PID46476 → Host 5088
- DB: tooba_alpha / tenant store-alpha
- Gates local: Tooba__StorefrontCapturedPreview__{Enabled,SelectionEnabled,CheckoutEnabled}=true AvailableDateCount=7

## Config before→after (authorized)
- Draft 01a16eeb weekly: slot_duration NULL→60 (rev 2→3) then history 60→120→60; final restored 60, published snapshot 01a1858d-af5e-7000-bbe8-8203037ab04f
- Offer 964ac1b4-b39e-4f29-9d41-e4402fdf21f1: fulfillment Platform + shippingTypeId 01a16f43-6b4a-7000-8670-4c16e14c6345
- Fulfillment one-level post:standard deactivated (option is_active=false) so catalog binding is post/NULL
- Backup CSVs: D:\temp\tb243\backup-weekly-before.csv, backup-draft-before.csv

## Order proof
- Checkout 01a1858c-1dfe-7000-91ae-0b5c08d243a2 Payment 01a1858c-1fb7-7000-9868-16a96bae67b5 manual Pending 2749545 IRR
- SellerOrder TB-20261029024514-01-43b144
- selectedDeliveryTimeWindow 14:00-15:00; history snapshot 01a18584-6c2b-7000-9196-e02f06c66d16 draft 01a16eeb sourceRevision 3 formatVersion 2 rateRow 01a16f4e-1c45-7000-a8d4-696c0c388a3f
- Private creds: D:\temp\tb243\order-buyer-cred.temp.json (not in git)

## Review handoff
- Email tb243-review-1793241974470@example.invalid userId 01a1858d-06c8-7000-921b-263ef74a418f
- Cart 01a1858d-06ee-7000-a130-e5f8824ab667 v4 unsubmitted; /api/auth/me 200; /fa/shipping 200
- Private: D:\temp\tb243\review-cred.temp.json

## Checks
- tsc --noEmit clean
- storefront-shipping-continue-state.test.ts 14/14
- storefront-shipping-format.test.ts 4/4
- Order.Infrastructure Release build 0 errors
