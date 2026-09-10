# TB-P09-T022-R2 — rebuild customer UI proof

Script: `docs/evidence/TB-P09-T022/_r2_rebuild.mjs`  
Raw Host result: `docs/evidence/TB-P09-T022/r2-rebuild-raw.json`

## Host sequence (pre-dispatch)

Checkout: `01a089a6-154f-7000-9586-bc1463ac6c8b`

1. Cancel active package `MP-01A089A61CF4` → cancel **200**
2. Create new consolidated package with tracking `CENTRAL-T022-R2-REBUILT` → create **200**
3. Owned customer fulfillments → **200**

| Field | Value |
| --- | --- |
| New packageNumber | `MP-01A089AA19B2` |
| preferredCustomerTrackingReference | `CENTRAL-T022-R2-REBUILT` |
| preferredCustomerTrackingPackageNumber | `MP-01A089AA19B2` |

## Customer UI

Same route as `r2-customer-ui-runtime.md`, after reload with owned session:

- Primary `کد پیگیری` shows **`CENTRAL-T022-R2-REBUILT`**
- Cancelled `CENTRAL-T022-R2` / prior package is **not** primary
- Member `TRK-…` tracking remains listed
- No stale primary after reload

`USER_VISUAL_ACCEPTED=NO`
