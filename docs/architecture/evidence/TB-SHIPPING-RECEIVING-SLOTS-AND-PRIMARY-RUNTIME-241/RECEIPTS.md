# TB-SHIPPING-RECEIVING-SLOTS-AND-PRIMARY-RUNTIME-241 — RECEIPTS

## A — Provenance

| Item | Value |
| --- | --- |
| Issue SHA | `5552a97f84ac28f948d690169be1e458420c0dd4` |
| Code SHA | `0bcfa52aa4ede4838b45dceb60ec2931219a6f7f` |
| Primary Host | `127.0.0.1:5088` / `D:\temp\tb241-primary\Tooba.Host.exe` / PID 45168 / DeploymentId `marketplace-tb241-primary` |
| Primary FE | `127.0.0.1:3000` / `next start -p 3000` from `D:\Users\User\source\repos\SarvNewVer` @ same SHA / PID 18832 |
| DB | `tooba_marketplace_tb239` (Marketplace, Tenant NULL) |
| Migrations applied | Shipping `20261028201416_ReceivingSlotDuration`; Order `20261028200100_AcceptSlotDurationFormat` |
| Handoff Chrome | profile `D:\temp\tb241-chrome`, CDP 9350, PID 47688 |
| Stopped task ports | FE 3100/3101/3102/3103/3104 + Host 5188/5189/5190 (see `D:\temp\tb241-stopped-ports.json`) |

## B — Projection slots (PRIMARY, handoff cart)

Cart `01a1846b-aede-7000-850f-de073a511739` v8, projection 200, 2 groups:

- Arman: first day slots `09:00-11:00`, `11:00-13:00`, `13:00-15:00`, `15:00-17:00` (duration 120)
- DigiStyle: first day slots `09:00-10:00` … `16:00-17:00` including `14:00-15:00` (duration 60)

## C — DOM on PRIMARY after date pick (unsubmitted)

Blocker: «برای «فروشگاه آرمان» یکی از ساعت‌های تحویل همان روز را انتخاب کنید.»
Arman radios: `09:00–11:00` … `15:00–17:00` (`data-testid` shipping-group-captured-slot-…).
DigiStyle radios: `09:00–10:00` … `16:00–17:00` including `14:00–15:00`.
Continue disabled; no StartPayment from the handoff window.

## D — Persisted Order chosen windows (prior proof cart)

Checkout `01a18468-1af1-7000-82ca-5e59db964f49` / payment Pending manual / reservations 3:

```
Arman     2026-11-01  14:00-15:00  open=504000000000 close=540000000000 format=2
DigiStyle 2026-11-01  16:00-17:00  open=576000000000 close=612000000000 format=2
Platform  2026-11-01  10:00-11:00  open=360000000000 close=396000000000 format=2
```

## E — Disclosed defect

`POST /v1/storefront/shipping/commit` once returned 500 `platform.unexpected` / `ContractOperationException`
(traceId `66fcc1116ad89db2014e4a4f6f6fb073`). StartPayment on the same primary produced the Order rows above.
No screenshot artifacts.
