# Visual smoke — TB-P09-T022

`USER_VISUAL_ACCEPTED=NO` — Worker must not claim human visual acceptance.

## API-backed visual checks (runtime matrix)

| Surface | Proof |
| --- | --- |
| Multi-seller Delivered order packages | Checkout `01a08973-dd8c-7000-b205-cc7f15358dfb` — packages present (`MP-01A08973E914` Cancelled + `MP-01A08973EB58` Delivered); central create/dispatch/deliver actions exercised |
| Single-seller packages=0 | Checkout `01a08973-d831-7000-ae48-d6f8a6bc3fcf` — `consolidatedPackages` empty; `create_consolidated_package` absent (central section stays hidden) |

## FE inspection note

Host admin detail + operational-history verified live (`MP-*` package numbers; FA history labels; no GUID summaries). FE `/fa/admin/orders/{id}` shell returned HTTP 500 in this smoke environment (auth/runtime shell), so Worker does not claim browser visual acceptance. Component structure covered by FE tests (`AdminConsolidatedPackageSection` multi-seller-only). No redesign.

Checklist (no redesign):

- Central section only when multi-seller
- Hierarchy fits existing Orders page; no duplicated shipment UI
- Member lock explanation clear; package card compact; actions obvious
- RTL clean; dialogs fit viewport; no dead controls
- No GUIDs / no technical English leakage in Persian locale
