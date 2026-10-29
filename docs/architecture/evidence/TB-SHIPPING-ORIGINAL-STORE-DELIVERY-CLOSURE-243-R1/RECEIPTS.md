# TB-SHIPPING-ORIGINAL-STORE-DELIVERY-CLOSURE-243-R1 RECEIPTS

## Runtime
- Host: `D:\temp\tb243-primary\Tooba.Host.exe` PID35260 URL `http://127.0.0.1:5088` DeploymentId=`store-alpha-tb243-primary` Edition=SingleStore
- FE: PID46476 `D:\temp\tb243-fe` → 3000 (unchanged source 7ea79556f this slice)
- DB: tooba_alpha / tenant store-alpha
- Host DLL SHA256: D09E6E4DF5720483995603EBE0F0CC06E6B62FB4E574BDAF87E710B053755626

## Weight
- Definition: `demo_attr_laptop_weight` / `01a05387-d58c-7000-8886-8ec4f64b05c8` / kg / active
- Product canonical `1.4` → 1400g via CatalogShippingWeightGrams
- Live projection freight `120000.00` on laptop offer `964ac1b4-…` (1000–2000g tier)
- Tests: CatalogShippingWeightGramsTests 9/9

## Binding
- Before R1: post options express+standard inactive (243 workaround)
- After: PUT `/v1/admin/shipping-services/shipping-methods/one-level/post:standard/activation` `{isActive:true}` → 200 changed
- DB: shipping_methods post:standard t; shipping_service_options standard t; express f
- Planner: null-option probe + active options; Null_option_and_named_option_bindings_both_survive_when_selected PASS

## Slots (draft 01a16eeb)
- set60a publish rev5 → proj60 slotCountFirst=8 (09–10…16–17)
- set120 publish rev6 → proj120 slotCountFirst=4 (09–11,11–13,13–15,15–17)
- set60b publish rev7 → proj60restore slotCountFirst=8
- Left published at 60

## Concurrent StartPayment
- Cause: `pk_checkout_abuse_customer_locks` 23505 inside ambient TransactionScope → 25P02 → 500
- Fix: ON CONFLICT upsert in CheckoutAbuseGate.AcquireCustomerLockAsync
- Live: twin same idempotencyKey → 200 + 409 cart.version.conflict (no 500); replay 200 same checkoutId/paymentId replayed=true
- checkout `01a185a2-2e5a-7000-a3b4-eee8c3710158` payment `01a185a2-2ffc-7000-88ad-bfb7892fed5d`

## Preservation
- Original Order GET `01a1858c-1dfe-7000-91ae-0b5c08d243a2` 200 date 2026-11-02 post
- Review cart `01a1858d-06ee-7000-a130-e5f8824ab667` Active v4; auth/me 200
- CDP9350/9351 not enumerated

## Proof artifact
- `D:\temp\tb243r1-proof.json` / `D:\temp\tb243r1-restore-standard.json`
