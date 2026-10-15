# TB-SHIPPING-FULL-OPERATIONS-077 — Development fixture ledger

Target: Development only. `http://127.0.0.1:5088` (`__platform-commerce` → `dev-singlestore`,
`store-alpha`, `tenant-alpha`), database `tooba_alpha` on `127.0.0.1:5432`, UI
`http://127.0.0.1:3000/admin/shipping-services`. No staging or production host was contacted.

## Rows created by this task

| Packaging policy id | Service / option | Destination scope | Created as | Final state |
| --- | --- | --- | --- | --- |
| `01a13ec9-4e2a-7000-a5cb-428d28a26e8a` | `tipax` / `express` | IR / Fars `24e00388-c216-531c-a3ca-11f382508760` | fee `12000.00` IRR, payer `CollectOnDelivery`, active | fee `15000.00` IRR, payer `Prepaid`, **`isActive=false`** |

The row was created through the real browser UI (dialog opened from the `tipax` service row action),
edited once (`12000` → `15000`, `CollectOnDelivery` → `Prepaid`), toggled inactive, toggled active, and
finally returned to **INACTIVE** so no ACTIVE synthetic fixture remains. The destination area (Fars)
was chosen precisely so the row could not collide with the pre-existing Alborz fixtures.

## Rows read but never modified

| Packaging policy id | Service / option | State before and after |
| --- | --- | --- |
| `01a13e7c-cfde-7000-98cc-f79246cf97b0` | `tipax` / `express` (Alborz) | fee `15000.00` IRR, `Prepaid`, `isActive=false`, `updatedAt 2026-10-15T08:38:28Z` — unchanged |
| `01a13e7b-e198-7000-9753-f417791d20cb` | `tipax` / `standard` (Alborz) | fee `15000.00` IRR, `Prepaid`, `isActive=false`, `updatedAt 2026-10-15T07:34:28Z` — unchanged |

Rate, coverage and operating-schedule fixtures were not touched at all: this task never wrote to a
rate table and the browser proof only exercised the packaging routes.

## Final verification

`GET /v1/admin/shipping-services/tipax/packaging-policies` after the proof returned all three rows with
`isActive=false`, and `GET /v1/admin/shipping-services/post/packaging-policies` returned an empty
`packagingPolicies` set (the explicit "zero packaging fee" default). No ACTIVE synthetic row created by
this task remains and no row was deleted.
