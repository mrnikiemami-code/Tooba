# TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1 — RECEIPTS

## Runtime

- Host PID 30328 `tb244-publish-primary` → `http://127.0.0.1:5088` /health 200, SingleStore/store-alpha/`tooba_alpha`
- FE PID 28396 `tb244-fe` → `http://127.0.0.1:3000` /api/health 200
- Buyer session cookie retained from 244 (`tb244-buyer-jar.txt`); `/api/auth/me` 200 → `01a0996c-1b0e-7000-a8bd-5fd78f9b9e4f`
- Cart `01a17b2d-e219-7000-b3b2-5bcdbe6810c5` Active version **48** qty **18**; 0 checkouts

## Compensation script

- `D:\temp\tb244r1-compensate.cjs` → `D:\temp\tb244r1-compensation.json`
- Admin bearer via Identity login `admin@local` (task TEMP; not committed)

### Owning-API outcomes (from compensation JSON)

| Step | HTTP | Note |
| --- | --- | --- |
| Clear product weights (3) | 200 each | clear while still bound |
| Unbind categories daf5/dba9 | 204 | |
| Patch unit → `g` | 200 | |
| Clear fulfillment ×4 | 200 | responsibility null |
| Replace rate-rows | 200 | sole row `01a16f4e-1c45…` [1000,2000) |
| Publish draft `01a16eeb…` | 200 | snapshot `01a185cb-6909-7000-87c0-7aa2c63ee8bc` selectionVersion **10** |
| Projection after restore | 200 | 4 groups, all `storefront.captured_preview.unassigned` |
| Payment-draft after restore | **400** | `storefront.captured_selection.unavailable` (guessed selection not payment-ready) |

## DB verify (`tooba_alpha`)

```
offer.offers (4 cart offers): fulfillment_responsibility NULL, fulfillment_shipping_type_id NULL
catalog.attribute_definitions demo_attr_mobile_weight: unit = g
category_attribute_bindings for daf5/dba9 + that definition: 0
product_attribute_values for 3 cart products + that definition: 0
authoring_rate_rows on draft 01a16eeb…: 1 row [1000,2000)
cart_shipping_drafts: selection_revision 1 (stale disk groups with accepted_snapshot still present)
cart_shipping_groups: 2 rows has_accepted=true (NOT presented; payment-draft refuses)
checkouts for cart: 0
```

## Authoritative investigation (no invented values)

- No remaining fulfillment on the four offers
- No shipping weight attribute values on the three cart products for `demo_attr_mobile_weight`
- No other `_weight` / shipping-weight authority found for these SKUs in the compensated catalog path
- Rate authority after restore: only [1000,2000) — cannot evaluate without group weight
- Therefore **no lawful green path**; decision table in RESULT.md

## Constraints honored

- No screenshots / no Order / no StartPayment / no cart delete / no business SQL / no seed / no new ports
- Checked-in gates OFF; local primary config unchanged
- User LOCAL / unrelated profiles preserved
- TB185 paused / default OFF / PendingAllocation
